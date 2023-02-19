using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_LOGONREALMEX : Message
    {
        public enum Statuses : UInt32
        {
            RealmUnavailable = 0x80000001,
            RealmLogonFailed = 0x80000002,
        }

        public SID_LOGONREALMEX()
        {
            Id = (byte)MessageIds.SID_LOGONREALMEX;
            Buffer = new byte[0];
        }

        public SID_LOGONREALMEX(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_LOGONREALMEX;
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

                        if (Buffer.Length < 25)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be at least 25 bytes, got {Buffer.Length}");

                        /**
                         * (UINT32) Client Token
                         * (UINT8)[20] Hashed realm password
                         * (STRING) Realm title
                         */

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var clientToken = r.ReadUInt32();
                        var inPassword = r.ReadBytes(20);
                        var realmTitle = r.ReadByteString();

                        return new SID_LOGONREALMEX().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>{
                            { "clientToken", clientToken }, { "inPassword", inPassword }, { "realmTitle", realmTitle }
                        }));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * [Note this format is slightly different from BNETDocs reference as of 2023-02-18]
                         * (UINT32)     MCP Cookie
                         * (UINT32)     MCP Status
                         * (UINT32)[2]  MCP Chunk 1
                         * (UINT32)     IP
                         * (UINT32)     Port
                         * (UINT32)[12] MCP Chunk 2
                         * (STRING)     Battle.net unique name (* as of D2 1.14d, this is empty)
                         */

                        Buffer = new byte[8]; // MCP Cookie + MCP Status only; Atlas does not have realm/MCP servers implemented yet.

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)(new Random()).Next()); // Cookie is randomized.
                        w.Write((UInt32)Statuses.RealmUnavailable); // Atlas does not have realm/MCP servers implemented yet.

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
