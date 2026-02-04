using System;
using System.Collections.Generic;

namespace HealthcareSystem.Application.DTOs.Prescription
{
    public class PrescriptionResponse
    {
        public Guid Id { get; set; }
        public string PrescriptionNumber { get; set; } = string.Empty;

        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;

        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;

        public DateTime PrescriptionDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string? Notes { get; set; }

        public List<PrescriptionItemResponse> Items { get; set; } = new List<PrescriptionItemResponse>();

        public DateTime CreatedAt { get; set; }
    }

    public class PrescriptionItemResponse
    {
        public Guid Id { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public int? Quantity { get; set; }
        public string? Instructions { get; set; }
    }
}