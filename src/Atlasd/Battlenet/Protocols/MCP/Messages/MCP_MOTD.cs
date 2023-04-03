using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using Atlasd.Helpers;

namespace Atlasd.Battlenet.Protocols.MCP.Messages
{
    class MCP_MOTD : Message
    {
        public MCP_MOTD()
        {
            Id = (byte)MessageIds.MCP_MOTD;
            Buffer = new byte[0];
        }

        public MCP_MOTD(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_MOTD;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            var realmState = context.RealmState;
            var gameState = context.RealmState.ClientState.GameState;

            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_MCP, realmState.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({3 + Buffer.Length} bytes)");

                        if (!Product.IsDiabloII(gameState.Product))
                            throw new GameProtocolViolationException(realmState.ClientState, $"{MessageName(Id)} must be sent from D2DV or D2XP");

                        if (Buffer.Length != 0)
                            throw new GameProtocolViolationException(realmState.ClientState, $"{MessageName(Id)} must be 0 bytes, got {Buffer.Length}");

                        return new MCP_MOTD().Invoke(new MessageContext(realmState, MessageDirection.ServerToClient));
                    }
                case MessageDirection.ServerToClient:
                    {
                        var message = "Welcome to the Olympus realm server for Atlas!";

                        int count = 1 + message.Length + 1;
                        Buffer = new byte[count];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((byte)0xFF);
                        w.WriteByteString(message.ToBytes());

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_MCP, realmState.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({3 + Buffer.Length} bytes)");
                        realmState.Send(ToByteArray(realmState.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
