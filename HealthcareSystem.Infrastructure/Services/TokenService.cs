using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configurations;
        public TokenService(IConfiguration configuration)
        {
            _configurations = configuration;
        }
        public string GenerateAccessToken(User user, string role)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configurations["JwtSettings:SecretKey"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email,user.Email),
            new Claim(ClaimTypes.Name,user.FirstName),
            new Claim(ClaimTypes.Role,role),
            new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
            };
            var token = new JwtSecurityToken(
                issuer: _configurations["JwtSettings:Issuer"],
                audience: _configurations["JwtSettings:Audience"],
                claims: claims,
                expires:DateTime.UtcNow.AddHours(
                    double.Parse(_configurations["JwtSettings:ExpiryInHours"]!)
                    ),
                signingCredentials: credentials
               

                );
            return new JwtSecurityTokenHandler().WriteToken(token);

        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public Guid? ValidateToken(string token)
        {
            //tokenhandler
            //key configurations
            //tokenhandler.validatetoken
            //jwttoken.validated token
            //userid
            //return userid 

            var tokenHandler=new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configurations["JwtSettings:SecurityKey"]!);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateAudience = true,
                    ValidAudience= _configurations["JwtSettings:Audience"],
                    ValidateIssuer = true,
                    ValidIssuer = _configurations["JwtSettings:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime=true,
                    ClockSkew=TimeSpan.Zero,

                },
                out SecurityToken validatedToken


                    );
                var securityToken = (JwtSecurityToken)validatedToken;
                var userId = Guid.Parse(securityToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value);
                return userId;
            }
            catch {
                return null;
            
            }
        }
    }
}
