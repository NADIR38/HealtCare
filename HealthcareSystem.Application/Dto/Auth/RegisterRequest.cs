using HealthcareSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Dto.Auth
{
    public  class RegisterRequest
    {
        public string FirstName { get; set; }= string.Empty;
        public string LastName { get; set; }=string.Empty;
        public string Email { get; set; }= string.Empty;
        public string PasswordHash { get; set; }= string.Empty;
        public string PhoneNumber { get; set; }=string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public Role Role { get; set; }
    }
}
