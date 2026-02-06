using HealthcareSystem.Application.DTOs.Invoice;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Application.Models;
using HealthcareSystem.Domain.Entities;
using HealthcareSystem.Domain.Enums;
using HealthcareSystem.Infrastructure.Data;
using HealthcareSystem.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthcareSystem.Infrastructure.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InvoiceService> _logger;
        private readonly IPdfService _pdfService;
        private readonly IEmailService _emailService;
        private readonly DatabaseHelpers _helper;
        public InvoiceService(
            ApplicationDbContext context,
            ILogger<InvoiceService> logger,
            IPdfService pdfService,
            IEmailService emailService,
            DatabaseHelpers helper)
        {
            _context = context;
            _logger = logger;
            _pdfService = pdfService;
            _emailService = emailService;
            _helper = helper;
        }

        public async Task<InvoiceResponse> CreateInvoiceAsync(CreateInvoiceRequest request, Guid createdBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate patient exists
                var patient = await _helper.CheckPatientExist(request.PatientId);

                // If appointmentId provided, validate it exists
                Appointment? appointment = null;
                if (request.AppointmentId.HasValue)
                {
                    appointment = await _helper.ValidateAppointment(request.AppointmentId.Value, request.PatientId);
                }

                // Generate invoice number
                var invoiceNumber = await GenerateInvoiceNumber();

                // Calculate amounts
                decimal subTotal = 0;
                var invoiceItems = new List<InvoiceItem>();

                foreach (var item in request.Items)
                {
                    var amount = CalculateAmount(item.Quantity, item.UnitPrice);
                    subTotal += amount;

                    invoiceItems.Add(new InvoiceItem
                    {
                        Id = Guid.NewGuid(),
                        Description = item.Description,
                        ItemType = item.ItemType,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Amount = amount
                    });
                }

                // Calculate total amount
                var taxAmount = request.TaxAmount ?? 0;
                var discountAmount = request.DiscountAmount ?? 0;
                var totalAmount = subTotal + taxAmount - discountAmount;

                // Create invoice
                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = invoiceNumber,
                    PatientId = request.PatientId,
                    AppointmentId = request.AppointmentId,
                    InvoiceDate = request.InvoiceDate,
                    DueDate = request.DueDate,
                    Status = InvoiceStatus.Draft,
                    SubTotal = subTotal,
                    TaxAmount = taxAmount,
                    DiscountAmount = discountAmount,
                    TotalAmount = totalAmount,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy,
                    Items = invoiceItems
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Invoice {InvoiceNumber} created successfully for patient {PatientId}",
                    invoiceNumber, request.PatientId);

                return await GetInvoiceByIdAsync(invoice.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating invoice for patient {PatientId}", request.PatientId);
                throw;
            }
        }

        public async Task<InvoiceResponse> CreateInvoiceFromAppointmentAsync(CreateInvoiceFromAppointmentRequest request, Guid createdBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Doctor)
                        .ThenInclude(d => d.User)
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == request.AppointmentId);

                if (appointment == null)
                {
                    throw new NotFoundException("Appointment", request.AppointmentId);
                }

                // Generate invoice number
                var invoiceNumber = await GenerateInvoiceNumber();

                var items = new List<InvoiceItem>();
                decimal subTotal = 0;

                var consultationFee = appointment.Doctor.ConsultationFee;
                items.Add(new InvoiceItem
                {
                    Id = Guid.NewGuid(),
                    Description = $"Consultation with Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}",
                    ItemType = "Consultation",
                    Quantity = 1,
                    UnitPrice = consultationFee,
                    Amount = consultationFee
                });
                subTotal += consultationFee;

                if (request.AdditionalItems != null && request.AdditionalItems.Any())
                {
                    foreach (var additionalItem in request.AdditionalItems)
                    {
                        var amount = CalculateAmount(additionalItem.Quantity, additionalItem.UnitPrice);
                        subTotal += amount;

                        items.Add(new InvoiceItem
                        {
                            Id = Guid.NewGuid(),
                            Description = additionalItem.Description,
                            ItemType = additionalItem.ItemType,
                            Quantity = additionalItem.Quantity,
                            UnitPrice = additionalItem.UnitPrice,
                            Amount = amount
                        });
                    }
                }

                var taxAmount = request.TaxAmount ?? 0;
                var discountAmount = request.DiscountAmount ?? 0;
                var totalAmount = subTotal + taxAmount - discountAmount;

                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = invoiceNumber,
                    PatientId = appointment.PatientId,
                    AppointmentId = appointment.Id,
                    InvoiceDate = DateTime.UtcNow,
                    DueDate = request.DueDate,
                    Status = InvoiceStatus.Pending,
                    SubTotal = subTotal,
                    TaxAmount = taxAmount,
                    DiscountAmount = discountAmount,
                    TotalAmount = totalAmount,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy,
                    Items = items
                };

                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Invoice {InvoiceNumber} created from appointment {AppointmentId}",
                    invoiceNumber, request.AppointmentId);

                return await GetInvoiceByIdAsync(invoice.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating invoice from appointment {AppointmentId}", request.AppointmentId);
                throw;
            }
        }

        public async Task<InvoiceResponse> GetInvoiceByIdAsync(Guid invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Patient)
                    .ThenInclude(p => p.User)
                .Include(i => i.Items)
                .Include(i => i.Payments)
                .Include(i => i.Appointment)
                .Include(i => i.CreatedByUser)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                throw new NotFoundException("Invoice", invoiceId);
            }

            return MapToResponse(invoice);
        }

        public async Task<InvoiceResponse> GetInvoiceByNumberAsync(string invoiceNumber)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Patient)
                    .ThenInclude(p => p.User)
                .Include(i => i.Items)
                .Include(i => i.Payments)
                .Include(i => i.Appointment)
                .Include(i => i.CreatedByUser)
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);

            if (invoice == null)
            {
                throw new NotFoundException($"Invoice with number {invoiceNumber} not found");
            }

            return MapToResponse(invoice);
        }

        public async Task<List<InvoiceResponse>> GetPatientInvoicesAsync(Guid patientId, InvoiceStatus? status = null)
        {
            // Validate patient exists
            await _helper.CheckPatientExist(patientId);

            var query = _context.Invoices
                .Include(i => i.Patient)
                    .ThenInclude(p => p.User)
                .Include(i => i.Items)
                .Include(i => i.Payments)
                .Include(i => i.Appointment)
                .Include(i => i.CreatedByUser)
                .Where(i => i.PatientId == patientId);

            // Optionally filter by status
            if (status.HasValue)
            {
                query = query.Where(i => i.Status == status.Value);
            }

            var invoices = await query
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            return invoices.Select(MapToResponse).ToList();
        }

        public async Task<List<InvoiceResponse>> GetAllInvoicesAsync(
            int page,
            int pageSize,
            InvoiceStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null)
        {
            var query = _context.Invoices
                .Include(i => i.Patient)
                    .ThenInclude(p => p.User)
                .Include(i => i.Items)
                .Include(i => i.Payments)
                .Include(i => i.Appointment)
                .Include(i => i.CreatedByUser)
                .AsQueryable();

            // Apply status filter
            if (status.HasValue)
            {
                query = query.Where(i => i.Status == status.Value);
            }

            // Apply date range filter
            if (fromDate.HasValue)
            {
                query = query.Where(i => i.InvoiceDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(i => i.InvoiceDate <= toDate.Value);
            }

            // Apply search term (patient name or invoice number)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(i =>
                    i.InvoiceNumber.ToLower().Contains(searchTerm) ||
                    i.Patient.User.FirstName.ToLower().Contains(searchTerm) ||
                    i.Patient.User.LastName.ToLower().Contains(searchTerm));
            }

            // Order by invoice date descending and paginate
            var invoices = await query
                .OrderByDescending(i => i.InvoiceDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return invoices.Select(MapToResponse).ToList();
        }

        public async Task<InvoiceResponse> UpdateInvoiceAsync(Guid invoiceId, UpdateInvoiceRequest request)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);

            if (invoice == null)
            {
                throw new NotFoundException("Invoice", invoiceId);
            }

            // Check status - cannot update Paid or Cancelled invoices
            if (invoice.Status == InvoiceStatus.Paid || invoice.Status == InvoiceStatus.Cancelled)
            {
                throw new InvalidOperationException($"Cannot update invoice with status {invoice.Status}");
            }

            // Update editable fields
            if (request.DueDate.HasValue)
            {
                invoice.DueDate = request.DueDate.Value;
            }

            if (request.TaxAmount.HasValue)
            {
                invoice.TaxAmount = request.TaxAmount.Value;
            }

            if (request.DiscountAmount.HasValue)
            {
                invoice.DiscountAmount = request.DiscountAmount.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                invoice.Notes = request.Notes;
            }

            // Recalculate total amount if tax or discount changed
            invoice.TotalAmount = invoice.SubTotal + (invoice.TaxAmount ?? 0) - (invoice.DiscountAmount ?? 0);
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceId} updated successfully", invoiceId);

            return await GetInvoiceByIdAsync(invoiceId);
        }

        public async Task<InvoiceResponse> UpdateInvoiceStatusAsync(Guid invoiceId, InvoiceStatus newStatus)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .Include(i => i.Patient)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                throw new NotFoundException("Invoice", invoiceId);
            }

            // Validate status transition
            if (!IsValidStatusTransition(invoice.Status, newStatus))
            {
                throw new InvalidOperationException(
                    $"Cannot transition from {invoice.Status} to {newStatus}");
            }

            // If marking as Paid, ensure PaidAmount >= TotalAmount
            if (newStatus == InvoiceStatus.Paid)
            {
                var paidAmount = invoice.Payments
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .Sum(p => p.Amount);

                if (paidAmount < invoice.TotalAmount)
                {
                    throw new InvalidOperationException(
                        $"Cannot mark invoice as Paid. Paid amount ({paidAmount:C}) is less than total amount ({invoice.TotalAmount:C})");
                }
            }

            invoice.Status = newStatus;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Send email notification to patient
            await SendStatusUpdateEmail(invoice);

            _logger.LogInformation("Invoice {InvoiceId} status updated to {Status}", invoiceId, newStatus);

            return await GetInvoiceByIdAsync(invoiceId);
        }

        public async Task<InvoiceResponse> SendInvoiceAsync(Guid invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Patient)
                    .ThenInclude(p => p.User)
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                throw new NotFoundException("Invoice", invoiceId);
            }

            if (invoice.Status != InvoiceStatus.Draft)
            {
                throw new InvalidOperationException("Only draft invoices can be sent");
            }

            // Update status to Pending
            invoice.Status = InvoiceStatus.Pending;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate PDF
            var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(invoiceId);

            // Send email with PDF attachment
            var emailMessage = new EmailMessage
            {
                To = new List<string> { invoice.Patient.User.Email },
                Subject = $"Invoice {invoice.InvoiceNumber}",
                Body = $@"
                    <html>
                    <body>
                        <h2>Invoice from Healthcare System</h2>
                        <p>Dear {invoice.Patient.User.FirstName} {invoice.Patient.User.LastName},</p>
                        <p>Please find attached your invoice {invoice.InvoiceNumber}.</p>
                        <p><strong>Invoice Details:</strong></p>
                        <ul>
                            <li>Invoice Number: {invoice.InvoiceNumber}</li>
                            <li>Invoice Date: {invoice.InvoiceDate:MMMM dd, yyyy}</li>
                            <li>Due Date: {invoice.DueDate?.ToString("MMMM dd, yyyy") ?? "N/A"}</li>
                            <li>Total Amount: {invoice.TotalAmount:C}</li>
                        </ul>
                        <p>Please make payment by the due date.</p>
                        <p>Best regards,<br/>Healthcare System Team</p>
                    </body>
                    </html>",
                IsHtml = true
            };

            await _emailService.SendEmailWithAttachmentAsync(
                emailMessage,
                pdfBytes,
                $"Invoice_{invoice.InvoiceNumber}.pdf");

            _logger.LogInformation("Invoice {InvoiceNumber} sent to patient {PatientEmail}",
                invoice.InvoiceNumber, invoice.Patient.User.Email);

            return await GetInvoiceByIdAsync(invoiceId);
        }

        public async Task<bool> CancelInvoiceAsync(Guid invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Payments)
                .Include(i => i.Patient)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                throw new NotFoundException("Invoice", invoiceId);
            }

            // Check if has completed payments
            var hasPayments = invoice.Payments.Any(p => p.Status == PaymentStatus.Completed);
            if (hasPayments)
            {
                throw new InvalidOperationException("Cannot cancel invoice with completed payments");
            }

            // Set status to Cancelled
            invoice.Status = InvoiceStatus.Cancelled;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Send cancellation email
            var emailMessage = new EmailMessage
            {
                To = new List<string> { invoice.Patient.User.Email },
                Subject = $"Invoice {invoice.InvoiceNumber} Cancelled",
                Body = $@"
                    <html>
                    <body>
                        <h2>Invoice Cancellation Notice</h2>
                        <p>Dear {invoice.Patient.User.FirstName} {invoice.Patient.User.LastName},</p>
                        <p>Your invoice {invoice.InvoiceNumber} has been cancelled.</p>
                        <p>If you have any questions, please contact us.</p>
                        <p>Best regards,<br/>Healthcare System Team</p>
                    </body>
                    </html>",
                IsHtml = true
            };

            await _emailService.SendEmailAsync(emailMessage);

            _logger.LogInformation("Invoice {InvoiceNumber} cancelled", invoice.InvoiceNumber);

            return true;
        }

        public async Task<List<InvoiceResponse>> GetOverdueInvoicesAsync()
        {
            var today = DateTime.UtcNow.Date;

            var invoices = await _context.Invoices
                .Include(i => i.Patient)
                    .ThenInclude(p => p.User)
                .Include(i => i.Items)
                .Include(i => i.Payments)
                .Include(i => i.Appointment)
                .Include(i => i.CreatedByUser)
                .Where(i => i.DueDate.HasValue &&
                           i.DueDate.Value.Date < today &&
                           i.Status != InvoiceStatus.Paid &&
                           i.Status != InvoiceStatus.Cancelled)
                .OrderBy(i => i.DueDate)
                .ToListAsync();

            return invoices.Select(MapToResponse).ToList();
        }

        public async Task<object> GetRevenueStatisticsAsync(DateTime fromDate, DateTime toDate)
        {
            var invoices = await _context.Invoices
                .Include(i => i.Payments)
                .Where(i => i.InvoiceDate >= fromDate && i.InvoiceDate <= toDate)
                .ToListAsync();

            var totalInvoices = invoices.Count;

            var paidInvoices = invoices.Where(i => i.Status == InvoiceStatus.Paid).ToList();
            var totalRevenue = paidInvoices.Sum(i => i.TotalAmount);

            var pendingInvoices = invoices
                .Where(i => i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.PartiallyPaid)
                .ToList();
            var pendingAmount = pendingInvoices.Sum(i =>
                i.TotalAmount - i.Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount));

            var today = DateTime.UtcNow.Date;
            var overdueInvoices = invoices
                .Where(i => i.DueDate.HasValue &&
                           i.DueDate.Value.Date < today &&
                           i.Status != InvoiceStatus.Paid &&
                           i.Status != InvoiceStatus.Cancelled)
                .ToList();
            var overdueAmount = overdueInvoices.Sum(i =>
                i.TotalAmount - i.Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount));

            // Group by payment method
            var paymentsByMethod = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Completed &&
                           p.PaymentDate >= fromDate &&
                           p.PaymentDate <= toDate)
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new
                {
                    PaymentMethod = g.Key.ToString(),
                    TotalAmount = g.Sum(p => p.Amount),
                    Count = g.Count()
                })
                .ToListAsync();

            return new
            {
                TotalInvoices = totalInvoices,
                TotalRevenue = totalRevenue,
                PaidInvoicesCount = paidInvoices.Count,
                PendingAmount = pendingAmount,
                PendingInvoicesCount = pendingInvoices.Count,
                OverdueAmount = overdueAmount,
                OverdueInvoicesCount = overdueInvoices.Count,
                PaymentsByMethod = paymentsByMethod,
                DateRange = new
                {
                    From = fromDate.ToString("yyyy-MM-dd"),
                    To = toDate.ToString("yyyy-MM-dd")
                }
            };
        }

        // Private helper methods
        private decimal CalculateAmount(int quantity, decimal price)
        {
            return quantity * price;
        }

        private async Task<string> GenerateInvoiceNumber()
        {
            var year = DateTime.UtcNow.Year;

            var lastInvoice = await _context.Invoices
                .Where(i => i.InvoiceNumber.StartsWith($"INV-{year}-"))
                .OrderByDescending(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            if (lastInvoice == null)
            {
                return $"INV-{year}-00001";
            }

            var parts = lastInvoice.InvoiceNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
            {
                return $"INV-{year}-{(lastNumber + 1):D5}";
            }

            _logger.LogWarning("Invalid invoice number format: {InvoiceNumber}. Resetting to INV-{Year}-00001",
                lastInvoice.InvoiceNumber, year);
            return $"INV-{year}-00001";
        }

        private bool IsValidStatusTransition(InvoiceStatus currentStatus, InvoiceStatus newStatus)
        {
            return (currentStatus, newStatus) switch
            {
                (InvoiceStatus.Draft, InvoiceStatus.Pending) => true,
                (InvoiceStatus.Draft, InvoiceStatus.Cancelled) => true,
                (InvoiceStatus.Pending, InvoiceStatus.Paid) => true,
                (InvoiceStatus.Pending, InvoiceStatus.PartiallyPaid) => true,
                (InvoiceStatus.Pending, InvoiceStatus.Cancelled) => true,
                (InvoiceStatus.PartiallyPaid, InvoiceStatus.Paid) => true,
                (InvoiceStatus.PartiallyPaid, InvoiceStatus.Cancelled) => true,
                _ => false
            };
        }

        private async Task SendStatusUpdateEmail(Invoice invoice)
        {
            var emailMessage = new EmailMessage
            {
                To = new List<string> { invoice.Patient.User.Email },
                Subject = $"Invoice Status Update - {invoice.InvoiceNumber}",
                Body = $@"
                    <html>
                    <body>
                        <h2>Invoice Status Update</h2>
                        <p>Dear {invoice.Patient.User.FirstName} {invoice.Patient.User.LastName},</p>
                        <p>Your invoice {invoice.InvoiceNumber} status has been updated to <strong>{invoice.Status}</strong>.</p>
                        <p>Invoice Details:</p>
                        <ul>
                            <li>Invoice Number: {invoice.InvoiceNumber}</li>
                            <li>Total Amount: {invoice.TotalAmount:C}</li>
                            <li>Status: {invoice.Status}</li>
                        </ul>
                        <p>Best regards,<br/>Healthcare System Team</p>
                    </body>
                    </html>",
                IsHtml = true
            };

            await _emailService.SendEmailAsync(emailMessage);
        }

        private InvoiceResponse MapToResponse(Invoice invoice)
        {
            // Calculate paid amount
            var paidAmount = invoice.Payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);

            var balanceAmount = invoice.TotalAmount - paidAmount;

            return new InvoiceResponse
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                PatientId = invoice.PatientId,
                PatientName = $"{invoice.Patient.User.FirstName} {invoice.Patient.User.LastName}",
                PatientNumber = invoice.Patient.PatientNumber,
                AppointmentId = invoice.AppointmentId,
                AppointmentNumber = invoice.Appointment?.AppointmentNumber,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                Status = invoice.Status,
                SubTotal = invoice.SubTotal,
                TaxAmount = invoice.TaxAmount ?? 0,
                DiscountAmount = invoice.DiscountAmount ?? 0,
                TotalAmount = invoice.TotalAmount,
                PaidAmount = paidAmount,
                BalanceAmount = balanceAmount,
                Notes = invoice.Notes,
                Items = invoice.Items.Select(i => new InvoiceItemResponse
                {
                    Id = i.Id,
                    Description = i.Description,
                    ItemType = i.ItemType,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Amount = i.Amount
                }).ToList(),
                Payments = invoice.Payments.Select(p => new PaymentSummary
                {
                    Id = p.Id,
                    PaymentNumber = p.PaymentNumber,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    Status = p.Status,
                    PaymentDate = p.PaymentDate
                }).ToList(),
                CreatedAt = invoice.CreatedAt,
                UpdatedAt = invoice.UpdatedAt,
                CreatedByName = $"{invoice.CreatedByUser.FirstName} {invoice.CreatedByUser.LastName}"
            };
        }
    }
}