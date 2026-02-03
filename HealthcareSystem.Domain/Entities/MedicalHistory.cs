using HealthcareSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Domain.Entities
{
    public class MedicalHistory
    {
        public Guid Id {  get; set; }
        public Guid PatientId { get; set; }
        public List<string>ChronicConditions= new List<string>();
        public List<string>PastSurgeries= new List<string>();
        public List<string> FamilyHistory= new List<string>();
        public List<string>CurrentMedications= new List<string>();
        public List<string>Allergies= new List<string>();
        public SmokingStatus SmokingStatus {  get; set; }
        public AlcoholConsumption AlcoholConsumption {  get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation property
        public Patient Patient { get; set; } = null!;

    }
}
