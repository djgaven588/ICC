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
            Queue<string> messagesToSend = new Queue<string>();

            var thread = new Thread(() => {
                while (clientOpen)
                {
                    string input = Console.ReadLine();
                    messagesToSend.Enqueue(input);
                }
            });

            thread.IsBackground = true;
            thread.Start();

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
                            Console.WriteLine(Encoding.UTF8.GetString(msg.data));
                            break;
                        case Telepathy.EventType.Disconnected:
                            Console.WriteLine("Disconnected from server");
                            break;
                        default:
                            break;
                    }
                }

                while (messagesToSend.Count > 0)
                {
                    SendMessage(messagesToSend.Dequeue());
                }

                Thread.Sleep(250);
            }
        }

        public void SendMessage(string content)
        {
            client.Send(Encoding.UTF8.GetBytes(content));
        }
    }
}
