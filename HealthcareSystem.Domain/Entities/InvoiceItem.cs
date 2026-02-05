using System;

namespace HealthcareSystem.Domain.Entities
{
    public class InvoiceItem
    {
        public Guid Id { get; set; }
        public Guid InvoiceId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ItemType { get; set; } // "Consultation", "Lab Test", "Procedure", "Medication"
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; } // Quantity * UnitPrice

        // Navigation property
        public Invoice Invoice { get; set; } = null!;
    }
}