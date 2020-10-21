using Atlasd.Daemon;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Atlasd
{
    class Program
    {
        public static async Task Main(string[] args)
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

            await Task.Run(() => { Daemon.Common.Start(); });

            while (true)
            {
                Thread.Sleep(10);
            }
        }
    }
}
