using System.Net;
using System.Net.Sockets;
using System.Text;
using Shared.Logging;
using Shared.Scheduling;
using Shared.Settings;

namespace Server.Health
{
    /// <summary>
    /// Minimal TCP HTTP server that binds to the Heroku-assigned PORT to satisfy boot checks.
    /// Also updates <see cref="NetworkSettings.ServerPort"/> to the same value for the UDP server.
    /// </summary>
    public sealed class HttpHealthServer(NetworkSettings networkSettings, ILogger logger) : IInitializable, IDisposable
    {
        private CancellationTokenSource? _cts;
        private Task? _serverTask;

        public void Initialize()
        {
            var envPort = Environment.GetEnvironmentVariable("PORT");
            if (string.IsNullOrEmpty(envPort) || !int.TryParse(envPort, out var herokuPort))
            {
                return;
            }

            // Align UDP server port with Heroku's assigned port
            networkSettings.ServerPort = herokuPort;

            _cts = new CancellationTokenSource();
            _serverTask = Task.Run(() => RunListenerAsync(herokuPort, _cts.Token), _cts.Token);
            logger.Info("HTTP health server started on port {0}", herokuPort);
        }

        public void Dispose()
        {
            if (_cts == null)
            {
                return;
            }

            try
            {
                _cts.Cancel();
                _serverTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // ignore on shutdown
            }
        }

        private static async Task RunListenerAsync(int port, CancellationToken cancellationToken)
        {
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!listener.Pending())
                    {
                        await Task.Delay(50, cancellationToken);
                        continue;
                    }

                    using var client = await listener.AcceptTcpClientAsync(cancellationToken);
                    using var stream = client.GetStream();

                    // best-effort read of request
                    var buffer = new byte[1024];
                    if (stream.CanRead && stream.DataAvailable)
                    {
                        _ = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    }

                    var body = "OK"u8.ToArray();
                    var responseHeaders =
                        "HTTP/1.1 200 OK\r\n" +
                        "Content-Type: text/plain\r\n" +
                        $"Content-Length: {body.Length}\r\n" +
                        "Connection: close\r\n" +
                        "\r\n";
                    var headerBytes = Encoding.ASCII.GetBytes(responseHeaders);
                    await stream.WriteAsync(headerBytes, 0, headerBytes.Length, cancellationToken);
                    await stream.WriteAsync(body, 0, body.Length, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // normal during shutdown
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}