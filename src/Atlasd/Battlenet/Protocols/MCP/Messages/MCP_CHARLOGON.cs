using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using Atlasd.Helpers;

namespace Atlasd.Battlenet.Protocols.MCP.Messages
{
    class MCP_CHARLOGON : Message
    {
        public enum Statuses : UInt32
        {
            Success = 0x00,
            NotFound = 0x46,
            Failed = 0x7A,
            Expired = 0x7B,
        };

        // set gamestate char name
        public MCP_CHARLOGON()
        {
            Id = (byte)MessageIds.MCP_CHARLOGON;
            Buffer = new byte[0];
        }

        public MCP_CHARLOGON(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_CHARLOGON;
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

                        if (Buffer.Length < 2)
                            throw new GameProtocolViolationException(realmState.ClientState, $"{MessageName(Id)} must be at least 2 bytes, got {Buffer.Length}");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var name = r.ReadByteString().AsString();

                        var character = Battlenet.Common.Realm.GetCharacter(realmState.ClientState.GameState.Username, name);

                        Statuses status;
                        if (character != null)
                        {
                            status = Statuses.Success;

                            realmState.ActiveCharacter = character;
                            realmState.ClientState.GameState.CharacterName = character.Name.ToBytes();
                            var statstring = realmState.ClientState.GenerateDiabloIIStatstring();
                            gameState.Statstring = statstring;
                        }
                        else
                        {
                            status = Statuses.NotFound;
                        }

                        return new MCP_CHARLOGON().Invoke(new MessageContext(realmState, MessageDirection.ServerToClient, new Dictionary<string, object> { { "status", status } }));
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
