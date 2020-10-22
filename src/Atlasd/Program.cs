using Atlasd.Daemon;
using System;
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

        public static bool Exit = false;
        public static int ExitCode = 0;

        public static async Task<int> Main(string[] args)
        {
            Thread.CurrentThread.Name = "Main";

            var assembly = typeof(Program).Assembly;
            Console.WriteLine($"[{DateTime.Now}] Welcome to {assembly.GetName().Name}!");
            Console.WriteLine($"[{DateTime.Now}] Build: {assembly.GetName().Version} ({DistributionMode})");

            Common.Initialize();
            Battlenet.Common.Initialize();

            await Task.Run(() => { Common.Start(); });

            while (!Exit)
            {
                await Task.Delay(10);
            }

            return ExitCode;
        }
    }
}
