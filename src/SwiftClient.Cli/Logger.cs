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
            Console.WriteLine(value);
            Console.ResetColor();
        }
    }
}
