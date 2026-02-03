using HealthcareSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Domain.Entities
{
   public class Patient
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string PatientNumber { get; set; } = string.Empty;
        public BloodGroup? BloodGroup { get; set; }
        public decimal? Height { get; set; } // in cm
        public decimal? Weight { get; set; } // in kg
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelation { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? InsuranceProvider { get; set; }
        public string? InsurancePolicyNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public User User { get; set; } = null!;
        public MedicalHistory? MedicalHistory { get; set; }
        //public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        //public ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
        [NotMapped]
        public ICollection<Document> Documents { get; set; } = new List<Document>();

    }
   
}
