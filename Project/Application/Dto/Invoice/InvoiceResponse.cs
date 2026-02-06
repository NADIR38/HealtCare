using HealthcareSystem.Domain.Enums;
using System;
using System.Collections.Generic;

namespace HealthcareSystem.Application.DTOs.Invoice
{
    public class InvoiceResponse
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;

        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientNumber { get; set; } = string.Empty;

        public Guid? AppointmentId { get; set; }
        public string? AppointmentNumber { get; set; }

        public DateTime InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public InvoiceStatus Status { get; set; }

        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }

        public string? Notes { get; set; }

        public List<InvoiceItemResponse> Items { get; set; } = new List<InvoiceItemResponse>();
        public List<PaymentSummary> Payments { get; set; } = new List<PaymentSummary>();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
    }

    public class InvoiceItemResponse
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ItemType { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }

    public class PaymentSummary
    {
        public Guid Id { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}