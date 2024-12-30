using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace signalR.Controllers
{
    public class ChatHub : Hub
    {
        // Dictionary to track users and their connection IDs
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();
        // Handle user joining the chat
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        // Handle user disconnecting
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (user != null)
            {
                UserConnections.TryRemove(user, out _);
                await Clients.All.SendAsync("UserDisconnected", user);
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Register user with connection ID
        public async Task RegisterUser(string username)
        {
            UserConnections[username] = Context.ConnectionId;
            await Clients.All.SendAsync("UserConnected", username);
        }

        // Send private message
        public async Task SendPrivateMessage(string fromUser, string toUser, string message)
        {
            if (UserConnections.TryGetValue(toUser, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceivePrivateMessage", fromUser, message);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", $"User '{toUser}' is not online.");
            }
        }
    }
}
