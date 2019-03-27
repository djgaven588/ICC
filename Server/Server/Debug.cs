using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public static class Debug
    {
        public static void Log(string message, string from)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(from + ":");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);
        }
    }
}
