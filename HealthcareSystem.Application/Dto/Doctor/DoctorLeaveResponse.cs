using HealthcareSystem.Domain.Entities;
using HealthcareSystem.Domain.Enums;
using System;

namespace HealthcareSystem.Application.DTOs.Doctor
{
    public class DoctorLeaveResponse
    {
        public Guid Id { get; set; }
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Reason { get; set; }
        public LeaveStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}