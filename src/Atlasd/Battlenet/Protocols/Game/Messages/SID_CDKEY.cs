using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;
using System.Linq;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CDKEY : Message
    {
        public const string KEYOWNER_TOOMANYSPAWNS = "TOO MANY SPAWNS";
        public const string KEYOWNER_NOSPAWNING = "NO SPAWNING";

        public enum Statuses : UInt32
        {
            Success = 1,
            InvalidKey = 2,
            BadProduct = 3,
            Banned = 4,
            InUse = 5,
        }

        public SID_CDKEY()
        {
            Id = (byte)MessageIds.SID_CDKEY;
            Buffer = new byte[0];
        }

        public SID_CDKEY(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CDKEY;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                        if (Buffer.Length < 6)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be at least 6 bytes");
                        /**
                         * (UINT32) Spawn Key (1 is TRUE, 0 is FALSE)
                         * (STRING) Game Key
                         * (STRING) Key owner name *
                         */

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        context.Client.GameState.SpawnKey = r.ReadUInt32() == 1;
                        context.Client.GameState.GameKeys.Append(new GameKey(r.ReadString()));
                        context.Client.GameState.KeyOwner = r.ReadByteString();

                        return new SID_CDKEY().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Result
                         * (STRING) Key Owner
                         */

                        Buffer = new byte[5];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)Statuses.Success);
                        w.Write((byte)0);

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
