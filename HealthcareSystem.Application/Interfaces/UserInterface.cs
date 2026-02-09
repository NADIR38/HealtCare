using HealthcareSystem.Application.DTOs.User;
using HealthcareSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// Get all users with optional role filter
        /// </summary>
        Task<List<UserResponse>> GetAllUsersAsync(int page, int pageSize, Role? role = null);

        /// <summary>
        /// Get user by ID
        /// </summary>
        Task<UserResponse> GetUserByIdAsync(Guid userId);

        /// <summary>
        /// Create new user with roles (Admin only)
        /// </summary>
        Task<UserResponse> CreateUserAsync(CreateUserRequest request);

        /// <summary>
        /// Update user details and roles
        /// </summary>
        Task<UserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request);

        /// <summary>
        /// Delete user
        /// </summary>
        Task<bool> DeleteUserAsync(Guid userId);

        /// <summary>
        /// Assign role to user
        /// </summary>
        Task<UserResponse> AssignRoleAsync(Guid userId, Role role);

        /// <summary>
        /// Remove role from user
        /// </summary>
        Task<UserResponse> RemoveRoleAsync(Guid userId, Role role);

        /// <summary>
        /// Get users available for doctor/patient profile creation
        /// </summary>
        Task<List<UserResponse>> GetAvailableUsersAsync(Role? targetRole = null);
    }
}