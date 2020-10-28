using Atlasd.Daemon;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Atlasd
{
    class Program
    {
        // used for printing debug or release in version string
        private const string DistributionMode =
#if DEBUG
            "debug";
#else
            "release";
#endif

        // used to signal process exit from other locations in code
        public static bool Exit = false;
        public static int ExitCode = 0;

        public static async Task<int> Main(string[] args)
        {
            Thread.CurrentThread.Name = "Main";

            var assembly = typeof(Program).Assembly;
            Console.WriteLine($"[{DateTime.Now}] Welcome to {assembly.GetName().Name}!");
            Console.WriteLine($"[{DateTime.Now}] Build: {assembly.GetName().Version} ({DistributionMode})");

#if DEBUG
            // wait for debugger to attach to process

            if (!Debugger.IsAttached)
            {
                Console.WriteLine($"[{DateTime.Now}] Waiting for debugger to attach...");
            }

            while (!Debugger.IsAttached)
            {
                await Task.Yield();
            }
#endif

            Common.Initialize();
            Battlenet.Common.Initialize();

            await Task.Run(() => { Common.Start(); });

            while (!Exit)
            {
                await Task.Yield();
            }

            return ExitCode;
        }
    }
}
