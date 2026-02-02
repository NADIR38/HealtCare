using HealthcareSystem.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;


namespace HealthcareSystem.Application.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        private readonly PasswordHasher<object> _passwordHasher;
        public PasswordHasher()
        {
            _passwordHasher = new PasswordHasher<object>();
        }
        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) {
                Console.WriteLine("Password is Null ");
            }
            return _passwordHasher.HashPassword(null!, password);
        }

        public bool VerifyPassword(string password, string HashedPassword)
        {
            var result=_passwordHasher.VerifyHashedPassword(null!,password, HashedPassword);
            return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
