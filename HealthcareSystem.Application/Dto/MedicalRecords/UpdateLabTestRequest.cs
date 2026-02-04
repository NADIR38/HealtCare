using HealthcareSystem.Domain.Enums;

namespace HealthcareSystem.Application.DTOs.LabTest
{
    public class UpdateLabTestRequest
    {
        public LabTestStatus? Status { get; set; }
        public string? Results { get; set; }
        public string? Notes { get; set; }
    }
}