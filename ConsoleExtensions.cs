using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorsModManager
{
    public static class ConsoleC
    {
        public static void WriteLineColors(string line, ConsoleColor foreground, ConsoleColor background)
        {
            Console.BackgroundColor = background;
            Console.ForegroundColor = foreground;

            Console.WriteLine(line);
            Console.ResetColor();
        }
        public static void WriteColors(string line, ConsoleColor foreground, ConsoleColor background)
        {           
            Console.BackgroundColor = background;
            Console.ForegroundColor = foreground;

            Console.Write(line);
            Console.ResetColor();
        }
    }
}
