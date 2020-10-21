using Atlasd.Daemon;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Atlasd
{
    class Program
    {
        private const string DistributionMode =
#if DEBUG
            "debug";
#else
            "release";
#endif

        public static async Task Main(string[] args)
        {
            Thread.CurrentThread.Name = "Main";

            var assembly = typeof(Program).Assembly;
            Console.WriteLine($"[{DateTime.Now}] Welcome to {assembly.GetName().Name}!");
            Console.WriteLine($"[{DateTime.Now}] Build: {assembly.GetName().Version} ({DistributionMode})");

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
