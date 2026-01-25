using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace AuthAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        // Store connected users (userId -> connectionId)
        private static readonly ConcurrentDictionary<int, string> ConnectedUsers = new();

        public override async Task OnConnectedAsync()
        {
            var userId = int.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            ConnectedUsers.TryAdd(userId, Context.ConnectionId);

            await Clients.All.SendAsync("UserConnected", userId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = int.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            ConnectedUsers.TryRemove(userId, out _);

            await Clients.All.SendAsync("UserDisconnected", userId);
            await base.OnDisconnectedAsync(exception);
        }

        // Send message to specific user
        public async Task SendMessageToUser(int receiverId, string message)
        {
            var senderId = int.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (ConnectedUsers.TryGetValue(receiverId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", senderId, message, DateTime.UtcNow);
            }

            // Also send to sender for confirmation
            await Clients.Caller.SendAsync("MessageSent", receiverId, message, DateTime.UtcNow);
        }

        // Typing indicator
        public async Task UserTyping(int receiverId)
        {
            var senderId = int.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (ConnectedUsers.TryGetValue(receiverId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("UserTyping", senderId);
            }
        }

        // Stop typing indicator
        public async Task UserStoppedTyping(int receiverId)
        {
            var senderId = int.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (ConnectedUsers.TryGetValue(receiverId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("UserStoppedTyping", senderId);
            }
        }

        // Match notification
        public async Task NotifyMatch(int userId1, int userId2)
        {
            if (ConnectedUsers.TryGetValue(userId1, out var connectionId1))
            {
                await Clients.Client(connectionId1).SendAsync("NewMatch", userId2);
            }

            if (ConnectedUsers.TryGetValue(userId2, out var connectionId2))
            {
                await Clients.Client(connectionId2).SendAsync("NewMatch", userId1);
            }
        }

        // Check if user is online
        public Task<bool> IsUserOnline(int userId)
        {
            return Task.FromResult(ConnectedUsers.ContainsKey(userId));
        }
    }
}