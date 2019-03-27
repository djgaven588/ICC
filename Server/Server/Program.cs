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

        private static EventHandler handler = new CustomEventHandler();

        static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                args = new string[] { "Welcome! This server is in debug mode..." };
            }

            if (args == null || args.Length == 0 || args[0] == null)
            {
                Debug.Log("A daily message was not specified in the program arguments, it is recommended to set this up. Using default daily message...", "Daily Message");
                dailyMsg = "Welcome to the server! This server does not have a daily message set.\nIf you are the server owner, make sure to run the server with the first parameter as the daily message!";
                passwordProtected = false;
                Debug.Log("This server is not password protected. This is fine if you want anyone and everyone to connect, but can be changed to add security. This can be changed by editing the second program argument", "Password Protection");
            }
            else
            {
                Debug.Log("The daily message is: " + args[0] + ". You can change this by changing the first program argument when starting the server.", "Daily Message");
                dailyMsg = args[0];

                if (args.Length > 1 && args[1] != null)
                {
                    passwordProtected = true;
                    password = args[1];
                    Debug.Log("This server is password protected. The password is " + password + ". You can change this by changing the second program argument when starting the server.", "Password Protection");
                }
                else
                {
                    passwordProtected = false;
                    Debug.Log("This server is not password protected. This is fine if you want anyone and everyone to connect, but can be changed to add security. This can be changed by editing the second program argument", "Password Protection");
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

            Telepathy.Message msg;
            while (serverRun)
            {
                while (server.GetNextMessage(out msg))
                {
                    switch (msg.eventType)
                    {
                        case Telepathy.EventType.Connected:
                            handler.OnConnect(msg.connectionId);
                            break;
                        case Telepathy.EventType.Data:
                            string msgContents = Encoding.UTF8.GetString(msg.data);

                            ConnectionData connection = connections[msg.connectionId];
                            if (connection.currentStage == ConnectionData.ConnectionStage.NewConnection)
                            {
                                if (passwordProtected)
                                {
                                    handler.OnPasswordCheck(msgContents, connection);
                                }
                                else
                                {
                                    handler.OnNotPasswordProtected(connection);
                                }
                            }
                            else if (connection.currentStage == ConnectionData.ConnectionStage.AwaitingNickname)
                            {
                                handler.OnUsernameSetAttempt(msgContents, connection);
                            }
                            else
                            {
                                handler.OnMessageReceived(msgContents, connection);
                            }

                            connections[connection.connectionId] = connection;

                            break;
                        case Telepathy.EventType.Disconnected:
                            handler.OnDisconnected(msg.connectionId);
                            break;
                    }
                }

                Thread.Sleep(250);
            }
        }

        public static void BroadcastMsg(string msg)
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            foreach (ConnectionData connection in connections.Values)
            {
                if (connection.currentStage == ConnectionData.ConnectionStage.ConnectionEstablished)
                {
                    connection.SendMessage(data);
                }
            }
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
