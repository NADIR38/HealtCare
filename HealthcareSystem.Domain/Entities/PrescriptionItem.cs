using System;

namespace HealthcareSystem.Domain.Entities
{
    public class PrescriptionItem
    {
        public Guid Id { get; set; }
        public Guid PrescriptionId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty; // e.g., "500mg"
        public string Frequency { get; set; } = string.Empty; // e.g., "Twice daily"
        public string Duration { get; set; } = string.Empty; // e.g., "7 days"
        public int? Quantity { get; set; }
        public string? Instructions { get; set; } // e.g., "Take with food"

        // Navigation property
        public Prescription Prescription { get; set; } = null!;
    }
}