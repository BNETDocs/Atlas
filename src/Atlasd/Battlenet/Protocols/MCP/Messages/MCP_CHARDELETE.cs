using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using Atlasd.Helpers;

namespace Atlasd.Battlenet.Protocols.MCP.Messages
{
    class MCP_CHARDELETE : Message
    {
        public enum Statuses : UInt32
        {
            Success = 0x00,
            NotFound = 0x49,
        };

        public MCP_CHARDELETE()
        {
            Id = (byte)MessageIds.MCP_CHARDELETE;
            Buffer = new byte[0];
        }

        public MCP_CHARDELETE(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_CHARDELETE;
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
                            throw new RealmProtocolException(realmState.ClientState, $"{MessageName(Id)} must be sent from D2DV or D2XP");

                        if (Buffer.Length < 4)
                            throw new RealmProtocolException(realmState.ClientState, $"{MessageName(Id)} must be at least 4 bytes, got {Buffer.Length}");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var cookie = r.ReadUInt16();
                        var name = r.ReadByteString().AsString();

                        var character = Battlenet.Common.Realm.GetCharacter(realmState.ClientState.GameState.Username, name);

                        Statuses status;
                        if (character != null)
                        {
                            status = Statuses.Success;
                            Battlenet.Common.Realm.DeleteCharacter(realmState.ClientState.GameState.Username, name);
                        }
                        else
                        {
                            status = Statuses.NotFound;
                        }

                        return new MCP_CHARDELETE().Invoke(new MessageContext(realmState, MessageDirection.ServerToClient, new Dictionary<string, object> { { "cookie", cookie }, { "status", status } }));
                    }
                case MessageDirection.ServerToClient:
                    {
                        int count = 6;
                        Buffer = new byte[count];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt16)context.Arguments["cookie"]);
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
