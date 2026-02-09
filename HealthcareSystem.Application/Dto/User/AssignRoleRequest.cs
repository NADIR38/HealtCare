using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.Application.DTOs.User
{
    public class AssignRoleRequest
    {
        [Required]
        public string Role { get; set; } = string.Empty;
    }
}