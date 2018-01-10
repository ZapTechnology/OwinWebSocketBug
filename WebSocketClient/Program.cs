using System;
using WebSocket4Net;

namespace WebSocketClient
{
    class Program
    {
        static void Main(string[] args)
        {
            WebSocket webSocket = new WebSocket("ws://localhost:55167/ws");

            int i = 0;

            webSocket.MessageReceived +=
                (sender, eventArgs) =>
                {
                    if (eventArgs.Message == i.ToString())
                    {
                        Console.WriteLine(i);
                        i++;
                        webSocket.Send(i.ToString());
                    }
                };

            webSocket.Opened += (sender, eventArgs) => webSocket.Send(i.ToString());

            webSocket.Open();

            Console.ReadLine();
        }
    }
}