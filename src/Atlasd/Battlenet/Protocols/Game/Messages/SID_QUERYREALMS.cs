using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_QUERYREALMS : Message
    {
        public SID_QUERYREALMS()
        {
            Id = (byte)MessageIds.SID_QUERYREALMS;
            Buffer = new byte[0];
        }

        public SID_QUERYREALMS(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_QUERYREALMS;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            var gameState = context.Client.GameState;

            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                        if (!Product.IsDiabloII(gameState.Product))
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from D2DV or D2XP");

                        if (Buffer.Length < 9)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be at least 9 bytes, got {Buffer.Length}");

                        /**
                         * (UINT32) Unused (0)
                         * (UINT32) Unused (0)
                         * (STRING) Unknown (empty)
                         */

                        return new SID_QUERYREALMS().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Unknown
                         * (UINT32) Count
                         * For Each Realm:
                         *     (UINT32) Unknown
                         *     (STRING) Realm title
                         *     (STRING) Realm description
                         */

                        Buffer = new byte[8];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)0);
                        w.Write((UInt32)0);

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
