using HealthcareSystem.Application.DTOs.Payment;
using HealthcareSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Interfaces
{
    public interface IPaymentService
    {
        /// <summary>
        /// Create a new payment for an invoice
        /// - Validate invoice exists and is not cancelled
        /// - Calculate remaining amount (TotalAmount - PaidAmount)
        /// - Validate payment amount doesn't exceed remaining amount
        /// - Generate payment number (PAY-YYYY-00001)
        /// - Create payment with status Completed
        /// - Update invoice status based on payment:
        ///   * If total paid >= invoice total → Paid
        ///   * If total paid > 0 but < total → PartiallyPaid
        /// - Send payment confirmation email to patient
        /// - Return payment response
        /// </summary>
        Task<PaymentResponse> CreatePaymentAsync(CreatePaymentRequest request, Guid createdBy);

        /// <summary>
        /// Get payment by ID
        /// - Fetch with all includes (Invoice, Patient, CreatedBy)
        /// - Map to response
        /// </summary>
        Task<PaymentResponse> GetPaymentByIdAsync(Guid paymentId);

        /// <summary>
        /// Get payment by payment number
        /// - Search by payment number
        /// - Include all related data
        /// - Map to response
        /// </summary>
        Task<PaymentResponse> GetPaymentByNumberAsync(string paymentNumber);

        /// <summary>
        /// Get all payments for a specific invoice
        /// - Validate invoice exists
        /// - Fetch all payments for the invoice
        /// - Order by payment date descending
        /// - Map to responses
        /// </summary>
        Task<List<PaymentResponse>> GetInvoicePaymentsAsync(Guid invoiceId);

        /// <summary>
        /// Get all payments for a specific patient
        /// - Validate patient exists
        /// - Fetch all payments through invoices
        /// - Order by payment date descending
        /// - Map to responses
        /// </summary>
        Task<List<PaymentResponse>> GetPatientPaymentsAsync(Guid patientId);

        /// <summary>
        /// Get all payments with pagination and filters
        /// - Apply payment method filter if provided
        /// - Apply status filter if provided
        /// - Apply date range filter if provided
        /// - Apply search term (payment number, invoice number, patient name)
        /// - Order by payment date descending
        /// - Paginate results
        /// - Map to responses
        /// </summary>
        Task<List<PaymentResponse>> GetAllPaymentsAsync(
            int page,
            int pageSize,
            PaymentMethod? paymentMethod = null,
            PaymentStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null);

        /// <summary>
        /// Update payment status
        /// - Fetch payment with invoice
        /// - Validate status transition (Pending→Completed/Failed, Completed→Refunded)
        /// - Update payment status
        /// - Recalculate invoice status based on all payments
        /// - Return updated response
        /// </summary>
        Task<PaymentResponse> UpdatePaymentStatusAsync(Guid paymentId, PaymentStatus newStatus);

        /// <summary>
        /// Refund a payment
        /// - Check if payment status is Completed
        /// - Set status to Refunded
        /// - Update invoice status (recalculate paid amount)
        /// - Send refund confirmation email to patient
        /// - Return success
        /// </summary>
        Task<bool> RefundPaymentAsync(Guid paymentId, string? reason = null);

        /// <summary>
        /// Get payment statistics
        /// - Total payments in date range
        /// - Total amount collected (completed payments only)
        /// - Group by payment method (count and amount)
        /// - Group by status (count and amount)
        /// - Average payment amount
        /// - Return statistics object
        /// </summary>
        Task<object> GetPaymentStatisticsAsync(DateTime fromDate, DateTime toDate);
    }
}