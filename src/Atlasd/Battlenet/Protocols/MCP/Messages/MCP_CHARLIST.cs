using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using Atlasd.Helpers;

namespace Atlasd.Battlenet.Protocols.MCP.Messages
{
    class MCP_CHARLIST : Message
    {
        public MCP_CHARLIST()
        {
            Id = (byte)MessageIds.MCP_CHARLIST;
            Buffer = new byte[0];
        }

        public MCP_CHARLIST(byte[] buffer)
        {
            Id = (byte)MessageIds.MCP_CHARLIST;
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
                            throw new RealmProtocolException(realmState.ClientState, $"{MessageName(Id)} must be sent from DDV or DXP");

                        if (Buffer.Length < 4)
                            throw new RealmProtocolException(realmState.ClientState, $"{MessageName(Id)} must be at least 4 bytes, got {Buffer.Length}");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var requested = r.ReadUInt32();

                        return new MCP_CHARLIST().Invoke(new MessageContext(realmState, MessageDirection.ServerToClient, new Dictionary<string, object> { { "requested", requested } }));
                    }
                case MessageDirection.ServerToClient:
                    {
                        int count = 8;
                        var characters = Battlenet.Common.Realm.GetCharacters(realmState.ClientState.GameState.Username);
                        var characterCount = characters.Count;

                        foreach (var kv in characters)
                        {
                            var character = kv.Value;

                            count += 4;
                            count += character.Name.Length + 1;
                            count += character.Statstring.ToBytes().Length + 1;
                        }

                        Buffer = new byte[count];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt16)(characterCount == 0 ? 1 : 1)); // i think this field is documented incorrectly
                        w.Write((UInt32)(characterCount));
                        w.Write((UInt16)(characterCount));

                        foreach (var kv in characters)
                        {
                            var name = kv.Key;
                            var character = kv.Value;

                            w.WriteByteString(character.Name.ToBytes());
                            w.WriteByteString(character.Statstring.ToBytes());
                        }

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_MCP, realmState.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({3 + Buffer.Length} bytes)");
                        realmState.Send(ToByteArray(realmState.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}