using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Dto.Doctor
{
    public class DoctorResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string DoctorNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Specialization { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string? Qualification { get; set; }
        public int? ExperienceYears { get; set; }
        public decimal ConsultationFee { get; set; }
        public string? Bio { get; set; }
        public bool IsAvailableForBooking { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<DoctorScheduleResponse> Schedules { get; set; } = new List<DoctorScheduleResponse>();
    }
}
