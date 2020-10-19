using Atlasd.Daemon;
using System;
using System.IO;
using System.Linq;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_AUTH_CHECK : Message
    {
        public SID_AUTH_CHECK()
        {
            Id = (byte)MessageIds.SID_AUTH_CHECK;
            Buffer = new byte[0];
        }

        public SID_AUTH_CHECK(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_AUTH_CHECK;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_AUTH_CHECK ({4 + Buffer.Length} bytes)");

                        if (Buffer.Length < 22)
                            throw new Exceptions.GameProtocolViolationException(context.Client, "SID_AUTH_CHECK must be at least 22 bytes");
                        /**
                         * (UINT32) Client Token
                         * (UINT32) EXE Version
                         * (UINT32) EXE Hash
                         * (UINT32) Number of CD-keys in this packet
                         * (UINT32) Spawn Key (1 is TRUE, 0 is FALSE) **
                         *
                         * For each Key:
                         *    (UINT32)     Key length
                         *    (UINT32)     Key Product value 
                         *    (UINT32)     Key Public value
                         *    (UINT32)     Unknown (0)
                         *     (UINT8)[20] Hashed Key Data
                         *
                         * (STRING) EXE Information
                         * (STRING) Key owner name *
                         */

                        var m = new MemoryStream(Buffer);
                        var r = new BinaryReader(m);

                        context.Client.GameState.ClientToken = r.ReadUInt32();
                        context.Client.GameState.Version.EXERevision = r.ReadUInt32();
                        context.Client.GameState.Version.EXEChecksum = r.ReadUInt32();

                        var numKeys = r.ReadUInt32();
                        context.Client.GameState.SpawnKey = (r.ReadUInt32() == 1);

                        // Read each key:
                        for (int i = 0; i < numKeys; i++)
                        {
                            var keyLength = r.ReadUInt32();
                            var productValue = r.ReadUInt32();
                            var publicValue = r.ReadUInt32();
                            var unknownValue = r.ReadUInt32();
                            var hashedKeyData = r.ReadBytes(20);

                            if (unknownValue != 0)
                                throw new Exceptions.GameProtocolViolationException(context.Client, "Invalid game key unknown value");

                            var gameKey = new GameKey(keyLength, productValue, publicValue, hashedKeyData);
                            context.Client.GameState.GameKeys.Append(gameKey);
                        }

                        context.Client.GameState.Version.EXEInformation = r.ReadString();
                        context.Client.GameState.KeyOwner = r.ReadString();

                        r.Close();
                        m.Close();

                        return new SID_AUTH_CHECK().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Result
                         * (STRING) Additional Information
                         */

                        Buffer = new byte[5];

                        var m = new MemoryStream(Buffer);
                        var w = new BinaryWriter(m);

                        w.Write((UInt32)0);
                        w.Write("");
                        
                        w.Close();
                        m.Close();

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_AUTH_CHECK ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray());
                        return true;
                    }
            }

            return false;
        }
    }
}
