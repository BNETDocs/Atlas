using Atlasd.Battlenet;
using Atlasd.Daemon;
using System;
using System.Reflection;

namespace Atlasd
{
    class Program
    {
        static void Main(string[] args)
        {
            var assembly = Assembly.GetCallingAssembly();
            Console.WriteLine($"[{DateTime.Now.ToString(Battlenet.Protocols.Common.HumanDateTimeFormat)}] Welcome to {assembly.GetName().Name}!");
#if DEBUG
            Console.WriteLine($"[{DateTime.Now.ToString(Battlenet.Protocols.Common.HumanDateTimeFormat)}] Build: {assembly.GetName().Version} (debug)");
#else
            Console.WriteLine($"[{DateTime.Now.ToString(Battlenet.Protocols.Common.HumanDateTimeFormat)}] Build: {assembly.GetName().Version} (release)");
#endif

            Common.Initialize();

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Server, $"Binding TCP listener socket to [{Common.Listener.LocalEndpoint}]");
            Common.Listener.Start();

            while (true) // Infinitely loop main thread
            {
                // Block until a connection is received ...
                new ClientState(Common.Listener.AcceptTcpClient());
            }
        }
    }
}
