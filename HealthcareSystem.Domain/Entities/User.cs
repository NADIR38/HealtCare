using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Domain.Entities
{
    public  class User
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }=string.Empty;
        public string Email { get; set; }=string.Empty;

        public string Password { get; set; }=string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string LastName { get; set; }=string.Empty;
        public string PhoneNumber { get; set; }=string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }=DateTime.Now;
        public DateTime UpdatedAt { get; set; } =DateTime.Now;
        public Gender Gender { get; set; }
         public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();


    }
    public enum Gender { Male, Female, Other }

}
