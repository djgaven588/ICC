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
    class Program
    {
        private static Telepathy.Server server;
        private static Dictionary<int, ConnectionData> connections = new Dictionary<int, ConnectionData>();
        private static HashSet<string> currentUsernames = new HashSet<string>();
        private static string dailyMsg;

        private struct ConnectionData
        {
            public int connectionId;
            public string nickname;
            public ConnectionStage currentStage;

            public void RemoveConnection()
            {
                if (nickname != null)
                {
                    currentUsernames.Remove(nickname);
                }
                connections.Remove(connectionId);
            }

            public void SendMessage(string data)
            {
                server.Send(connectionId, Encoding.UTF8.GetBytes(data));
            }

            public void SendMessage(byte[] data)
            {
                server.Send(connectionId, data);
            }

            public void SetStage(ConnectionStage stage)
            {
                this.currentStage = stage;
            }

            public bool SetNickname(string nick)
            {
                if (nick.Length < 16 && nick.Length > 3 && !currentUsernames.Contains(nick))
                {
                    nickname = nick;
                    currentUsernames.Add(nick);
                    return true;
                }
                return false;
            }

            public ConnectionData(int connectionId)
            {
                this.connectionId = connectionId;
                currentStage = ConnectionStage.NewConnection;
                nickname = null;
            }

            public enum ConnectionStage
            {
                NewConnection,
                AwaitingRSAEncryptedResponse,
                AwaitingAESEncryptedResonse,
                AwaitingNickname,
                ConnectionEstablished
            }
        }

        static void Main(string[] args)
        {
            if (args == null || args.Length == 0 || args[0] == null)
            {
                Log("A daily message was not specified in the program arguments, it is recommended to set this up. Using default daily message...", "Daily Message");
                dailyMsg = "Welcome to the server! This server does not have a daily message set.\nIf you are the server owner, make sure to run the server with the first parameter as the daily message!";
            }
            else
            {
                Log("The daily message is: " + args[0] + ". You can change this by changing the first program arugment when starting the server.", "Daily Message");
                dailyMsg = args[0];
            }
            new Program().Setup();
        }

        public void Setup()
        {
            Telepathy.Logger.Log = LogTelepathy;
            Telepathy.Logger.LogError = LogTelepathy;
            Telepathy.Logger.LogWarning = LogTelepathy;

            server = new Telepathy.Server();
            server.Start(9999);
            Log("Server online! Connect to it using the port 9999\nThe local ip of the server is: " + GetLocalIPAddress()
                + "\nIf you want this server to be access by the outside world, port forward port 9999 and give users your public address." 
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
                            connections.Add(msg.connectionId, new ConnectionData(msg.connectionId));
                            Log("A user has connected, waiting for communication to be confirmed by the client.", "New Connection");
                            break;
                        case Telepathy.EventType.Data:
                            string msgContents = Encoding.UTF8.GetString(msg.data);

                            ConnectionData connection = connections[msg.connectionId];
                            if (connection.currentStage == ConnectionData.ConnectionStage.NewConnection)
                            {
                                Log("A user has established communication. Waiting for a username...", "User Connected");
                                connection.currentStage = ConnectionData.ConnectionStage.AwaitingNickname;
                                connection.SendMessage("Welcome to the server client! Please send your nickname.");
                            }
                            else if (connection.currentStage == ConnectionData.ConnectionStage.AwaitingNickname)
                            {
                                Log("A user has requested to use the username '" + msgContents + "'", "Nickname");
                                bool result = connection.SetNickname(msgContents);
                                if (result)
                                {
                                    Log("The username '" + msgContents + "' was available and valid. Welcome " + msgContents + "!", "Nickname");
                                    connection.currentStage = ConnectionData.ConnectionStage.ConnectionEstablished;
                                    connection.SendMessage("Welcome " + connection.nickname + "!\nDaily Message:\n" + dailyMsg);
                                }
                                else
                                {
                                    Log("The username '" + msgContents + "' was not available or not valid. The user was notified.", "Nickname");
                                    connection.SendMessage("Your username is either too long, too short, or is already in use!\nMake sure your username is unique, greather than 3 characters, and less than 16 characters!");
                                }
                            }
                            else
                            {
                                Log(connection.nickname + " said: " + msgContents, "User Message");
                                BroadcastMsg(connection.nickname + ": " + msgContents);
                            }

                            connections[connection.connectionId] = connection;

                            break;
                        case Telepathy.EventType.Disconnected:
                            ConnectionData connectData = connections[msg.connectionId];
                            connectData.RemoveConnection();
                            Log((connectData.nickname != null ? "User " + connectData.nickname : "Unknown User") + " Disconnected", "Disconnection");
                            break;
                    }
                }

                Thread.Sleep(250);
            }
        }

        public void BroadcastMsg(string msg)
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

        public static void Log(string message, string from)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(from + ":");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);
        }
    }
}
