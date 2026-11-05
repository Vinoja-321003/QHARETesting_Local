using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.API.WebSocket
{
    /// <summary>
    /// Opt-in WebSocket client for API tests. Disabled unless a URL is supplied or configured.
    /// </summary>
    public sealed class WebSocketTestClient : IAsyncDisposable
    {
        private readonly ClientWebSocket _socket = new();

        public static bool IsConfigured(string? url = null)
        {
            return !string.IsNullOrWhiteSpace(url ?? TestConfiguration.Instance.WebSocketUrl);
        }

        public WebSocketState State => _socket.State;

        public async Task ConnectAsync(string? url = null, CancellationToken cancellationToken = default)
        {
            var target = url ?? TestConfiguration.Instance.WebSocketUrl;
            if (string.IsNullOrWhiteSpace(target))
            {
                throw new InvalidOperationException("WebSocket URL is not configured. Set APITesting:WebSocket:Url.");
            }

            Logger.Information("Connecting WebSocket to {Url}", target);
            await _socket.ConnectAsync(new Uri(target), cancellationToken);
        }

        public async Task SendTextAsync(string message, CancellationToken cancellationToken = default)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
        }

        public async Task<string> ReceiveTextAsync(CancellationToken cancellationToken = default)
        {
            var buffer = new byte[4096];
            var result = await _socket.ReceiveAsync(buffer, cancellationToken);
            return Encoding.UTF8.GetString(buffer, 0, result.Count);
        }

        public async ValueTask DisposeAsync()
        {
            if (_socket.State == WebSocketState.Open)
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
            }

            _socket.Dispose();
        }
    }
}
