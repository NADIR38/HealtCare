using HealthcareSystem.Application.DTOs.Payment;
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
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentService> _logger;
        private readonly IEmailService _emailService;
        private readonly DatabaseHelpers _helper;

        public PaymentService(
            ApplicationDbContext context,
            ILogger<PaymentService> logger,
            IEmailService emailService,
            DatabaseHelpers helper)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _helper = helper;
        }

        public async Task<PaymentResponse> CreatePaymentAsync(CreatePaymentRequest request, Guid createdBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate invoice exists
                var invoice = await _context.Invoices
                    .Include(i => i.Payments)
                    .Include(i => i.Patient)
                        .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(i => i.Id == request.InvoiceId);

                if (invoice == null)
                {
                    throw new NotFoundException("Invoice", request.InvoiceId);
                }

                // Check if invoice is cancelled
                if (invoice.Status == InvoiceStatus.Cancelled)
                {
                    throw new InvalidOperationException("Cannot add payment to a cancelled invoice");
                }

                // Calculate current paid amount
                var currentPaidAmount = invoice.Payments
                    .Where(p => p.Status == PaymentStatus.Completed)
                    .Sum(p => p.Amount);

                // Validate payment amount
                var remainingAmount = invoice.TotalAmount - currentPaidAmount;
                if (request.Amount > remainingAmount)
                {
                    throw new InvalidOperationException(
                        $"Payment amount ({request.Amount:C}) exceeds remaining balance ({remainingAmount:C})");
                }

                // Generate payment number
                var paymentNumber = await GeneratePaymentNumber();

                // Create payment
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = request.InvoiceId,
                    PaymentNumber = paymentNumber,
                    Amount = request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    Status = PaymentStatus.Completed, // Mark as completed immediately
                    PaymentDate = request.PaymentDate,
                    TransactionId = request.TransactionId,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                _context.Payments.Add(payment);

                // Update invoice status based on payment
                var newPaidAmount = currentPaidAmount + request.Amount;
                if (newPaidAmount >= invoice.TotalAmount)
                {
                    invoice.Status = InvoiceStatus.Paid;
                }
                else if (newPaidAmount > 0)
                {
                    invoice.Status = InvoiceStatus.PartiallyPaid;
                }

                invoice.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                try
                {
                    // Send payment confirmation email
                    await SendPaymentConfirmationEmail(payment, invoice);

                    _logger.LogInformation("Payment {PaymentNumber} created for invoice {InvoiceNumber}",
                        paymentNumber, invoice.InvoiceNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, "Cannot send email");
                }
                return await GetPaymentByIdAsync(payment.Id);
            }
            // PaymentService.cs line 119 ke aas paas
            catch (Exception ex)
            {
                // Sirf tab rollback karen agar transaction abhi active ho
                if (_context.Database.CurrentTransaction != null)
                {
                    await _context.Database.RollbackTransactionAsync();
                }
                _logger.LogError(ex, "Payment creation failed");
                throw;
            }
        }

        public async Task<PaymentResponse> GetPaymentByIdAsync(Guid paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Patient)
                        .ThenInclude(pt => pt.User)
                .Include(p => p.CreatedByUser)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                throw new NotFoundException("Payment", paymentId);
            }

            return MapToResponse(payment);
        }

        public async Task<PaymentResponse> GetPaymentByNumberAsync(string paymentNumber)
        {
            var payment = await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Patient)
                        .ThenInclude(pt => pt.User)
                .Include(p => p.CreatedByUser)
                .FirstOrDefaultAsync(p => p.PaymentNumber == paymentNumber);

            if (payment == null)
            {
                throw new NotFoundException($"Payment with number {paymentNumber} not found");
            }

            return MapToResponse(payment);
        }

        public async Task<List<PaymentResponse>> GetInvoicePaymentsAsync(Guid invoiceId)
        {
            // Validate invoice exists
            var invoice = await _helper.CheckEntityExists<Invoice>(invoiceId);

            var payments = await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Patient)
                        .ThenInclude(pt => pt.User)
                .Include(p => p.CreatedByUser)
                .Where(p => p.InvoiceId == invoiceId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(MapToResponse).ToList();
        }

        public async Task<List<PaymentResponse>> GetPatientPaymentsAsync(Guid patientId)
        {
            // Validate patient exists
            await _helper.CheckPatientExist(patientId);

            var payments = await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Patient)
                        .ThenInclude(pt => pt.User)
                .Include(p => p.CreatedByUser)
                .Where(p => p.Invoice.PatientId == patientId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(MapToResponse).ToList();
        }

        public async Task<List<PaymentResponse>> GetAllPaymentsAsync(
            int page,
            int pageSize,
            PaymentMethod? paymentMethod = null,
            PaymentStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null)
        {
            var query = _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Patient)
                        .ThenInclude(pt => pt.User)
                .Include(p => p.CreatedByUser)
                .AsQueryable();

            // Apply payment method filter
            if (paymentMethod.HasValue)
            {
                query = query.Where(p => p.PaymentMethod == paymentMethod.Value);
            }

            // Apply status filter
            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            // Apply date range filter
            if (fromDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate <= toDate.Value);
            }

            // Apply search term (payment number, invoice number, patient name)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(p =>
                    p.PaymentNumber.ToLower().Contains(searchTerm) ||
                    p.Invoice.InvoiceNumber.ToLower().Contains(searchTerm) ||
                    p.Invoice.Patient.User.FirstName.ToLower().Contains(searchTerm) ||
                    p.Invoice.Patient.User.LastName.ToLower().Contains(searchTerm));
            }

            // Order by payment date descending and paginate
            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return payments.Select(MapToResponse).ToList();
        }

        public async Task<PaymentResponse> UpdatePaymentStatusAsync(Guid paymentId, PaymentStatus newStatus)
        {
            var payment = await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Payments)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                throw new NotFoundException("Payment", paymentId);
            }

            // Validate status transition
            if (!IsValidStatusTransition(payment.Status, newStatus))
            {
                throw new InvalidOperationException(
                    $"Cannot transition payment status from {payment.Status} to {newStatus}");
            }

            var oldStatus = payment.Status;
            payment.Status = newStatus;

            // Update invoice status based on payment changes
            var completedPayments = payment.Invoice.Payments
                .Where(p => p.Id != paymentId && p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);

            if (newStatus == PaymentStatus.Completed)
            {
                completedPayments += payment.Amount;
            }

            if (completedPayments >= payment.Invoice.TotalAmount)
            {
                payment.Invoice.Status = InvoiceStatus.Paid;
            }
            else if (completedPayments > 0)
            {
                payment.Invoice.Status = InvoiceStatus.PartiallyPaid;
            }
            else
            {
                payment.Invoice.Status = InvoiceStatus.Pending;
            }

            payment.Invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment {PaymentId} status updated from {OldStatus} to {NewStatus}",
                paymentId, oldStatus, newStatus);

            return await GetPaymentByIdAsync(paymentId);
        }

        public async Task<bool> RefundPaymentAsync(Guid paymentId, string? reason = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Payments)
                    .Include(p => p.Invoice.Patient)
                        .ThenInclude(pt => pt.User)
                    .FirstOrDefaultAsync(p => p.Id == paymentId);

                if (payment == null)
                {
                    throw new NotFoundException("Payment", paymentId);
                }

                if (payment.Status != PaymentStatus.Completed)
                {
                    throw new InvalidOperationException("Only completed payments can be refunded");
                }

                // Update payment status to Refunded
                payment.Status = PaymentStatus.Refunded;
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    payment.Notes = $"{payment.Notes ?? ""}\nRefund Reason: {reason}";
                }

                // Recalculate invoice status
                var completedPayments = payment.Invoice.Payments
                    .Where(p => p.Id != paymentId && p.Status == PaymentStatus.Completed)
                    .Sum(p => p.Amount);

                if (completedPayments >= payment.Invoice.TotalAmount)
                {
                    payment.Invoice.Status = InvoiceStatus.Paid;
                }
                else if (completedPayments > 0)
                {
                    payment.Invoice.Status = InvoiceStatus.PartiallyPaid;
                }
                else
                {
                    payment.Invoice.Status = InvoiceStatus.Pending;
                }

                payment.Invoice.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Send refund confirmation email
                var emailMessage = new EmailMessage
                {
                    To = new List<string> { payment.Invoice.Patient.User.Email },
                    Subject = $"Payment Refund - {payment.PaymentNumber}",
                    Body = $@"
                        <html>
                        <body>
                            <h2>Payment Refund Confirmation</h2>
                            <p>Dear {payment.Invoice.Patient.User.FirstName} {payment.Invoice.Patient.User.LastName},</p>
                            <p>Your payment has been refunded.</p>
                            <p><strong>Payment Details:</strong></p>
                            <ul>
                                <li>Payment Number: {payment.PaymentNumber}</li>
                                <li>Amount Refunded: {payment.Amount:C}</li>
                                <li>Original Payment Date: {payment.PaymentDate:MMMM dd, yyyy}</li>
                                <li>Payment Method: {payment.PaymentMethod}</li>
                                {(!string.IsNullOrWhiteSpace(reason) ? $"<li>Reason: {reason}</li>" : "")}
                            </ul>
                            <p>The refund will be processed to your original payment method within 5-10 business days.</p>
                            <p>Best regards,<br/>Healthcare System Team</p>
                        </body>
                        </html>",
                    IsHtml = true
                };
                try
                {

                    await _emailService.SendEmailAsync(emailMessage);

                    _logger.LogInformation("Payment {PaymentNumber} refunded. Reason: {Reason}",
                        payment.PaymentNumber, reason ?? "Not specified");
                }
                catch (Exception ex)
                {
                    _logger.LogInformation(ex, "Cannot send email");
                }
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error refunding payment {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task<object> GetPaymentStatisticsAsync(DateTime fromDate, DateTime toDate)
        {
            var payments = await _context.Payments
                .Include(p => p.Invoice)
                .Where(p => p.PaymentDate >= fromDate && p.PaymentDate <= toDate)
                .ToListAsync();

            var totalPayments = payments.Count;
            var completedPayments = payments.Where(p => p.Status == PaymentStatus.Completed).ToList();
            var totalAmount = completedPayments.Sum(p => p.Amount);

            var byMethod = payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new
                {
                    PaymentMethod = g.Key.ToString(),
                    Count = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            var byStatus = payments
                .GroupBy(p => p.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .ToList();

            return new
            {
                TotalPayments = totalPayments,
                CompletedPaymentsCount = completedPayments.Count,
                TotalAmountCollected = totalAmount,
                AveragePaymentAmount = completedPayments.Any() ? totalAmount / completedPayments.Count : 0,
                PaymentsByMethod = byMethod,
                PaymentsByStatus = byStatus,
                DateRange = new
                {
                    From = fromDate.ToString("yyyy-MM-dd"),
                    To = toDate.ToString("yyyy-MM-dd")
                }
            };
        }

        // Private helper methods
        private async Task<string> GeneratePaymentNumber()
        {
            var year = DateTime.UtcNow.Year;

            var lastPayment = await _context.Payments
                .Where(p => p.PaymentNumber.StartsWith($"PAY-{year}-"))
                .OrderByDescending(p => p.PaymentNumber)
                .FirstOrDefaultAsync();

            if (lastPayment == null)
            {
                return $"PAY-{year}-00001";
            }

            var parts = lastPayment.PaymentNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
            {
                return $"PAY-{year}-{(lastNumber + 1):D5}";
            }

            _logger.LogWarning("Invalid payment number format: {PaymentNumber}. Resetting to PAY-{Year}-00001",
                lastPayment.PaymentNumber, year);
            return $"PAY-{year}-00001";
        }

        private bool IsValidStatusTransition(PaymentStatus currentStatus, PaymentStatus newStatus)
        {
            return (currentStatus, newStatus) switch
            {
                (PaymentStatus.Pending, PaymentStatus.Completed) => true,
                (PaymentStatus.Pending, PaymentStatus.Failed) => true,
                (PaymentStatus.Completed, PaymentStatus.Refunded) => true,
                _ => false
            };
        }

        private async Task SendPaymentConfirmationEmail(Payment payment, Invoice invoice)
        {
            var emailMessage = new EmailMessage
            {
                To = new List<string> { invoice.Patient.User.Email },
                Subject = $"Payment Confirmation - {payment.PaymentNumber}",
                Body = $@"
                    <html>
                    <body>
                        <h2>Payment Received</h2>
                        <p>Dear {invoice.Patient.User.FirstName} {invoice.Patient.User.LastName},</p>
                        <p>Thank you for your payment. We have successfully received your payment.</p>
                        <p><strong>Payment Details:</strong></p>
                        <ul>
                            <li>Payment Number: {payment.PaymentNumber}</li>
                            <li>Invoice Number: {invoice.InvoiceNumber}</li>
                            <li>Amount Paid: {payment.Amount:C}</li>
                            <li>Payment Date: {payment.PaymentDate:MMMM dd, yyyy}</li>
                            <li>Payment Method: {payment.PaymentMethod}</li>
                        </ul>
                        <p><strong>Invoice Summary:</strong></p>
                        <ul>
                            <li>Total Amount: {invoice.TotalAmount:C}</li>
                            <li>Paid Amount: {invoice.Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount):C}</li>
                            <li>Balance: {invoice.TotalAmount - invoice.Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount):C}</li>
                            <li>Status: {invoice.Status}</li>
                        </ul>
                        <p>Best regards,<br/>Healthcare System Team</p>
                    </body>
                    </html>",
                IsHtml = true
            };

            await _emailService.SendEmailAsync(emailMessage);
        }

        private PaymentResponse MapToResponse(Payment payment)
        {
            return new PaymentResponse
            {
                Id = payment.Id,
                InvoiceId = payment.InvoiceId,
                InvoiceNumber = payment.Invoice.InvoiceNumber,
                PaymentNumber = payment.PaymentNumber,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                Status = payment.Status,
                PaymentDate = payment.PaymentDate,
                TransactionId = payment.TransactionId,
                Notes = payment.Notes,
                CreatedAt = payment.CreatedAt,
                CreatedByName = $"{payment.CreatedByUser.FirstName} {payment.CreatedByUser.LastName}",
                PatientName = $"{payment.Invoice.Patient.User.FirstName} {payment.Invoice.Patient.User.LastName}",
                PatientNumber = payment.Invoice.Patient.PatientNumber
            };
        }
    }
}