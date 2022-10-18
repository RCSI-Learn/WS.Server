using System.Net;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var webSocketOptions = new WebSocketOptions { KeepAliveInterval = TimeSpan.FromMinutes(2) };

app.UseWebSockets(webSocketOptions);
app.Use(async (context, next) => {
    if (context.Request.Path == "/send") {
        if (context.WebSockets.IsWebSocketRequest) {
            using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync()) {
                await Send(context, webSocket);
            }
        }
        else {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }
    }
    else {
        await next.Invoke();
    }
});

static async Task Send(HttpContext context, WebSocket webSocket) {
    var buffer = new byte[4096];
    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Threading.CancellationToken.None);
    if (result != null) {
        while (!result.CloseStatus.HasValue) {
            string msg = Encoding.UTF8.GetString(new ArraySegment<byte>(buffer, 0, result.Count));
            Console.WriteLine("Client Says: " + msg);
            await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"Server says: {DateTime.UtcNow:f}")), result.MessageType, result.EndOfMessage, System.Threading.CancellationToken.None);
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), System.Threading.CancellationToken.None);
            //Console.WriteLine("")
        }
    }
    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, System.Threading.CancellationToken.None);
}

//app.MapGet("/", () => "Hello World!");

app.Run();

