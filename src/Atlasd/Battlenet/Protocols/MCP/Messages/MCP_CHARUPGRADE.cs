using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Atlasd.Battlenet.Exceptions;
using Atlasd.Battlenet.Protocols.MCP.Models;
using Atlasd.Daemon;
using Atlasd.Helpers;

namespace Atlasd.Battlenet.Protocols.MCP.Messages
{
    class MCP_CHARUPGRADE : Message
    {
        public enum Statuses : UInt32
        {
            Success     = 0x00,
            NotFound    = 0x46,
            Failed      = 0x7A,
            Expired     = 0x7B,
            Already     = 0x7C
        };

        public MCP_CHARUPGRADE()
        {
            Id = (byte)MessageIds.MCP_CHARUPGRADE;
            Buffer = new byte[0];
        }

        public MCP_CHARUPGRADE(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_CHARUPGRADE;
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

                        if (Buffer.Length < 2)
                            throw new RealmProtocolException(realmState.ClientState, $"{MessageName(Id)} must be at least 2 bytes, got {Buffer.Length}");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var name = r.ReadByteString().AsString();

                        var character = Battlenet.Common.Realm.GetCharacter(realmState.ClientState.GameState.Username, name);

                        Statuses status;
                        if (character != null)
                        {
                            if ((character.Flags & CharacterFlags.Expansion) == CharacterFlags.Expansion)
                            {
                                status = Statuses.Already;
                            }
                            else
                            {
                                status = Statuses.Success;
                                character.Flags = character.Flags | CharacterFlags.Expansion;
                                character.Statstring.Flags = (byte)(character.Statstring.Flags | (byte)CharacterFlags.Expansion);
                            }
                        }
                        else
                        {
                            status = Statuses.NotFound;
                        }

                        return new MCP_CHARUPGRADE().Invoke(new MessageContext(realmState, MessageDirection.ServerToClient, new Dictionary<string, object> { { "status", status } }));
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
