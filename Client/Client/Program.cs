using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    public class Program
    {
        private static Telepathy.Client client;
        private ClientState state = ClientState.WaitingForPasswordStatus;
        private bool passwordProtected = false;
        private EncryptionType encryption = EncryptionType.N_A;
        private string aesKey = null;

        private bool futureMessagesEncrypted = false;

        private enum ClientState
        {
            WaitingForPasswordStatus,
            WaitingForEncryptionMethod,
            PasswordEnabled,
            FinalizeAESEncryption,
            Nickname,
            Established
        }

        private enum EncryptionType
        {
            PRESHARED_AES,
            N_A
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

            var thread = new Thread(() =>
            {
                while (clientOpen)
                {
                    string input = Console.ReadLine();
                    messagesToSend.Enqueue(input);
                }
            })
            {
                IsBackground = true
            };
            thread.Start();

            while (clientOpen)
            {
                while (client.GetNextMessage(out Telepathy.Message msg))
                {
                    switch (msg.eventType)
                    {
                        case Telepathy.EventType.Connected:
                            Log("Connected to server! Waiting for password status...", "Connected");
                            break;
                        case Telepathy.EventType.Data:
                            string msgContents = (futureMessagesEncrypted) ? PresharedAESEncryption.AESDecrypt(msg.data, aesKey): Encoding.UTF8.GetString(msg.data);
                            switch (state)
                            {
                                case ClientState.WaitingForPasswordStatus:
                                    if (msgContents == "passwordProtected")
                                    {
                                        passwordProtected = true;
                                    }
                                    state = ClientState.WaitingForEncryptionMethod;
                                    break;
                                case ClientState.WaitingForEncryptionMethod:
                                    if (msgContents == "PRESHARED-AES")
                                    {
                                        encryption = EncryptionType.PRESHARED_AES;
                                        Log("This server has encryption, and a password. Pleas enter the password...", "Encryption / Password");
                                        SendMessage("Yes");
                                        state = ClientState.PasswordEnabled;
                                    }
                                    else if (msgContents == "N/A")
                                    {
                                        encryption = EncryptionType.N_A;
                                        Log("This server has no encryption, and no password. Continuing...", "Encryption");
                                        SendMessage("Yes");
                                        SendMessage("");
                                        state = ClientState.Nickname;
                                    }
                                    else
                                    {
                                        Log($"This server wants to use {msgContents} encryption, but this client doesn't support it. Notifying...", "Encryption");
                                        SendMessage("Not compatible");
                                        state = ClientState.Established;
                                    }
                                    break;
                                case ClientState.PasswordEnabled:
                                    if (msgContents == "V")
                                    {
                                        Log("Your password was valid! Continuing...", "Valid Password");
                                        state = (encryption == EncryptionType.PRESHARED_AES) ? ClientState.FinalizeAESEncryption : ClientState.Nickname;
                                        futureMessagesEncrypted = encryption == EncryptionType.PRESHARED_AES;
                                    }
                                    else
                                    {
                                        Log(msgContents, "Invalid Password");
                                    }
                                    break;
                                case ClientState.FinalizeAESEncryption:
                                    aesKey = msgContents;
                                    state = ClientState.Nickname;
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
            if (state == ClientState.PasswordEnabled)
            {
                aesKey = PresharedAESEncryption.GetAESHash(content);
            }
            client.Send((futureMessagesEncrypted || state == ClientState.PasswordEnabled) ? PresharedAESEncryption.AESEncrypt(content, aesKey): Encoding.UTF8.GetBytes(content));
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
