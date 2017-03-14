using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zsync_the_things.Commands;

namespace Zsync_the_things
{
    class Program
    {
        static int Main(string[] args)
        {
            /*
            if (Debugger.IsAttached)
            {
                Console.WriteLine("Write Command Below");
                return ManyConsole.ConsoleCommandDispatcher.DispatchCommand(
                ManyConsole.ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(BaseCommand)), Console.ReadLine().CommandLineToArgs(),
                Console.Out);
                Console.ReadLine();

            }
            */
            return ManyConsole.ConsoleCommandDispatcher.DispatchCommand(
                ManyConsole.ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof (BaseCommand)), args,
                Console.Out);
        }
    }
}
