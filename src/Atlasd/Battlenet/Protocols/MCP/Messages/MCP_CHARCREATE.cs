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
    class MCP_CHARCREATE: Message
    {
        public enum Statuses : UInt32
        {
            Success         = 0x00,
            AlreadyExists   = 0x14,
            Invalid         = 0x15,
        };

        public MCP_CHARCREATE()
        {
            Id = (byte)MessageIds.MCP_CHARCREATE;
            Buffer = new byte[0];
        }

        public MCP_CHARCREATE(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_CHARCREATE;
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

                        if (Buffer.Length < 5)
                            throw new RealmProtocolException(realmState.ClientState, $"{MessageName(Id)} must be at least 5 bytes, got {Buffer.Length}");

                        //(UINT32) Character class
                        //(UINT16) Character flags
                        //(STRING) Character name

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var type    = (CharacterTypes)(r.ReadUInt32() + 1); // i think this field is documented incorrectly
                        var flags   = (CharacterFlags)(r.ReadUInt16());
                        var name    = r.ReadByteString().AsString();

                        if (name.Length < 2)
                        {
                            return new MCP_CHARCREATE().Invoke(new MessageContext(realmState, MessageDirection.ServerToClient, new Dictionary<string, object> { { "status", Statuses.Invalid } }));
                        }

                        var characters = Battlenet.Common.Realm.GetCharacters(realmState.ClientState.GameState.Username);
                        Character character;
                        characters.TryGetValue(name, out character);

                        Statuses status;
                        if (character == null)
                        {
                            status = Statuses.Success;
                            character = new Character(
                                name, type, flags,
                                (flags & CharacterFlags.Ladder) == CharacterFlags.Ladder ? LadderTypes.Season_1 : LadderTypes.NonLadder
                            );
                            Battlenet.Common.Realm.AddCharacter(realmState.ClientState.GameState.Username, name, character);
                        }
                        else
                        {
                            //TODO: make names globally unique across accounts as well
                            status = Statuses.AlreadyExists;
                        }

                        return new MCP_CHARCREATE().Invoke(new MessageContext(realmState, MessageDirection.ServerToClient, new Dictionary<string, object> { { "status", status } }));
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
