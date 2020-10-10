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
                var client = new Battlenet.Sockets.ClientState(Common.Listener.AcceptTcpClient());

                // Spawn a new thread to handle this connection ...
                (new Thread(() =>
                {
                    while (true) // Infinitely loop childSocketThread ...
                    {
                        var bCloseConnection = true;
                        try
                        {
                            if (!client.Receive()) break; // ... unless Receive() or
                            if (!client.Invoke()) break; // ... Invoke() return false
                            bCloseConnection = false;
                            continue;
                        }
                        /*catch (SocketException ex)
                        {
                            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, client.RemoteEndPoint, "TCP connection lost!" + (ex.Message.Length > 0 ? " " + ex.Message : ""));
                        }
                        catch (Exception ex)
                        {
                            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client, client.RemoteEndPoint, ex.GetType().Name + " error encountered!" + (ex.Message.Length > 0 ? " " + ex.Message : ""));
                        }*/
                        finally
                        {
                            if (client != null && bCloseConnection)
                                client.Close();
                        }
                        break;
                    }
                })).Start();
            }
            
        }
    }
}
