using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HealthcareSystem.Application.Dto.Auth;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Domain.Entities;
using HealthcareSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthcareSystem.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenservice;
        public AuthService(ApplicationDbContext context, IPasswordHasher passwordHasher, ITokenService tokenservice)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenservice = tokenservice;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            //finduser
            //checks and verify password
            //check role default patient
            //tokens
            //addtoken to db
            //save changes
            //return authresponse
var user=await _context.Users.Include(u=>u.UserRoles).FirstOrDefaultAsync(u=>u.Email==request.Email);
            if (user == null)
            {
                throw new Exception("User Already Exists");
            }
            if (user.IsActive != true)
            {
                throw new Exception("user Account is Deactivated");
            }
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new Exception("Incorrect password");
            }
            var roles = user.UserRoles.FirstOrDefault()?.Role.ToString() ?? "Patient";
            var accessToken=_tokenservice.GenerateAccessToken(user,roles);
            var refreshToken = _tokenservice.GenerateRefreshToken();
            var token = new RefreshToken { 
            ExpiresAt= DateTime.UtcNow.AddDays(2),
            CreatedAt= DateTime.UtcNow,
            Token=refreshToken,
            Id=Guid.NewGuid(),
            UserId=user.Id,
            };
            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();
            return new AuthResponse
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserId = user.Id,
                Token = accessToken,
                RefreshToken = refreshToken,
                Role = roles,
                ExpiresAt=DateTime.UtcNow.AddHours(2),


            };


        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null || storedToken.RevokedAt != null)
            {
                throw new Exception("Invalid refresh token");
            }

            if (storedToken.ExpiresAt < DateTime.UtcNow)
            {
                throw new Exception("Refresh token expired");
            }

            var user = storedToken.User;
            var userRole = user.UserRoles.FirstOrDefault()?.Role.ToString() ?? "Patient";

            // Generate new tokens
            var newAccessToken = _tokenservice.GenerateAccessToken(user, userRole);
            var newRefreshToken = _tokenservice.GenerateRefreshToken();

            // Revoke old token
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.ReplacedByToken = newRefreshToken;

            // Create new refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(newRefreshTokenEntity);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = userRole,
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(2)
            };
        }
        public async Task<bool> LogoutAsync(string refreshToken)
        {
            // 1. Database se wo token dhundein jo active ho
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.RevokedAt == null);

            if (storedToken == null)
            {
                return false; // Token pehle hi revoked hai ya exist nahi karta
            }

            // 2. Token ko revoke karein
            storedToken.RevokedAt = DateTime.UtcNow;

            // 3. Save changes
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Check if user already exists
            // Create new user
            // AssignRole
            //generate access and refereshtoke
            //store token
            //Save
            //Authresponse return
            var user=await _context.Users.AnyAsync(x=>x.Email==request.Email);
            if (user)
            {
                throw new Exception("User Already exist Login");
            }
          var  newUser = new User { 
              Id=Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword( request.PasswordHash),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Gender = request.Gender,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            IsActive=true,
            CreatedAt=DateTime.UtcNow,
            UpdatedAt=DateTime.UtcNow,
            //Password=request.PasswordHash
            
            };
             _context.Users.Add(newUser);
            var roles = new UserRole { 
            Id= Guid.NewGuid(),
            UserId=newUser.Id,
            AssignedAt=DateTime.UtcNow,
            Role=request.Role,
            };
            _context.UserRoles.Add(roles);
            var accessToken = _tokenservice.GenerateAccessToken(newUser, request.Role.ToString());
            var refreshToken = _tokenservice.GenerateRefreshToken();

            var refreshTokenIdentity = new RefreshToken { 
            Id= Guid.NewGuid(),
            UserId= newUser.Id,
            Token=refreshToken,
            ExpiresAt=DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,

            };
            _context.RefreshTokens.Add(refreshTokenIdentity);
             await _context.SaveChangesAsync();
            var AuthResponse = new AuthResponse
            {
                UserId = newUser.Id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Role = request.Role.ToString(),
                Token = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(2)



            };
            return AuthResponse;





        }
    }
}
