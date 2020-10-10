using Atlasd.Battlenet;
using Atlasd.Daemon;
using System;
using System.Reflection;
using System.Threading;

namespace Atlasd
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(DateTime.Now.ToString("ddd, dd MMM yyyy, h:mm tt")); // Mon, 23 Dec 2019, 4:32 AM

            var version = Assembly.GetEntryAssembly();
            Console.WriteLine(version.GetName().Name + " v" + version.GetName().Version.ToString());

            Common.Initialize();

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, "Binding socket...");
            Common.Listener.Start();

            while (true) // Infinitely loop main thread
            {
                // Block until a connection is received ...
                new ClientState(Common.Listener.AcceptTcpClient());
            }
        }
    }
}
