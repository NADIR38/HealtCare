using System;

namespace HealthcareSystem.Domain.Entities
{
    public class VitalSigns
    {
        public Guid Id { get; set; }
        public Guid MedicalRecordId { get; set; }

        // Vital measurements
        public string? BloodPressureSystolic { get; set; } // e.g., "120"
        public string? BloodPressureDiastolic { get; set; } // e.g., "80"
        public decimal? Temperature { get; set; } // in Fahrenheit or Celsius
        public int? HeartRate { get; set; } // beats per minute
        public int? RespiratoryRate { get; set; } // breaths per minute
        public decimal? OxygenSaturation { get; set; } // SpO2 percentage
        public decimal? Weight { get; set; } // in kg
        public decimal? Height { get; set; } // in cm
        public decimal? BMI { get; set; } // Body Mass Index
        public string? Notes { get; set; }
        public DateTime RecordedAt { get; set; }

        // Navigation property
        public MedicalRecord MedicalRecord { get; set; } = null!;
    }
}