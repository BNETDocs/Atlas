using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_REGISTRY : Message
    {
        public enum HiveKeyIds : UInt32
        {
            HKEY_CLASSES_ROOT     = 0x80000000,
            HKEY_CURRENT_USER     = 0x80000001,
            HKEY_LOCAL_MACHINE    = 0x80000002,
            HKEY_USERS            = 0x80000003,
            HKEY_PERFORMANCE_DATA = 0x80000004,
            HKEY_CURRENT_CONFIG   = 0x80000005,
            HKEY_DYN_DATA         = 0x80000006,
        }

        public SID_REGISTRY()
        {
            Id = (byte)MessageIds.SID_REGISTRY;
            Buffer = new byte[0];
        }

        public SID_REGISTRY(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_REGISTRY;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                        if (Buffer.Length < 5)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 5 bytes");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var cookie = r.ReadUInt32();
                        var value = r.ReadByteString();

                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Requested registry cookie [0x{cookie:X8}] value [{Encoding.UTF8.GetString(value)}]");
                        return true;
                    }
                case MessageDirection.ServerToClient:
                    {
                        var cookie = (UInt32)context.Arguments["cookie"];
                        var hiveKeyId = (UInt32)context.Arguments["hiveKeyId"];
                        var keyPath = (string)context.Arguments["keyPath"];
                        var keyName = (string)context.Arguments["keyName"];

                        Buffer = new byte[10 + Encoding.UTF8.GetByteCount(keyPath) + Encoding.UTF8.GetByteCount(keyName)];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)cookie);
                        w.Write((UInt32)hiveKeyId);
                        w.Write((string)keyPath);
                        w.Write((string)keyName);

                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Requesting registry cookie [0x{cookie}:X8] hive [0x{hiveKeyId:X8}] key path [{keyPath}] name [{keyName}]");
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
