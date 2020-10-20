using System;
using System.Net;
using System.Runtime.InteropServices;

namespace Atlasd.Daemon
{
    class Logging
    {
        public static LogLevel CurrentLogLevel = LogLevel.Debug;

        public enum LogLevel
        {
            Error,
            Warning,
            Info,
            Debug,
        };

        public enum LogType
        {
            Channel,
            Config,
            Client,
            Client_BNFTP,
            Client_Chat,
            Client_Game,
            Client_IPC,
            Client_MCP,
            Client_UDP,
            Server,
        };

        public static string LogLevelToString(LogLevel level)
        {
            return level switch
            {
                LogLevel.Error => "Error",
                LogLevel.Warning => "Warning",
                LogLevel.Info => "Info",
                LogLevel.Debug => "Debug",
                _ => throw new IndexOutOfRangeException("Unknown log level"),
            };
        }

        public static string LogTypeToString(LogType type)
        {
            return type switch
            {
                LogType.Channel => "Channel",
                LogType.Config => "Config",
                LogType.Client => "Client",
                LogType.Client_BNFTP => "Client_BNFTP",
                LogType.Client_Chat => "Client_Chat",
                LogType.Client_Game => "Client_Game",
                LogType.Client_IPC => "Client_IPC",
                LogType.Client_MCP => "Client_MCP",
                LogType.Client_UDP => "Client_UDP",
                LogType.Server => "Server",
                _ => throw new IndexOutOfRangeException("Unknown log type"),
            };
        }

        public static void WriteLine(LogLevel level, LogType type, string buffer)
        {
            if (level > CurrentLogLevel) return;

            lock (Console.Out) Console.Out.WriteLine($"[{DateTime.Now}] [{LogLevelToString(level)}] [{LogTypeToString(type).Replace("_", "] [")}] {buffer}");
        }

        public static void WriteLine(LogLevel level, LogType type, EndPoint endp, string buffer)
        {
            WriteLine(level, type, $"[{endp}] {buffer}");
        }
    }
}
