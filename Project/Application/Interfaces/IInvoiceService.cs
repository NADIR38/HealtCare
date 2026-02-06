using HealthcareSystem.Application.DTOs.Invoice;
using HealthcareSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Interfaces
{
    public interface IInvoiceService
    {
        /// <summary>
        /// TODO: Create invoice manually
        /// - Validate patient exists
        /// - If appointmentId provided, validate it exists
        /// - Generate invoice number (INV-YYYY-00001)
        /// - Calculate amounts: Amount = Quantity * UnitPrice for each item
        /// - Calculate SubTotal = Sum of all item amounts
        /// - Calculate TotalAmount = SubTotal + TaxAmount - DiscountAmount
        /// - Set status to Draft initially
        /// - Create invoice with items
        /// - Return response with all details
        /// </summary>
        Task<InvoiceResponse> CreateInvoiceAsync(CreateInvoiceRequest request, Guid createdBy);

        /// <summary>
        /// TODO: Create invoice from appointment automatically
        /// - Fetch appointment with doctor details
        /// - Create invoice with consultation fee as first item
        /// - Add any additional items from request
        /// - Calculate totals
        /// - Link to appointment
        /// - Set status to Pending
        /// - Return response
        /// </summary>
        Task<InvoiceResponse> CreateInvoiceFromAppointmentAsync(CreateInvoiceFromAppointmentRequest request, Guid createdBy);

        /// <summary>
        /// TODO: Get invoice by ID
        /// - Fetch with all includes (Patient, Items, Payments, Appointment, CreatedBy)
        /// - Calculate PaidAmount = Sum of completed payments
        /// - Calculate BalanceAmount = TotalAmount - PaidAmount
        /// - Map to response
        /// </summary>
        Task<InvoiceResponse> GetInvoiceByIdAsync(Guid invoiceId);

        /// <summary>
        /// TODO: Get invoice by invoice number
        /// - Search by invoice number
        /// - Include all related data
        /// - Map to response
        /// </summary>
        Task<InvoiceResponse> GetInvoiceByNumberAsync(string invoiceNumber);

        /// <summary>
        /// TODO: Get all invoices for a patient
        /// - Validate patient exists
        /// - Fetch all invoices ordered by date descending
        /// - Optionally filter by status
        /// - Map to responses
        /// </summary>
        Task<List<InvoiceResponse>> GetPatientInvoicesAsync(Guid patientId, InvoiceStatus? status = null);

        /// <summary>
        /// TODO: Get all invoices with pagination and filters
        /// - Apply status filter if provided
        /// - Apply date range filter if provided
        /// - Apply search term (patient name, invoice number)
        /// - Order by invoice date descending
        /// - Paginate results
        /// - Map to responses
        /// </summary>
        Task<List<InvoiceResponse>> GetAllInvoicesAsync(
            int page,
            int pageSize,
            InvoiceStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null);

        /// <summary>
        /// TODO: Update invoice (only if status is Draft or Pending)
        /// - Fetch invoice
        /// - Check status (cannot update Paid/Cancelled)
        /// - Update editable fields
        /// - Recalculate totals if tax/discount changed
        /// - Save changes
        /// - Return updated response
        /// </summary>
        Task<InvoiceResponse> UpdateInvoiceAsync(Guid invoiceId, UpdateInvoiceRequest request);

        /// <summary>
        /// TODO: Update invoice status
        /// - Fetch invoice with payments
        /// - Validate status transition (Draft→Pending→Paid/Cancelled)
        /// - If marking as Paid, ensure PaidAmount >= TotalAmount
        /// - Update status
        /// - Send email notification to patient
        /// - Return updated response
        /// </summary>
        Task<InvoiceResponse> UpdateInvoiceStatusAsync(Guid invoiceId, InvoiceStatus newStatus);

        /// <summary>
        /// TODO: Mark invoice as sent (Draft → Pending)
        /// - Update status to Pending
        /// - Send invoice email to patient with PDF attachment
        /// - Return response
        /// </summary>
        Task<InvoiceResponse> SendInvoiceAsync(Guid invoiceId);

        /// <summary>
        /// TODO: Cancel invoice
        /// - Check if has payments (cannot cancel if paid)
        /// - Set status to Cancelled
        /// - Send cancellation email to patient
        /// - Return success
        /// </summary>
        Task<bool> CancelInvoiceAsync(Guid invoiceId);

        /// <summary>
        /// TODO: Get overdue invoices
        /// - Fetch invoices where DueDate < Today and Status != Paid/Cancelled
        /// - Order by due date
        /// - Map to responses
        /// </summary>
        Task<List<InvoiceResponse>> GetOverdueInvoicesAsync();

        /// <summary>
        /// TODO: Get revenue statistics
        /// - Total invoices in date range
        /// - Total revenue (sum of paid invoices)
        /// - Pending amount (sum of pending/partially paid)
        /// - Overdue amount
        /// - Group by payment method
        /// - Return statistics object
        /// </summary>
        Task<object> GetRevenueStatisticsAsync(DateTime fromDate, DateTime toDate);
    }
}