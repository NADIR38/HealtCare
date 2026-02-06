using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.Application.DTOs.Prescription
{
    public class CreatePrescriptionRequest
    {
        [Required]
        public Guid MedicalRecordId { get; set; }

        public DateTime? ValidUntil { get; set; }
        public string? Notes { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one medication is required")]
        public List<PrescriptionItemRequest> Items { get; set; } = new List<PrescriptionItemRequest>();
    }

    public class PrescriptionItemRequest
    {
        [Required]
        public string MedicineName { get; set; } = string.Empty;

        [Required]
        public string Dosage { get; set; } = string.Empty;

        [Required]
        public string Frequency { get; set; } = string.Empty;

        [Required]
        public string Duration { get; set; } = string.Empty;

        public int? Quantity { get; set; }
        public string? Instructions { get; set; }
    }
}