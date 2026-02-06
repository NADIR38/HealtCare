using HealthcareSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Dto.Patient
{
    public class MedicalHistoryResponse
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public List<string> ChronicConditions { get; set; } = new List<string>();
        public List<string> Allergies { get; set; } = new List<string>();
        public List<string> PastSurgeries { get; set; } = new List<string>();
        public List<string> FamilyHistory { get; set; } = new List<string>();
        public List<string> CurrentMedications { get; set; } = new List<string>();
        public SmokingStatus SmokingStatus { get; set; }
        public AlcoholConsumption AlcoholConsumption { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
