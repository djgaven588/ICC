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
        private ClientState state = ClientState.WaitingForPasswordStatus;

        private enum ClientState
        {
            WaitingForPasswordStatus,
            PasswordEnabled,
            Nickname,
            Established
        }

        static void Main(string[] args)
        {
            Console.Title = "ICC - Console Client";
            new Program().Setup();
        }

        public void Setup()
        {
            client = new Telepathy.Client();
            Log("Please enter the IP of the server below.", "Starting");
            string ip = Console.ReadLine();
            client.Connect(ip, 9999);

            Log($"Connecting to server at address {ip}, and port 9999", "Connecting");

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
                            Log("Connected to server! Waiting for password status...", "Connected");
                            break;
                        case Telepathy.EventType.Data:
                            string msgContents = Encoding.UTF8.GetString(msg.data);
                            switch (state)
                            {
                                case ClientState.WaitingForPasswordStatus:
                                    if (msgContents.Length > 0)
                                    {
                                        Log("This server has a password, please enter the password for this server: ", "Password Protection");
                                        state = ClientState.PasswordEnabled;
                                    }
                                    else
                                    {
                                        Log("This server does not have a password. Continuing...", "Password Protection");
                                        SendMessage("");
                                        state = ClientState.Nickname;
                                    }
                                    break;
                                case ClientState.PasswordEnabled:
                                    if (msgContents == "V")
                                    {
                                        Log("Your password was valid! Continuing...", "Valid Password");
                                        state = ClientState.Nickname;
                                    }
                                    else
                                    {
                                        Log(msgContents, "Invalid Password");
                                    }
                                    break;
                                case ClientState.Nickname:
                                    if (msgContents == "V")
                                    {
                                        state = ClientState.Established;
                                    }
                                    else
                                    {
                                        Log(msgContents, "Nickname");
                                    }
                                    break;
                                case ClientState.Established:
                                    Log(msgContents, "Message");
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case Telepathy.EventType.Disconnected:
                            Log("Disconnected from server", "Disconnected");
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

        public static void Log(string content, string from)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(from + ":");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(content);
        }
    }
}
