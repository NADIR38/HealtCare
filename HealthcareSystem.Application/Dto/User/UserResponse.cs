using HealthcareSystem.Domain.Entities;
using HealthcareSystem.Domain.Enums;
using System;
using System.Collections.Generic;

namespace HealthcareSystem.Application.DTOs.User
{
    public class UserResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public Gender Gender { get; set; } 
        public DateTime DateOfBirth { get; set; }
        public List<Role> Roles { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}