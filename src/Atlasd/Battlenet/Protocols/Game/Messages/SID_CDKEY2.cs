using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;
using System.Linq;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CDKEY2 : Message
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

        public SID_CDKEY2()
        {
            Id = (byte)MessageIds.SID_CDKEY2;
            Buffer = new byte[0];
        }

        public SID_CDKEY2(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CDKEY2;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_CDKEY2 (" + (4 + Buffer.Length) + " bytes)");

                        if (Buffer.Length < 45)
                            throw new GameProtocolViolationException(context.Client, "SID_CDKEY2 must be at least 45 bytes");
                        /**
                         * (UINT32) Spawn Key (1 is TRUE, 0 is FALSE)
                         * (UINT32) Key length
                         * (UINT32) Key Product value
                         * (UINT32) Key Public value
                         * (UINT32) Server Token
                         * (UINT32) Client Token
                         *  (UINT8) [20] Hashed Key Data
                         * (STRING) Key owner name *
                         */

                        var m = new MemoryStream(Buffer);
                        var r = new BinaryReader(m);

                        context.Client.GameState.SpawnKey = (r.ReadUInt32() == 1);
                        var keyLength = r.ReadUInt32();
                        var productValue = r.ReadUInt32();
                        var publicValue = r.ReadUInt32();
                        var serverToken = r.ReadUInt32();
                        context.Client.GameState.ClientToken = r.ReadUInt32();
                        var hashedKeyData = r.ReadBytes(20);
                        context.Client.GameState.KeyOwner = r.ReadString();

                        r.Close();
                        m.Close();

                        if (serverToken != context.Client.GameState.ServerToken)
                            throw new GameProtocolViolationException(context.Client, "SID_CDKEY2 server token mismatch");

                        var gameKey = new GameKey(keyLength, productValue, publicValue, hashedKeyData);
                        context.Client.GameState.GameKeys.Append(gameKey);

                        return new SID_CDKEY2().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Result
                         * (STRING) Key Owner
                         */

                        Buffer = new byte[5];

                        var m = new MemoryStream(Buffer);
                        var w = new BinaryWriter(m);

                        w.Write((UInt32)Statuses.Success);
                        w.Write("");

                        w.Close();
                        m.Close();

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_CDKEY2 (" + (4 + Buffer.Length) + " bytes)");
                        context.Client.Send(ToByteArray());
                        return true;
                    }
            }

            return false;
        }
    }
}
