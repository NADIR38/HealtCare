using System;
using System.Collections.Generic;

namespace HealthcareSystem.Domain.Entities
{
    public class Prescription
    {
        public Guid Id { get; set; }
        public Guid MedicalRecordId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public string PrescriptionNumber { get; set; } = string.Empty;
        public DateTime PrescriptionDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public MedicalRecord MedicalRecord { get; set; } = null!;
        public Patient Patient { get; set; } = null!;
        public Doctor Doctor { get; set; } = null!;
        public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
    }
}