using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlugDOS
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "PlugDOS";
            PlugDOS dos = new PlugDOS();

            dos.Boot();
            PlugDOS.WriteLine("[Thank you for using PlugDOS]", ConsoleColor.DarkGray);
            while(true) { }
        }
    }
}
