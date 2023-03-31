using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                        Dictionary<byte[], byte[]> realms =
                            context.Arguments == null || !context.Arguments.ContainsKey("realms") ?
                            new Dictionary<byte[], byte[]>() :
                            (Dictionary<byte[], byte[]>)context.Arguments["realms"];

                        /**
                         * (UINT32) Unknown (0)
                         * (UINT32) Count
                         * For each Realm:
                         *     (UINT32) Unknown (1)
                         *     (STRING) Realm title
                         *     (STRING) Realm description
                         */

                        int count = 8;
                        foreach (var pair in realms) count += pair.Key.Length + pair.Value.Length + 6;
                        Buffer = new byte[count];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)0);
                        w.Write((UInt32)realms.Count);
                        foreach (var pair in realms)
                        {
                            w.Write((UInt32)1);
                            w.WriteByteString(pair.Key);
                            w.WriteByteString(pair.Value);
                        }

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
