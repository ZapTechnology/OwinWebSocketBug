using System;
using System.IO;
using System.Net.WebSockets;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketClient
{
    class Program
    {
        async static Task Main(string[] args)
        {
            var webSocket = new ClientWebSocket();

            await webSocket.ConnectAsync(new Uri("ws://localhost:55167/ws"), CancellationToken.None);

            var memoryStream = new MemoryStream();

            await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(0.ToString())), WebSocketMessageType.Text, true, CancellationToken.None);

            await Task.Run(() => HandleSocket(webSocket, new ArraySegment<byte>(new byte[1024]), CancellationToken.None, memoryStream));
        }

        private static async Task HandleSocket(WebSocket webSocket, ArraySegment<byte> receivedDataBuffer,
            CancellationToken cancellationToken, MemoryStream memoryStream)
        {
            int i = 0;

            StreamReader reader = new StreamReader(memoryStream);

            //Checks WebSocket state.
            while (webSocket.State == WebSocketState.Open)
            {
                await ReadMessage(webSocket, receivedDataBuffer, memoryStream);

                memoryStream.Position = 0;

                string message = reader.ReadToEnd();

                if (message == i.ToString())
                {
                    Console.WriteLine(i);
                    i++;
                    byte[] buffer = Encoding.UTF8.GetBytes(i.ToString());
                    await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                }

                memoryStream.Position = 0;
                memoryStream.SetLength(0);
            }
        }

        private static async Task ReadMessage(WebSocket webSocket, ArraySegment<byte> receivedDataBuffer, MemoryStream memoryStream)
        {
            WebSocketReceiveResult webSocketReceiveResult;

            long length = 0;

            do
            {
                webSocketReceiveResult =
                    await webSocket.ReceiveAsync(receivedDataBuffer, CancellationToken.None)
                        .ConfigureAwait(false);

                length += webSocketReceiveResult.Count;

                await memoryStream.WriteAsync(receivedDataBuffer.Array,
                        receivedDataBuffer.Offset,
                        webSocketReceiveResult.Count)
                    .ConfigureAwait(false);
            }
            while (!webSocketReceiveResult.EndOfMessage);
        }
    }
}