using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;

namespace OwinHangSample
{
    public class WebSocketHandler : IHttpHandler
    {
        private static Task mLastSendAsync;

        public void ProcessRequest(HttpContext context)
        {
            //Checks if the query is WebSocket request.  
            if (context.IsWebSocketRequest)
            {
                //If yes, we attach the asynchronous handler. 
                context.AcceptWebSocketRequest(OnAccept);
            }
        }

        public bool IsReusable { get { return false; } }



        //Asynchronous request handler.
        public async Task OnAccept(AspNetWebSocketContext aspNetWebSocketContext)
        {
            //Gets the current WebSocket object.
            WebSocket webSocket = aspNetWebSocketContext.WebSocket;

            /*We define a certain constant which will represent
            size of received data. It is established by us and 
            we can set any value. We know that in this case the size of the sent
            data is very small.
            */
            const int maxMessageSize = 1024;

            //Buffer for received bits.
            var receivedDataBuffer = new ArraySegment<Byte>(new Byte[maxMessageSize]);

            MemoryStream memoryStream = new MemoryStream();

            var cancellationToken = CancellationToken.None;

            await Task.Run(() => HandleSocket(webSocket, receivedDataBuffer, cancellationToken, memoryStream));
        }

        private async Task HandleSocket(WebSocket webSocket, ArraySegment<byte> receivedDataBuffer,
            CancellationToken cancellationToken, MemoryStream memoryStream)
        {
            //Checks WebSocket state.
            while (webSocket.State == WebSocketState.Open)
            {
                await ReadMessage(webSocket, receivedDataBuffer, memoryStream);

                memoryStream.Position = 0;

                //Sends data back.
                Task lastSendAsync = webSocket.SendAsync(new ArraySegment<byte>(memoryStream.ToArray()),
                    WebSocketMessageType.Text, true, cancellationToken);

                mLastSendAsync = lastSendAsync;

                await lastSendAsync;

                memoryStream.Position = 0;
                memoryStream.SetLength(0);
            }
        }

        private async Task<WebSocketReceiveResult> ReadMessage(WebSocket webSocket, ArraySegment<byte> receivedDataBuffer, MemoryStream memoryStream)
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

            return webSocketReceiveResult;
        }
    }
}