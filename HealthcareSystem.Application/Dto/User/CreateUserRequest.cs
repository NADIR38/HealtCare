using HealthcareSystem.Domain.Entities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.Application.DTOs.User
{
    public class CreateUserRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public Gender Gender { get; set; } 

        [Required]
        public DateTime DateOfBirth { get; set; }


        [Required]
        [MinLength(1)]
        public List<string> Roles { get; set; } = new();
    }
}