using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Library.Network
{
    /// <summary>
    /// 把 WebSocket 包装成 Stream，使上层 BaseConnection 的字节流收发逻辑无需改动。
    /// 服务端在已建立的 TCP 流上先完成 HTTP 升级握手，再用 WebSocket.CreateFromStream（框架自带，无 NuGet）创建。
    /// 这样服务器对外就是 WebSocket，浏览器 WASM 客户端可直接连接。
    /// </summary>
    public sealed class WebSocketStream : Stream
    {
        private readonly WebSocket _ws;
        private bool _disposed;

        public WebSocketStream(WebSocket webSocket)
        {
            _ws = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
        }

        public override bool CanRead =>
            !_disposed && (_ws.State == WebSocketState.Open || _ws.State == WebSocketState.CloseReceived || _ws.State == WebSocketState.CloseSent);
        public override bool CanSeek => false;
        public override bool CanWrite =>
            !_disposed && (_ws.State == WebSocketState.Open || _ws.State == WebSocketState.CloseReceived);
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_disposed) return 0;

            // 跳过非二进制帧（文本/ping/pong），直到拿到数据或连接关闭。
            int guard = 0;
            while (guard++ < 16)
            {
                try
                {
                    var result = _ws.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count), CancellationToken.None).GetAwaiter().GetResult();

                    if (result.MessageType == WebSocketMessageType.Close)
                        return 0;

                    if (result.MessageType == WebSocketMessageType.Text)
                        continue;

                    return result.Count;
                }
                catch (WebSocketException)
                {
                    return 0;
                }
                catch (ObjectDisposedException)
                {
                    return 0;
                }
                catch (OperationCanceledException)
                {
                    return 0;
                }
            }
            return 0;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_disposed) return;

            try
            {
                _ws.SendAsync(new ArraySegment<byte>(buffer, offset, count), WebSocketMessageType.Binary, true, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (WebSocketException) { }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
        }

        /// <summary>在已建立的 TCP 流上完成 WebSocket 握手，返回包装后的 Stream（握手失败抛异常）。</summary>
        public static WebSocketStream Accept(NetworkStream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            // 逐字节读取 HTTP 请求头直到 \r\n\r\n，避免 StreamReader 预读破坏紧随其后的 WS 帧。
            var headerBytes = new List<byte>(256);
            int b;
            while ((b = stream.ReadByte()) >= 0)
            {
                headerBytes.Add((byte)b);
                int n = headerBytes.Count;
                if (n >= 4 && headerBytes[n - 4] == '\r' && headerBytes[n - 3] == '\n' && headerBytes[n - 2] == '\r' && headerBytes[n - 1] == '\n')
                    break;
            }

            string headerText = Encoding.ASCII.GetString(headerBytes.ToArray());
            string key = null;
            foreach (var line in headerText.Split('\n'))
            {
                if (line.StartsWith("Sec-WebSocket-Key", StringComparison.OrdinalIgnoreCase))
                {
                    int idx = line.IndexOf(':');
                    if (idx >= 0) key = line.Substring(idx + 1).Trim();
                }
            }

            if (string.IsNullOrEmpty(key))
                throw new InvalidOperationException("请求不是有效的 WebSocket 升级请求。");

            string accept = ComputeAccept(key);
            string response = "HTTP/1.1 101 Switching Protocols\r\n" +
                             "Upgrade: websocket\r\n" +
                             "Connection: Upgrade\r\n" +
                             "Sec-WebSocket-Accept: " + accept + "\r\n\r\n";
            byte[] resp = Encoding.ASCII.GetBytes(response);
            stream.Write(resp, 0, resp.Length);

            WebSocket ws = WebSocket.CreateFromStream(stream, isServer: true, subProtocol: null, keepAliveInterval: TimeSpan.FromSeconds(30));
            return new WebSocketStream(ws);
        }

        private static string ComputeAccept(string key)
        {
            const string Guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] hash = SHA1.HashData(Encoding.ASCII.GetBytes(key + Guid));
            return Convert.ToBase64String(hash);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                if (_ws.State == WebSocketState.Open || _ws.State == WebSocketState.CloseReceived)
                    _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).GetAwaiter().GetResult();
            }
            catch { }
            try { _ws.Abort(); } catch { }
            try { _ws.Dispose(); } catch { }

            base.Dispose(disposing);
        }
    }
}
