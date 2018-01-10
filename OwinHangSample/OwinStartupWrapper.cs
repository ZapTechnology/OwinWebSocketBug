using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using OwinHangSample;
using Website;

[assembly: OwinStartup(typeof(OwinStartupWrapper))]

namespace Website
{
    public class OwinStartupWrapper
    {
        public void Configuration(IAppBuilder app)
        {
            app.Map("/ws", SomeAction);
        }

        private void SomeAction(IAppBuilder app)
        {
            app.Use(HttpHandler);
        }

        private async Task HttpHandler(IOwinContext context, Func<Task> next)
        {
            Action<IDictionary<string, object>, Func<IDictionary<string, object>, Task>> accept =
                context.Get<Action<IDictionary<string, object>, Func<IDictionary<string, object>, Task>>>("websocket.Accept");

            if (accept != null)
            {
                accept(new Dictionary<string, object>(),
                    websocketContext => OnAccept(websocketContext, context));

                return;
            }

            await next();
        }

        //Asynchronous request handler.
        public async Task OnAccept(IDictionary<string, object> websocketContext, IOwinContext context)
        {
            //Gets the current WebSocket object.
            OwinWebSocketWrapper webSocket = new OwinWebSocketWrapper(websocketContext);

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

        private async Task HandleSocket(OwinWebSocketWrapper webSocket, ArraySegment<byte> receivedDataBuffer,
            CancellationToken cancellationToken, MemoryStream memoryStream)
        {
            //Checks WebSocket state.
            while (webSocket.State == WebSocketState.Open)
            {
                await ReadMessage(webSocket, receivedDataBuffer, memoryStream);

                memoryStream.Position = 0;

                //Sends data back.
                await webSocket.SendAsync(new ArraySegment<byte>(memoryStream.ToArray()),
                    WebSocketMessageType.Text, true, cancellationToken);

                memoryStream.Position = 0;
                memoryStream.SetLength(0);
            }
        }

        private async Task<WebSocketReceiveResult> ReadMessage(IWebSocketWrapper webSocket, ArraySegment<byte> receivedDataBuffer, MemoryStream memoryStream)
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