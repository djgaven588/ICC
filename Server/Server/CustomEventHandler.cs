using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class CustomEventHandler : EventHandler
    {
        private Dictionary<string, Action<string[], ConnectionData>> commands = new Dictionary<string, Action<string[], ConnectionData>>(); 

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
            if (commands.TryGetValue(segments[0].Substring(1), out Action<string[], ConnectionData> action))
            {
                action.Invoke(segments, connection);
            }
        }

        public CustomEventHandler()
        {
            commands.Add("nick", (string[] args, ConnectionData connection) => { ChangeNickname(args, connection); });
        }

        private void ChangeNickname(string[] args, ConnectionData connection)
        {

        }
    }
}
