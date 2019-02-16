using System;
using System.Collections.Generic;
using System.Linq;
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
                dailyMsg = "Welcome to the server! This server does not have a daily message set.\nIf you are the server owner, make sure to run the server with the first parameter as the daily message!";
            }
            else
            {
                dailyMsg = args[0];
            }
            new Program().Setup();
        }

        public void Setup()
        {
            server = new Telepathy.Server();
            server.Start(9999);
            Console.WriteLine("Server online! Connect to it using the port 9999");
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
                            Console.WriteLine("A user has connected, waiting for communication to be confirmed by the client.");
                            break;
                        case Telepathy.EventType.Data:
                            string msgContents = Encoding.UTF8.GetString(msg.data);
                            //Console.WriteLine(msg.connectionId + " Data: " + msgContents);

                            ConnectionData connection = connections[msg.connectionId];
                            if (connection.currentStage == ConnectionData.ConnectionStage.NewConnection)
                            {
                                Console.WriteLine("A user has established communication. Waiting for a username...");
                                connection.currentStage = ConnectionData.ConnectionStage.AwaitingNickname;
                                connection.SendMessage("Welcome to the server client! Please send your nickname.");
                            }
                            else if (connection.currentStage == ConnectionData.ConnectionStage.AwaitingNickname)
                            {
                                Console.WriteLine("A user has requested to use the username '" + msgContents + "'");
                                bool result = connection.SetNickname(msgContents);
                                if (result)
                                {
                                    Console.WriteLine("The username '" + msgContents + "' was available and valid. Welcome " + msgContents + "!");
                                    connection.currentStage = ConnectionData.ConnectionStage.ConnectionEstablished;
                                    connection.SendMessage("Welcome " + connection.nickname + "!\nDaily Message:\n" + dailyMsg);
                                }
                                else
                                {
                                    Console.WriteLine("The username '" + msgContents + "' was not available or not valid. The user was notified.");
                                    connection.SendMessage("Your username is either too long, too short, or is already in use!\nMake sure your username is unique, greather than 3 characters, and less than 16 characters!");
                                }
                            }
                            else
                            {
                                Console.WriteLine(connection.nickname + " said: " + msgContents);
                                BroadcastMsg(connection.nickname + ": " + msgContents);
                            }

                            connections[connection.connectionId] = connection;

                            break;
                        case Telepathy.EventType.Disconnected:
                            connections[msg.connectionId].RemoveConnection();
                            Console.WriteLine(msg.connectionId + " Disconnected");
                            break;
                    }
                }
                Thread.Sleep(1000);
            }
        }

        public void BroadcastMsg(string msg)
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            foreach (ConnectionData connection in connections.Values)
            {
                connection.SendMessage(data);
            }
        }
    }
}
