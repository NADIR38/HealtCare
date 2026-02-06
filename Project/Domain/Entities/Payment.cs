using HealthcareSystem.Domain.Enums;
using System;

namespace HealthcareSystem.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? TransactionId { get; set; } // For online payments
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }

        // Navigation properties
        public Invoice Invoice { get; set; } = null!;
        public User CreatedByUser { get; set; } = null!;
    }
}