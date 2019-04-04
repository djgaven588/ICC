using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class Program
    {
        public static Telepathy.Server server;
        public static Dictionary<int, ConnectionData> connections = new Dictionary<int, ConnectionData>();
        public static HashSet<string> currentUsernames = new HashSet<string>();

        public static string dailyMsg;
        public static bool passwordProtected;
        public static string password;

        public static bool encryptionEnabled;

        private static EventHandler handler = new CustomEventHandler();

        static void Main(string[] args)
        {
            if (File.Exists("DailyMessage.txt"))
            {
                string contents = File.ReadAllText("DailyMessage.txt");
                Debug.Log($"Daily message set to {contents}. You can modify the 'DailyMessage.txt' file next to your executable to modify this.", "Daily Message");
                dailyMsg = contents;
            }
            else
            {
                string dailyMessage = "Welcome! This server is running the default configuration. Please notify the server operator so they can fix it.";
                Debug.Log($"A daily message file doesn't exist. You can create one nameed 'DailyMessage.txt' next to the server executable to modify the daily message. It was set to {dailyMessage}.", "Daily Message");
                dailyMsg = dailyMessage;
            }

            if (args == null || args.Length == 0 || args[0].Length == 0)
            {
                Debug.Log("This server was started with no password. You can change the first parameter when starting the server to modify the password. This server is not encrypted.", "Password Protection");
                passwordProtected = false;
                encryptionEnabled = false;
            }
            else
            {
                passwordProtected = true;
                encryptionEnabled = true;
                password = args[0];
                Debug.Log("This server is password protected. The password is " + password + ". You can change this by changing the second program argument when starting the server. This server is encrypted.", "Password Protection");
            }

            new Program().Setup();
        }

        public void Setup()
        {
            Console.Title = "ICC - Console Server";

            Telepathy.Logger.Log = LogTelepathy;
            Telepathy.Logger.LogError = LogTelepathy;
            Telepathy.Logger.LogWarning = LogTelepathy;

            server = new Telepathy.Server();
            server.Start(9999);
            Debug.Log("Server online! Connect to it using the port 9999\nThe local ip of the server is: " + GetLocalIPAddress()
                + "\nIf you want this server to be accessed by the outside world, port forward port 9999 and give users your public address." 
                + "\nNOTE: You, the server host, are responsible for anything that happens because of port forwarding. Be careful.", "Server Startup");
            Loop();
        }

        public void Loop()
        {
            bool serverRun = true;
            while (serverRun)
            {
                while (server.GetNextMessage(out Telepathy.Message msg))
                {
                    switch (msg.eventType)
                    {
                        case Telepathy.EventType.Connected:
                            handler.OnConnect(msg.connectionId);
                            break;
                        case Telepathy.EventType.Data:
                            ConnectionData connection = connections[msg.connectionId];

                            string msgContents = (connection.GetAESKey() != null) ? 
                                PresharedKeyEncryption.AESDecrypt(Convert.FromBase64String(Encoding.UTF8.GetString(msg.data)), connection.GetAESKey()) : 
                                Encoding.UTF8.GetString(msg.data);

                            try
                            {
                                switch (connection.GetConnectionStage())
                                {
                                    case ConnectionData.ConnectionStage.AwaitingEncryptionSupport:
                                        handler.OnEncryptionSupported(msgContents, ref connection);
                                        break;
                                    case ConnectionData.ConnectionStage.AwaitingEncryptionSetup:
                                        handler.OnEncryptionSetup(msgContents, ref connection);
                                        break;
                                    case ConnectionData.ConnectionStage.AwaitingNickname:
                                        handler.OnUsernameSetAttempt(msgContents, ref connection);
                                        break;
                                    case ConnectionData.ConnectionStage.ConnectionEstablished:
                                        handler.OnMessageReceived(msgContents, ref connection);
                                        break;
                                    default:
                                        Debug.Log($"Client connection state {connection.GetConnectionStage().ToString()} is not supported!", "Missing Support");
                                        break;
                                }

                                connections[connection.GetConnectionId()] = connection;
                            }
                            catch (Exception e)
                            {
                                Debug.Log($"Exception caused by message with contents {msg.data}. Exception info: {e.ToString()}", "Caught Exception, On Message Received");
                                KickUser(connection, "Caused server error");
                            }
                            break;
                        case Telepathy.EventType.Disconnected:
                            handler.OnDisconnected(msg.connectionId);
                            break;
                    }
                }

                Thread.Sleep(100);
            }
        }

        public static void BroadcastMsg(string msg)
        {
            foreach (ConnectionData connection in connections.Values)
            {
                if (connection.GetConnectionStage() == ConnectionData.ConnectionStage.ConnectionEstablished)
                {
                    connection.SendMessage(msg);
                }
            }
        }

        public static void KickUser(ConnectionData connection, string reason)
        {
            connection.SendMessage("Kicked from server, reason: " + reason);
            server.Disconnect(connection.GetConnectionId());
            if (connection.GetNickname() != null && connection.GetNickname() != "")
            {
                BroadcastMsg($"User {connection.GetNickname()} was kicked for '{reason}'");
            }
            //handler.OnDisconnected(connection.connectionId);
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "No local network using IPv4 was found, people may not be able to connect to this server.";
        }

        public void LogTelepathy(string message)
        {
            //Not setup as it is messy.
        }
    }
}
