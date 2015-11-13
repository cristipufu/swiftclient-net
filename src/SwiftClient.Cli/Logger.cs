using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SwiftClient.Cli
{
    public static class Logger
    {
        public static void Log(string value)
        {
            Console.ResetColor();
            Console.WriteLine(value);
        }

        public static void LogWarning(string value)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(value);
            Console.ResetColor();
        }

        public static void LogError(string value)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            //TODO: determine if the current cursor is on a new line or not
            Console.Write(Environment.NewLine);
            Console.WriteLine(value);
            Console.ResetColor();
        }
    }
}
