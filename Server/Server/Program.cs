using System;
using System.Collections.Generic;
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
            if (args == null || args.Length == 0)
            {
                Debug.Log("Program arguments were not specified, the daily message is set to notify users that this server is not configured properly.", "Daily Message");
                Debug.Log("Program arguments were not specified, the password is set to none.", "Password Protection");
                args = new string[] { "Welcome! This server is running the default configuration. Please notify the server operator so they can fix it." };
            }

            if (args == null || args.Length == 0 || args[0] == null)
            {
                Debug.Log("A daily message was not specified in the program arguments, it is recommended to set this up. Using default daily message...", "Daily Message");
                dailyMsg = "Welcome to the server! This server does not have a daily message set.\nIf you are the server owner, make sure to run the server with the first parameter as the daily message!";
                passwordProtected = false;
                encryptionEnabled = false;
                Debug.Log("This server is not password protected. This is fine if you want anyone and everyone to connect, but can be changed to add security. This can be changed by editing the second program argument. This server also doesn't have encryption enabled.", "Password Protection");
            }
            else
            {
                Debug.Log("The daily message is: " + args[0] + ". You can change this by changing the first program argument when starting the server.", "Daily Message");
                dailyMsg = args[0];

                if (args.Length > 1 && args[1] != null)
                {
                    passwordProtected = true;
                    encryptionEnabled = true;
                    password = args[1];
                    Debug.Log("This server is password protected. The password is " + password + ". You can change this by changing the second program argument when starting the server. This server is encrypted.", "Password Protection");
                }
                else
                {
                    passwordProtected = false;
                    encryptionEnabled = false;
                    Debug.Log("This server is not password protected. This is fine if you want anyone and everyone to connect, but can be changed to add security. This can be changed by editing the second program argument. This server is not encrypted.", "Password Protection");
                }
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

                            string msgContents = (connection.aesCommunicationKey != null) ? 
                                PresharedKeyEncryption.AESDecrypt(Convert.FromBase64String(Encoding.UTF8.GetString(msg.data)), connection.aesCommunicationKey) : 
                                Encoding.UTF8.GetString(msg.data);

                            try
                            {
                                switch (connection.currentStage)
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
                                        Debug.Log($"Client connection state {connection.currentStage.ToString()} is not supported!", "Missing Support");
                                        break;
                                }

                                connections[connection.connectionId] = connection;
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
                if (connection.currentStage == ConnectionData.ConnectionStage.ConnectionEstablished)
                {
                    connection.SendMessage(msg);
                }
            }
        }

        public static void KickUser(ConnectionData connection, string reason)
        {
            connection.SendMessage("Kicked from server, reason: " + reason);
            server.Disconnect(connection.connectionId);
            if (connection.nickname != null && connection.nickname != "")
            {
                BroadcastMsg($"User {connection.nickname} was kicked for '{reason}'");
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
