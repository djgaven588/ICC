using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public abstract class EventHandler
    {
        public virtual void OnConnect(int connectionId)
        {
            Program.connections.Add(connectionId, new ConnectionData(connectionId));
            Program.connections[connectionId].SendMessage(Program.passwordProtected ? "passwordProtected" : "");
            Program.connections[connectionId].SendMessage(Program.passwordProtected ? "PRESHARED-AES" : "N/A");
            Debug.Log("A user has connected, they have been notified of the servers password protection status.", "New Connection");
        }

        public virtual void OnDisconnected(int connectionId)
        {
            ConnectionData connectData = Program.connections[connectionId];
            connectData.RemoveConnection();
            Debug.Log((connectData.GetNickname() != null ? "User " + connectData.GetNickname() : "Unknown User") + " Disconnected", "Disconnection");
            if (connectData.GetNickname() != null)
            {
                Program.BroadcastMsg("A user who went by " + connectData.GetNickname() + ", has left the chat... They shall me missed!");
            }
        }

        public virtual void OnEncryptionSupported(string response, ref ConnectionData connection)
        {
            if (response == "Yes")
            {
                connection.SetStage(ConnectionData.ConnectionStage.AwaitingEncryptionSetup);
            }
            else
            {
                Program.KickUser(connection, "Client does not support the encryption required by the server.");
            }
        }

        public virtual void OnEncryptionSetup(string response, ref ConnectionData connection)
        {
            if (Program.passwordProtected)
            {
                try
                {
                    OnPresharedAESCheck(PresharedKeyEncryption.AESDecrypt(Convert.FromBase64String(response), PresharedKeyEncryption.GetAESHash(Program.password)), ref connection);
                }
                catch
                {
                    connection.SendMessage("Failed authentication, please try again. If you know the password you entered is correct, please contact your server admin.");
                }
            }
            else
            {
                OnNoEncryption(ref connection);
            }
        }

        public virtual void OnPresharedAESCheck(string passwordToCheck, ref ConnectionData connection)
        {
            if (passwordToCheck == Program.password)
            {
                Debug.Log("A user has established communication. Waiting for a username...", "User Login Success");
                connection.SetStage(ConnectionData.ConnectionStage.AwaitingNickname);
                connection.SendMessage("V");
                string newKey = Convert.ToBase64String(PresharedKeyEncryption.GenerateAESKey());
                connection.SetAESKey(PresharedKeyEncryption.GetAESHash(Program.password));
                connection.SendMessage(newKey);
                connection.SetAESKey(newKey);
                connection.SendMessage("Welcome to the server client! Please send your nickname.");
            }
            else
            {
                Debug.Log("A user tried to connect with a password which didn't match. Notifying user.", "User Login Failed");
                connection.SendMessage("Your password was invalid. Please try again.");
            }
        }

        public virtual void OnNoEncryption(ref ConnectionData connection)
        {
            Debug.Log("A user has established communication. Waiting for a username...", "User Connected");
            connection.SetStage(ConnectionData.ConnectionStage.AwaitingNickname);
            connection.SendMessage("Welcome to the server client! Please send your nickname.");
        }

        public virtual void OnUsernameSetAttempt(string msgContents, ref ConnectionData connection)
        {
            Debug.Log("A user has requested to use the username '" + msgContents + "'", "Nickname");
            bool result = connection.SetNickname(msgContents);
            if (result)
            {
                Debug.Log("The username '" + msgContents + "' was available and valid. Welcome " + msgContents + "!", "Nickname");
                connection.SetStage(ConnectionData.ConnectionStage.ConnectionEstablished);
                connection.SendMessage("Welcome " + connection.GetNickname() + "!");
                connection.SendMessage("V");
                connection.SendMessage("Server: Daily Message:\n" + Program.dailyMsg);
                Program.BroadcastMsg("A user who goes by the name of " + msgContents + ", has joined the chat!");
            }
            else
            {
                Debug.Log("The username '" + msgContents + "' was not available or not valid. The user was notified.", "Nickname");
                connection.SendMessage("Your username is either too long, too short, or is already in use!\nMake sure your username is unique, greather than 3 characters, and less than 16 characters!");
            }
        }

        public virtual void OnMessageReceived(string msgContents, ref ConnectionData connection)
        {
            Debug.Log(connection.GetNickname() + " said: " + msgContents, "User Message");
            Program.BroadcastMsg(connection.GetNickname() + ": " + msgContents);
        }
    }
}