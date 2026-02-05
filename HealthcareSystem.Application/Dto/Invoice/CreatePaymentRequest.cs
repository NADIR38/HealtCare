using HealthcareSystem.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.Application.DTOs.Payment
{
    public class CreatePaymentRequest
    {
        [Required]
        public Guid InvoiceId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        public string? TransactionId { get; set; }

        public string? Notes { get; set; }
    }
    public class UpdatePaymentStatusRequest
    {
        public PaymentStatus NewStatus { get; set; }
    }

    public class RefundPaymentRequest
    {
        public string? Reason { get; set; }
    }
}