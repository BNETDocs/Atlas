using System;
using System.Reflection;
using System.Threading;

namespace Atlasd
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.Name = "Main";

            var assembly = Assembly.GetCallingAssembly();
            Console.WriteLine($"[{DateTime.Now}] Welcome to {assembly.GetName().Name}!");
#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] Build: {assembly.GetName().Version} (debug)");
#else
            Console.WriteLine($"[{DateTime.Now}] Build: {assembly.GetName().Version} (release)");
#endif

            Daemon.Common.Initialize();
            Battlenet.Common.Initialize();

            Daemon.Common.Start();

            Console.WriteLine("Press enter key to terminate daemon");
            Console.ReadLine();
        }
    }
}
