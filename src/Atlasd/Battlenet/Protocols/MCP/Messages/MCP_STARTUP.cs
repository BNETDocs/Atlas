using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;

namespace Atlasd.Battlenet.Protocols.MCP.Messages
{
    class MCP_STARTUP : Message
    {
        public enum Statuses : UInt32
        {
            Success = 1,
            Unavailable = 2,
        };

        public MCP_STARTUP()
        {
            Id = (byte)MessageIds.MCP_STARTUP;
            Buffer = new byte[0];
        }

        public MCP_STARTUP(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_STARTUP;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            var realmState = context.RealmState;

            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_MCP, realmState.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({3 + Buffer.Length} bytes)");

                        if (Buffer.Length < 65)
                            throw new GameProtocolViolationException(realmState.ClientState, $"{MessageName(Id)} must be at least 65 bytes, got {Buffer.Length}");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var cookie = r.ReadUInt32();
                        ClientState clientState;

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_MCP, realmState.RemoteEndPoint, $"Received realm cookie 0x{cookie:X4}");

                        Battlenet.Common.RealmClientStates.TryGetValue(cookie, out clientState);
                        if (clientState != null)
                        {
                            clientState.RealmState = realmState;
                            realmState.ClientState = clientState;

                            if (!Product.IsDiabloII(clientState.GameState.Product))
                                throw new GameProtocolViolationException(realmState.ClientState, $"{MessageName(Id)} must be sent from D2DV or D2XP");

                            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_MCP, realmState.RemoteEndPoint, $"Realm cookie [0x{cookie:X4}] found and associated");
                            return new MCP_STARTUP().Invoke(new MessageContext(realmState, MessageDirection.ServerToClient, new Dictionary<string, object> { { "status", Statuses.Success } }));
                        }
                        else
                        {
                            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_MCP, realmState.RemoteEndPoint, $"Realm cookie [0x{cookie:X4}] does not exist");
                            return new MCP_STARTUP().Invoke(new MessageContext(realmState, MessageDirection.ServerToClient, new Dictionary<string, object> { { "status", Statuses.Unavailable } }));
                        }
                    }
                case MessageDirection.ServerToClient:
                    {
                        int count = 4;
                        Buffer = new byte[count];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)context.Arguments["status"]);

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_MCP, realmState.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({3 + Buffer.Length} bytes)");
                        realmState.Send(ToByteArray(realmState.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
