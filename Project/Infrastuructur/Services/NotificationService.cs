using HealthcareSystem.Application.DTOs.Notification;
using HealthcareSystem.Application.Hubs;
using HealthcareSystem.Application.Interfaces;
using HealthcareSystem.Domain.Entities;
using HealthcareSystem.Domain.Enums;
using HealthcareSystem.Infrastructure.Data;
using HealthcareSystem.Infrastructure.Helpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthcareSystem.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext context,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<NotificationResponse> CreateNotificationAsync(CreateNotificationRequest request)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Type = request.Type,
                Title = request.Title,
                Message = request.Message,
                ActionUrl = request.ActionUrl,
                RelatedEntityId = request.RelatedEntityId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Notification created for user {UserId}: {Title}",
                request.UserId, request.Title);

            return MapToResponse(notification);
        }

        public async Task SendNotificationAsync(Guid userId, NotificationType type, string title,
            string message, string? actionUrl = null, string? relatedEntityId = null)
        {
            try
            {
                // Save to database
                var request = new CreateNotificationRequest
                {
                    UserId = userId,
                    Type = type,
                    Title = title,
                    Message = message,
                    ActionUrl = actionUrl,
                    RelatedEntityId = relatedEntityId
                };

                var notification = await CreateNotificationAsync(request);

                // Send real-time notification via SignalR
                await _hubContext.Clients.Group($"user_{userId}")
                    .SendAsync("ReceiveNotification", new
                    {
                        id = notification.Id,
                        type = notification.Type.ToString(),
                        title = notification.Title,
                        message = notification.Message,
                        actionUrl = notification.ActionUrl,
                        relatedEntityId = notification.RelatedEntityId,
                        createdAt = notification.CreatedAt
                    });

                _logger.LogInformation("Real-time notification sent to user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
            }
        }

        public async Task BroadcastNotificationAsync(List<Guid> userIds, NotificationType type,
            string title, string message, string? actionUrl = null, string? relatedEntityId = null)
        {
            foreach (var userId in userIds)
            {
                await SendNotificationAsync(userId, type, title, message, actionUrl, relatedEntityId);
            }
        }

        public async Task BroadcastToAdminsAsync(NotificationType type, string title, string message,
            string? actionUrl = null, string? relatedEntityId = null)
        {
            try
            {
                // Get all admin users
                var adminUsers = await _context.Users
                    .Where(u => u.UserRoles.Any(r => r.Role == Role.Admin))
                    .Select(u => u.Id)
                    .ToListAsync();

                await BroadcastNotificationAsync(adminUsers, type, title, message, actionUrl, relatedEntityId);

                // Also broadcast to admin group
                await _hubContext.Clients.Group("admins")
                    .SendAsync("ReceiveNotification", new
                    {
                        type = type.ToString(),
                        title,
                        message,
                        actionUrl,
                        relatedEntityId,
                        createdAt = DateTime.UtcNow
                    });

                _logger.LogInformation("Notification broadcasted to {Count} admins", adminUsers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting notification to admins");
            }
        }

        public async Task<List<NotificationResponse>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(50) // Limit to last 50 notifications
                .ToListAsync();

            return notifications.Select(MapToResponse).ToList();
        }

        public async Task<NotificationResponse> MarkAsReadAsync(Guid notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null)
            {
                throw new NotFoundException("Notification", notificationId);
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Notification {NotificationId} marked as read", notificationId);

            return MapToResponse(notification);
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Marked {Count} notifications as read for user {UserId}",
                notifications.Count, userId);
        }

        public async Task<bool> DeleteNotificationAsync(Guid notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null)
            {
                throw new NotFoundException("Notification", notificationId);
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Notification {NotificationId} deleted", notificationId);

            return true;
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<int> DeleteOldNotificationsAsync(int olderThanDays = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

            var oldNotifications = await _context.Notifications
                .Where(n => n.IsRead && n.ReadAt < cutoffDate)
                .ToListAsync();

            _context.Notifications.RemoveRange(oldNotifications);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted {Count} old notifications", oldNotifications.Count);

            return oldNotifications.Count;
        }

        private NotificationResponse MapToResponse(Notification notification)
        {
            return new NotificationResponse
            {
                Id = notification.Id,
                Type = notification.Type,
                Title = notification.Title,
                Message = notification.Message,
                ActionUrl = notification.ActionUrl,
                RelatedEntityId = notification.RelatedEntityId,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                ReadAt = notification.ReadAt
            };
        }
    }
}