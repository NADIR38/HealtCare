using HealthcareSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user, string role);
        string GenerateRefreshToken();
        Guid? ValidateToken(string token);
    }
}
