namespace Test.Shared
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Response specification returned by the mock server for a single request.
    /// </summary>
    public sealed class MockResponse
    {
        /// <summary>HTTP status code to return.</summary>
        public int StatusCode { get; set; } = 200;

        /// <summary>Response body.</summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>When true, the body is sent using chunked transfer-encoding.</summary>
        public bool Chunked { get; set; } = false;

        /// <summary>Artificial delay applied before the response is written, in milliseconds.</summary>
        public int DelayMs { get; set; } = 0;
    }

    /// <summary>
    /// Minimal in-process HTTP/1.1 server used to simulate the NeverBounce API.
    /// Binds to 127.0.0.1 (never localhost) on an ephemeral port so tests never
    /// touch the real service and never require URL-ACL reservations on Windows.
    /// </summary>
    public sealed class MockNeverBounceServer : IDisposable
    {
        private readonly TcpListener _Listener;
        private readonly Func<int, string, MockResponse> _Responder;
        private readonly CancellationTokenSource _Cts = new CancellationTokenSource();
        private int _Count = -1;
        private bool _Disposed = false;

        /// <summary>Base URL, e.g. http://127.0.0.1:{port}.</summary>
        public string BaseUrl { get; }

        /// <summary>Endpoint to assign to NeverBounceClient.Endpoint (BaseUrl + /v4).</summary>
        public string Endpoint { get; }

        /// <summary>Number of requests handled so far.</summary>
        public int RequestCount { get { return Volatile.Read(ref _Count) + 1; } }

        /// <summary>Raw request target (path + query) of the most recent request.</summary>
        public string? LastRawUrl { get; private set; }

        /// <summary>
        /// Instantiate the mock server. The responder receives the zero-based request
        /// index and the raw request target, and returns the response to send.
        /// </summary>
        public MockNeverBounceServer(Func<int, string, MockResponse> responder)
        {
            _Responder = responder ?? throw new ArgumentNullException(nameof(responder));

            int port = GetFreePort();
            BaseUrl = "http://127.0.0.1:" + port;
            Endpoint = BaseUrl + "/v4";

            _Listener = new TcpListener(IPAddress.Loopback, port);
            _Listener.Start();

            _ = Task.Run(() => AcceptLoopAsync(_Cts.Token));
        }

        /// <summary>
        /// Convenience factory that returns the same static response for every request.
        /// </summary>
        public static MockNeverBounceServer Constant(int statusCode, string body, bool chunked = false, int delayMs = 0)
        {
            return new MockNeverBounceServer((idx, url) => new MockResponse
            {
                StatusCode = statusCode,
                Body = body,
                Chunked = chunked,
                DelayMs = delayMs
            });
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client;

                try
                {
                    client = await _Listener.AcceptTcpClientAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    break;
                }

                int idx = Interlocked.Increment(ref _Count);
                _ = Task.Run(() => HandleClientAsync(client, idx));
            }
        }

        private async Task HandleClientAsync(TcpClient client, int idx)
        {
            try
            {
                using (client)
                using (NetworkStream ns = client.GetStream())
                {
                    string requestText = await ReadHeadersAsync(ns).ConfigureAwait(false);
                    string rawUrl = ParseRequestTarget(requestText);
                    LastRawUrl = rawUrl;

                    MockResponse response;
                    try
                    {
                        response = _Responder(idx, rawUrl);
                    }
                    catch
                    {
                        response = new MockResponse { StatusCode = 500, Body = string.Empty };
                    }

                    if (response.DelayMs > 0)
                        await Task.Delay(response.DelayMs).ConfigureAwait(false);

                    await WriteResponseAsync(ns, response).ConfigureAwait(false);
                    await ns.FlushAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                // Connection errors are ignored; the client under test observes them as failures.
            }
        }

        private static async Task<string> ReadHeadersAsync(NetworkStream ns)
        {
            byte[] buffer = new byte[4096];
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                int read = await ns.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (read <= 0) break;

                sb.Append(Encoding.ASCII.GetString(buffer, 0, read));
                if (sb.ToString().Contains("\r\n\r\n")) break;
            }

            return sb.ToString();
        }

        private static string ParseRequestTarget(string requestText)
        {
            if (string.IsNullOrEmpty(requestText)) return string.Empty;

            int lineEnd = requestText.IndexOf("\r\n", StringComparison.Ordinal);
            string firstLine = lineEnd >= 0 ? requestText.Substring(0, lineEnd) : requestText;

            string[] parts = firstLine.Split(' ');
            return parts.Length >= 2 ? parts[1] : string.Empty;
        }

        private static async Task WriteResponseAsync(NetworkStream ns, MockResponse response)
        {
            byte[] body = Encoding.UTF8.GetBytes(response.Body ?? string.Empty);

            StringBuilder head = new StringBuilder();
            head.Append("HTTP/1.1 ").Append(response.StatusCode).Append(' ')
                .Append(ReasonPhrase(response.StatusCode)).Append("\r\n");
            head.Append("Content-Type: application/json\r\n");
            head.Append("Connection: close\r\n");

            if (response.Chunked)
            {
                head.Append("Transfer-Encoding: chunked\r\n\r\n");
                byte[] headBytes = Encoding.ASCII.GetBytes(head.ToString());
                await ns.WriteAsync(headBytes, 0, headBytes.Length).ConfigureAwait(false);

                // Emit the body as two chunks to exercise multi-chunk reassembly.
                await WriteChunkAsync(ns, body, 0, body.Length / 2).ConfigureAwait(false);
                await WriteChunkAsync(ns, body, body.Length / 2, body.Length - (body.Length / 2)).ConfigureAwait(false);

                byte[] terminator = Encoding.ASCII.GetBytes("0\r\n\r\n");
                await ns.WriteAsync(terminator, 0, terminator.Length).ConfigureAwait(false);
            }
            else
            {
                head.Append("Content-Length: ").Append(body.Length).Append("\r\n\r\n");
                byte[] headBytes = Encoding.ASCII.GetBytes(head.ToString());
                await ns.WriteAsync(headBytes, 0, headBytes.Length).ConfigureAwait(false);
                await ns.WriteAsync(body, 0, body.Length).ConfigureAwait(false);
            }
        }

        private static async Task WriteChunkAsync(NetworkStream ns, byte[] body, int offset, int count)
        {
            byte[] sizeLine = Encoding.ASCII.GetBytes(count.ToString("x") + "\r\n");
            await ns.WriteAsync(sizeLine, 0, sizeLine.Length).ConfigureAwait(false);
            if (count > 0) await ns.WriteAsync(body, offset, count).ConfigureAwait(false);
            byte[] crlf = Encoding.ASCII.GetBytes("\r\n");
            await ns.WriteAsync(crlf, 0, crlf.Length).ConfigureAwait(false);
        }

        private static string ReasonPhrase(int statusCode)
        {
            switch (statusCode)
            {
                case 200: return "OK";
                case 400: return "Bad Request";
                case 401: return "Unauthorized";
                case 429: return "Too Many Requests";
                case 500: return "Internal Server Error";
                case 503: return "Service Unavailable";
                default: return "Status";
            }
        }

        private static int GetFreePort()
        {
            TcpListener probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        /// <summary>Dispose of resources and stop listening.</summary>
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;

            try { _Cts.Cancel(); } catch { }
            try { _Listener.Stop(); } catch { }
            try { _Cts.Dispose(); } catch { }
        }
    }
}
