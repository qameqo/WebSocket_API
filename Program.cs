using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
//app.UseWebSockets();
//app.Map("/ws", async context =>
//{
//    if (context.WebSockets.IsWebSocketRequest)
//    {
//        using var ws = await context.WebSockets.AcceptWebSocketAsync();
//        while (true)
//        {
//            var message = "Hi !!!";
//            var bytes = Encoding.UTF8.GetBytes(message);
//            var arrSegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
//            if (ws.State == WebSocketState.Open)
//            {
//                await ws.SendAsync(arrSegment, WebSocketMessageType.Text, true, CancellationToken.None);
//            }
//            else if (ws.State == WebSocketState.Closed || ws.State == WebSocketState.Aborted)
//            {
//                break;
//            }
//            Thread.Sleep(1000);
//        }
//    }
//    else
//    {
//        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
//    }
//});
//await app.RunAsync();
var app = builder.Build();

app.UseWebSockets();
var connectedClients = new ConcurrentBag<System.Net.WebSockets.WebSocket>();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        connectedClients.Add(webSocket);
        await HandleWebSocketAsync(webSocket);
    }
    else
    {
        await next();
    }
});

app.MapGet("/", () =>

"WebSocket chat server is running.");

await app.RunAsync();

async Task HandleWebSocketAsync(System.Net.WebSockets.WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];
    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

    while (!result.CloseStatus.HasValue)
    {
        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

        // Broadcast message to all connected clients
        foreach (var client in connectedClients)
        {
            if (client.State == WebSocketState.Open && client != webSocket)
            {
                var responseBuffer = Encoding.UTF8.GetBytes(message);
                await client.SendAsync(new ArraySegment<byte>(responseBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    }

    connectedClients.TryTake(out _); // Remove client from list
    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
}
