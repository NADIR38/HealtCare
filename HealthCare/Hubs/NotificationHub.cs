using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HealthcareSystem.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private static readonly Dictionary<string, string> _userConnections = new();

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();

            if (!string.IsNullOrEmpty(userId))
            {
                _userConnections[userId] = Context.ConnectionId;
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

                Console.WriteLine($"User {userId} connected with ConnectionId {Context.ConnectionId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();

            if (!string.IsNullOrEmpty(userId))
            {
                _userConnections.Remove(userId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

                Console.WriteLine($"User {userId} disconnected");
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinDoctorGroup(string doctorId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"doctor_{doctorId}");
        }

        public async Task LeaveDoctorGroup(string doctorId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"doctor_{doctorId}");
        }

        public async Task JoinAdminGroup()
        {
            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
            }
        }

        public async Task MarkNotificationAsRead(string notificationId)
        {
            await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
        }

        private string GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                              ?? Context.User?.FindFirst("sub")?.Value;

            return userIdClaim ?? string.Empty;
        }

        public static string? GetConnectionId(string userId)
        {
            return _userConnections.TryGetValue(userId, out var connectionId) ? connectionId : null;
        }
    }
}