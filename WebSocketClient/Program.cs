using System;
using System.Reflection;
using System.Threading;
using WebSocket4Net;
using WebSocket4Net.Protocol;

namespace WebSocketClient
{
    class Program
    {
        static void Main(string[] args)
        {
            WebSocket webSocket = new WebSocket("ws://localhost:55167/ws");

            int i = 0;
            IProtocolProcessor protocolProcessor = typeof(WebSocket).GetProperty("ProtocolProcessor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(webSocket) as IProtocolProcessor;

            webSocket.MessageReceived +=
                (sender, eventArgs) =>
                {
                    Console.WriteLine("Recieved: " + eventArgs.Message);
                    if (eventArgs.Message == i.ToString())
                    {
                        Console.WriteLine(i);
                        i++;
                        webSocket.Send(i.ToString());
                        protocolProcessor.SendPing(webSocket, DateTime.Now.ToString());
                    }
                    else
                    {
                        Console.WriteLine("Respond doesn't match: " + i.ToString());
                    }
                };

            webSocket.Opened += (sender, eventArgs) => webSocket.Send(i.ToString());

            webSocket.Open();

            Console.ReadLine();
        }
    }
}