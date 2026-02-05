using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.Application.DTOs.Invoice
{
    public class CreateInvoiceRequest
    {
        [Required]
        public Guid PatientId { get; set; }

        public Guid? AppointmentId { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; }

        public DateTime? DueDate { get; set; }

        public decimal? TaxAmount { get; set; }

        public decimal? DiscountAmount { get; set; }

        public string? Notes { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<InvoiceItemRequest> Items { get; set; } = new List<InvoiceItemRequest>();
    }

    public class InvoiceItemRequest
    {
        [Required]
        public string Description { get; set; } = string.Empty;

        public string? ItemType { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }
    }
}