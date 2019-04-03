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
        public string aesCommunicationKey;

        public void RemoveConnection()
        {
            if (nickname != null)
            {
                Program.currentUsernames.Remove(nickname);
            }
            Program.connections.Remove(connectionId);
        }

        public void SendMessage(string data)
        {
            Program.server.Send(connectionId,
                        Encoding.UTF8.GetBytes(
                            (aesCommunicationKey != null) ? Convert.ToBase64String(PresharedKeyEncryption.AESEncrypt(data, aesCommunicationKey)) : data
                        ));
        }

        public void SetStage(ConnectionStage stage)
        {
            this.currentStage = stage;
        }

        public bool SetNickname(string nick)
        {
            if (nick.Length < 16 && nick.Length > 3 && !Program.currentUsernames.Contains(nick))
            {
                nickname = nick;
                Program.currentUsernames.Add(nick);
                return true;
            }
            return false;
        }

        public ConnectionData(int connectionId)
        {
            this.connectionId = connectionId;
            currentStage = ConnectionStage.AwaitingEncryptionSupport;
            nickname = null;
            aesCommunicationKey = null;
        }

        public enum ConnectionStage
        {
            AwaitingEncryptionSupport,
            AwaitingEncryptionSetup,
            AwaitingNickname,
            ConnectionEstablished
        }
    }
}
