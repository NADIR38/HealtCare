using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Domain.Entities
{
    public class MedicalRecord
    {
        public Guid Id {  get; set; }
        public Guid PatientId { get; set; }
        public Guid? AppointmentId { get; set; }
        public Guid DoctorId { get; set; }
        public DateTime VisitDate { get; set; }
        public string? ChiefComplaint {  get; set; }
        public string? Diagnosis { get; set; }
        public List<string> VitalSigns=new List<string>();
        public string? Notes {  get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public User User { get; set; } = null!;
        public Patient Patient { get; set; } = null!;
        public Appointment? Appointment { get; set; }
        public Doctor Doctor { get; set; } = null!;


    }
}
