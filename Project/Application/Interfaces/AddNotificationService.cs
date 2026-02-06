using HealthcareSystem.Application.DTOs.Notification;
using HealthcareSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthcareSystem.Application.Interfaces
{
    public interface INotificationService
    {
        /// <summary>
        /// Create and send a notification to a user
        /// </summary>
        Task<NotificationResponse> CreateNotificationAsync(CreateNotificationRequest request);

        /// <summary>
        /// Send real-time notification via SignalR and save to database
        /// </summary>
        Task SendNotificationAsync(Guid userId, NotificationType type, string title, string message,
            string? actionUrl = null, string? relatedEntityId = null);

        /// <summary>
        /// Broadcast notification to multiple users
        /// </summary>
        Task BroadcastNotificationAsync(List<Guid> userIds, NotificationType type, string title, string message,
            string? actionUrl = null, string? relatedEntityId = null);

        /// <summary>
        /// Broadcast to all admins
        /// </summary>
        Task BroadcastToAdminsAsync(NotificationType type, string title, string message,
            string? actionUrl = null, string? relatedEntityId = null);

        /// <summary>
        /// Get all notifications for a user
        /// </summary>
        Task<List<NotificationResponse>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false);

        /// <summary>
        /// Mark notification as read
        /// </summary>
        Task<NotificationResponse> MarkAsReadAsync(Guid notificationId);

        /// <summary>
        /// Mark all notifications as read for a user
        /// </summary>
        Task MarkAllAsReadAsync(Guid userId);

        /// <summary>
        /// Delete a notification
        /// </summary>
        Task<bool> DeleteNotificationAsync(Guid notificationId);

        /// <summary>
        /// Get unread notification count for a user
        /// </summary>
        Task<int> GetUnreadCountAsync(Guid userId);

        /// <summary>
        /// Delete all read notifications older than specified days
        /// </summary>
        Task<int> DeleteOldNotificationsAsync(int olderThanDays = 30);
    }
}