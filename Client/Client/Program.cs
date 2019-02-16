using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        private static Telepathy.Client client;

        static void Main(string[] args)
        {
            new Program().Setup();
        }

        public void Setup()
        {
            client = new Telepathy.Client();
            client.Connect("localhost", 9999);

            Loop();
        }

        public void Loop()
        {
            bool clientOpen = true;
            Telepathy.Message msg;
            while (clientOpen)
            {
                while (client.GetNextMessage(out msg))
                {
                    switch (msg.eventType)
                    {
                        case Telepathy.EventType.Connected:
                            Console.WriteLine("Connected to server!");
                            SendMessage("Hello server!");
                            break;
                        case Telepathy.EventType.Data:
                            Console.WriteLine("Received From Server: " + Encoding.UTF8.GetString(msg.data));
                            SendMessage("djgaven588");
                            break;
                        case Telepathy.EventType.Disconnected:
                            Console.WriteLine("Disconnected from server");
                            break;
                        default:
                            break;
                    }
                }

                Thread.Sleep(1000);
            }
        }

        public void SendMessage(string content)
        {
            client.Send(Encoding.UTF8.GetBytes(content));
        }
    }
}
