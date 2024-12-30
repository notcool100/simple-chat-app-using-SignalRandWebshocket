using System.Net.WebSockets;
using System.Text;

public class WebSocketManager
{
    private readonly RequestDelegate _next;
    private static readonly Dictionary<string, WebSocket> WebSocketConnections = new();

    public WebSocketManager(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Path == "/ws")
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                string username = context.Request.Query["username"];
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                WebSocketConnections[username] = webSocket;

                await HandleConnection(username, webSocket);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }
        else
        {
            await _next(context);
        }
    }

    private async Task HandleConnection(string username, WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var parts = receivedMessage.Split(":", 2);
                var toUser = parts[0];
                var message = parts[1];

                if (WebSocketConnections.TryGetValue(toUser, out var recipientSocket))
                {
                    var outgoingMessage = $"{username}: {message}";
                    var bytes = Encoding.UTF8.GetBytes(outgoingMessage);
                    await recipientSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                else
                {
                    var errorMessage = Encoding.UTF8.GetBytes($"User '{toUser}' is not connected.");
                    await webSocket.SendAsync(new ArraySegment<byte>(errorMessage), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                WebSocketConnections.Remove(username);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by user", CancellationToken.None);
            }
        }
    }
}
