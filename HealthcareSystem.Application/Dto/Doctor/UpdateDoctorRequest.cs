// UpdateDoctorRequest.cs
using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.Application.Dto.Doctor
{
    public class UpdateDoctorRequest
    {
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Specialization must be between 2 and 100 characters")]
        public string? Specialization { get; set; }

        [StringLength(500, ErrorMessage = "Qualification cannot exceed 500 characters")]
        public string? Qualification { get; set; }

        [Range(0, 70, ErrorMessage = "Experience years must be between 0 and 70")]
        public int? ExperienceYears { get; set; }

        [Range(0.01, 100000, ErrorMessage = "Consultation fee must be between 0.01 and 100000")]
        public decimal? ConsultationFee { get; set; }

        [StringLength(2000, ErrorMessage = "Bio cannot exceed 2000 characters")]
        public string? Bio { get; set; }

        public bool? IsAvailableForBooking { get; set; }
    }
}