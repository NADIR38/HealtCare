using HealthcareSystem.Application.DTOs.User;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Domain.Entities;
using HealthcareSystem.Domain.Enums;
using HealthcareSystem.Infrastructure.Data;
using HealthcareSystem.Infrastructure.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthcareSystem.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<UserService> _logger;

        public UserService(
            ApplicationDbContext context,
            IPasswordHasher passwordHasher,
            ILogger<UserService> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<List<UserResponse>> GetAllUsersAsync(int page, int pageSize, Role? role = null)
        {
            var query = _context.Users
                .Include(u => u.UserRoles)
                .AsQueryable();

            if (role.HasValue)
            {
                query = query.Where(u => u.UserRoles.Any(r => r.Role == role.Value));
            }

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return users.Select(MapToResponse).ToList();
        }

        public async Task<UserResponse> GetUserByIdAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            return MapToResponse(user);
        }

        public async Task<UserResponse> CreateUserAsync(CreateUserRequest request)
        {
            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                throw new DuplicateException($"User with email {request.Email} already exists");
            }

            // Validate roles
            var validRoles = request.Roles
                .Select(r => Enum.TryParse<Role>(r, out var role) ? role : (Role?)null)
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .Distinct()
                .ToList();

            if (validRoles.Count == 0)
            {
                throw new ValidationException("At least one valid role must be provided");
            }

            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Hash password
            user.PasswordHash = _passwordHasher.HashPassword( request.Password);

            // Add user to database
            _context.Users.Add(user);

            // Assign roles
            foreach (var role in validRoles)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    Role = role
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("User created: {Email} with roles: {Roles}",
                user.Email, string.Join(", ", validRoles));

            return await GetUserByIdAsync(user.Id);
        }

        public async Task<UserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            // Update user details
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;
            user.Gender = request.Gender;
            user.DateOfBirth = request.DateOfBirth;
            user.UpdatedAt = DateTime.UtcNow;

            // Update roles
            // Remove existing roles
            var existingRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

            _context.UserRoles.RemoveRange(existingRoles);

            // Add new roles
            var validRoles = request.Roles
                .Select(r => Enum.TryParse<Role>(r, out var role) ? role : (Role?)null)
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .Distinct()
                .ToList();

            if (validRoles.Count == 0)
            {
                throw new ValidationException("At least one valid role must be provided");
            }

            foreach (var role in validRoles)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = userId,
                    Role = role
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("User updated: {UserId}", userId);

            return await GetUserByIdAsync(userId);
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            // Check if user has doctor profile
            var hasDoctor = await _context.Doctor.AnyAsync(d => d.UserId == userId);
            if (hasDoctor)
            {
                throw new BusinessException("Cannot delete user with doctor profile. Delete doctor profile first.");
            }

            // Check if user has patient profile
            var hasPatient = await _context.Patients.AnyAsync(p => p.UserId == userId);
            if (hasPatient)
            {
                throw new BusinessException("Cannot delete user with patient profile. Delete patient profile first.");
            }

            // Remove roles first
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

            _context.UserRoles.RemoveRange(userRoles);

            // Remove user
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            _logger.LogInformation("User deleted: {UserId}", userId);

            return true;
        }

        public async Task<UserResponse> AssignRoleAsync(Guid userId, Role role)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            // Check if role already assigned
            var hasRole = user.UserRoles.Any(r => r.Role == role);
            if (hasRole)
            {
                throw new ValidationException($"User already has {role} role");
            }

            // Add role
            _context.UserRoles.Add(new UserRole
            {
                UserId = userId,
                Role = role
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation("Role {Role} assigned to user {UserId}", role, userId);

            return await GetUserByIdAsync(userId);
        }

        public async Task<UserResponse> RemoveRoleAsync(Guid userId, Role role)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new NotFoundException("User", userId);
            }

            // Check if user has only one role
            if (user.UserRoles.Count == 1)
            {
                throw new ValidationException("Cannot remove the last role from user");
            }

            // Find and remove role
            var userRole = user.UserRoles.FirstOrDefault(r => r.Role == role);
            if (userRole == null)
            {
                throw new ValidationException($"User does not have {role} role");
            }

            _context.UserRoles.Remove(userRole);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Role {Role} removed from user {UserId}", role, userId);

            return await GetUserByIdAsync(userId);
        }

        public async Task<List<UserResponse>> GetAvailableUsersAsync(Role? targetRole = null)
        {
            var query = _context.Users
                .Include(u => u.UserRoles)
                .AsQueryable();

            if (targetRole.HasValue)
            {
                if (targetRole.Value == Role.Doctor)
                {
                    // Get users with Doctor role but no doctor profile
                    var doctorUserIds = await _context.Doctor
                        .Select(d => d.UserId)
                        .ToListAsync();

                    query = query.Where(u =>
                        u.UserRoles.Any(r => r.Role == Role.Doctor) &&
                        !doctorUserIds.Contains(u.Id)
                    );
                }
                else if (targetRole.Value == Role.Patient)
                {
                    var patientUserIds = await _context.Patients
                        .Select(p => p.UserId)
                        .ToListAsync();

                    query = query.Where(u =>
                        u.UserRoles.Any(r => r.Role == Role.Patient) &&
                        !patientUserIds.Contains(u.Id)
                    );
                }
            }

            var users = await query
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            return users.Select(MapToResponse).ToList();
        }

        private UserResponse MapToResponse(User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth ?? DateTime.MinValue,
                Roles = user.UserRoles.Select(r => r.Role).ToList(),
                CreatedAt = user.CreatedAt
            };
        }
    }
}