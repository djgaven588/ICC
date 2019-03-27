using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public struct ConnectionData
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
}
