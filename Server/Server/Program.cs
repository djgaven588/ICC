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
                ConnectionEstablished,
                Disconnected
            }
        }

        static void Main(string[] args)
        {
            new Program().Setup();
        }

        public void Setup()
        {
            server = new Telepathy.Server();
            server.Start(9999);
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
                            Console.WriteLine(msg.connectionId + " Connected");
                            break;
                        case Telepathy.EventType.Data:
                            Console.WriteLine(msg.connectionId + " Data: " + Encoding.UTF8.GetString(msg.data));
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
    }
}
