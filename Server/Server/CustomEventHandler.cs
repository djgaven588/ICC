using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class CustomEventHandler : EventHandler
    {
        private Dictionary<string, Command> commands = new Dictionary<string, Command>(); 

        public override void OnMessageReceived(string msgContents, ref ConnectionData connection)
        {
            if (!msgContents.StartsWith("/"))
            {
                base.OnMessageReceived(msgContents, ref connection);
                return;
            }

            HandleCommand(msgContents, ref connection);
        }

        private void HandleCommand(string command, ref ConnectionData connection)
        {
            string[] segments = command.Split(' ');
            if (commands.TryGetValue(segments[0].Substring(1), out Command action))
            {
                connection = action(segments, connection);
            }
            else
            {
                connection.SendMessage($"The command '{segments[0]}' is not an available command. Use /help to help you get started with commands.");
            }
        }

        public CustomEventHandler()
        {
            Command cmdNick = ChangeNicknameMethod;
            Command cmdHelp = HelpMethod;
            commands.Add("nick", cmdNick);
            commands.Add("help", cmdHelp);
        }

        private delegate ConnectionData Command(string[] args, ConnectionData connection);

        private ConnectionData ChangeNicknameMethod(string[] args, ConnectionData connection)
        {
            if (args.Length <= 1)
            {
                connection.SendMessage("The 'nick' command requires you to specify what your new nickname is, please try again with the correct parameters.");
                return connection;
            }

            string oldUsername = args[1];
            bool result = connection.SetNickname(args[1]);
            if (result)
            {
                connection.SendMessage($"The nickname '{args[1]}' was available and valid! Your new nickname is set!");
                Program.currentUsernames.Remove(oldUsername);
            }
            else
            {
                connection.SendMessage($"The nickname '{args[1]}' was not availible, or may not have been valid. Please try again with another nickname.");
            }
            return connection;
        }

        private ConnectionData HelpMethod(string[] args, ConnectionData connection)
        {
            connection.SendMessage("--Help Center--\r\n \r\n-Available Commands-");
            foreach (string item in commands.Keys)
            {
                connection.SendMessage("/" + item);
            }
            return connection;
        }
    }
}
