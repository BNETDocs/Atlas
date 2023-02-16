using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_QUERYREALMS2 : Message
    {
        public SID_QUERYREALMS2()
        {
            Id = (byte)MessageIds.SID_QUERYREALMS2;
            Buffer = new byte[0];
        }

        public SID_QUERYREALMS2(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_QUERYREALMS2;
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

                        if (Buffer.Length != 0)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be 0 bytes, got {Buffer.Length}");

                        return new SID_QUERYREALMS2().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Unknown (0)
                         * (UINT32) Count
                         * For each Realm:
                         *     (UINT32) Unknown (1)
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
