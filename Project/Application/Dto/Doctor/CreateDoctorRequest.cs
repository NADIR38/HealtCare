// CreateDoctorRequest.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.Application.Dto.Doctor
{
    public class CreateDoctorRequest
    {
        [Required(ErrorMessage = "User ID is required")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Specialization is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Specialization must be between 2 and 100 characters")]
        public string Specialization { get; set; } = string.Empty;

        [Required(ErrorMessage = "License number is required")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "License number must be between 5 and 100 characters")]
        public string LicenseNumber { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Qualification cannot exceed 500 characters")]
        public string? Qualification { get; set; }

        [Range(0, 70, ErrorMessage = "Experience years must be between 0 and 70")]
        public int? ExperienceYears { get; set; }

        [Required(ErrorMessage = "Consultation fee is required")]
        [Range(0.01, 100000, ErrorMessage = "Consultation fee must be between 0.01 and 100000")]
        public decimal ConsultationFee { get; set; }

        [StringLength(2000, ErrorMessage = "Bio cannot exceed 2000 characters")]
        public string? Bio { get; set; }
    }
}