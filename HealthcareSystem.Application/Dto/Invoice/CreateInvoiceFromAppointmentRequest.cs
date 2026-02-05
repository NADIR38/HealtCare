using System;
using System.Collections.Generic;

namespace HealthcareSystem.Application.DTOs.Invoice
{
    public class CreateInvoiceFromAppointmentRequest
    {
        public Guid AppointmentId { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? Notes { get; set; }

        // Optional: Additional items beyond consultation fee
        public List<InvoiceItemRequest>? AdditionalItems { get; set; }
    }
}