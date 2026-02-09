using HealthcareSystem.Domain.Entities;
using HealthcareSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthcareSystem.Application.DTOs.User
{
    public class UpdateUserRequest
    {
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
        public List<Role> Roles { get; set; } = new();
    }
}