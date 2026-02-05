using System;

namespace HealthcareSystem.Application.DTOs.Invoice
{
    public class UpdateInvoiceRequest
    {
        public DateTime? DueDate { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? Notes { get; set; }
    }
}