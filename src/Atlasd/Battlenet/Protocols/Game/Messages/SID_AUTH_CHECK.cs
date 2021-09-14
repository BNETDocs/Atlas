using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_AUTH_CHECK : Message
    {
        public enum Statuses : UInt32
        {
            Success = 0x000,

            // 0x0NN: (where NN is the version code supplied in SID_AUTH_INFO):
            //   Invalid version code (note that 0x100 is not set in this case)

            VersionTooOld = 0x100,
            InvalidVersion = 0x101,
            VersionTooNew  = 0x102,

            GameKeyInvalid = 0x200,
            GameKeyInUse   = 0x201,
            GameKeyBanned  = 0x202,
            GameKeyProductMismatch = 0x203,

            GameKeyExpansion = 0x010,
        }

        public SID_AUTH_CHECK()
        {
            Id = (byte)MessageIds.SID_AUTH_CHECK;
            Buffer = new byte[0];
        }

        public SID_AUTH_CHECK(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_AUTH_CHECK;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                        if (Buffer.Length < 22)
                            throw new Exceptions.GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be at least 22 bytes");
                        /**
                         * (UINT32) Client Token
                         * (UINT32) EXE Version
                         * (UINT32) EXE Hash
                         * (UINT32) Number of CD-keys in this packet
                         * (UINT32) Spawn Key (1 is TRUE, 0 is FALSE) **
                         *
                         * For each Key:
                         *    (UINT32)     Key length
                         *    (UINT32)     Key Product value 
                         *    (UINT32)     Key Public value
                         *    (UINT32)     Unknown (0)
                         *     (UINT8)[20] Hashed Key Data
                         *
                         * (STRING) EXE Information
                         * (STRING) Key owner name *
                         */

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        context.Client.GameState.ClientToken = r.ReadUInt32();
                        context.Client.GameState.Version.EXERevision = r.ReadUInt32();
                        context.Client.GameState.Version.EXEChecksum = r.ReadUInt32();

                        var numKeys = r.ReadUInt32();
                        context.Client.GameState.SpawnKey = (r.ReadUInt32() == 1);

                        // Read each key:
                        for (int i = 0; i < numKeys; i++)
                        {
                            var keyLength = r.ReadUInt32();
                            var productValue = r.ReadUInt32();
                            var publicValue = r.ReadUInt32();
                            var unknownValue = r.ReadUInt32();
                            var hashedKeyData = r.ReadBytes(20);

                            if (unknownValue != 0)
                                throw new GameProtocolViolationException(context.Client, "Invalid game key unknown value");

                            GameKey gameKey = null;
                            try
                            {
                                gameKey = new GameKey(keyLength, productValue, publicValue, hashedKeyData);
                            }
                            catch (GameProtocolViolationException)
                            {
                                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "Received invalid game key");
                                gameKey = null;
                            }
                            finally
                            {
                                context.Client.GameState.GameKeys.Add(gameKey);
                            }
                        }

                        context.Client.GameState.Version.EXEInformation = r.ReadByteString();
                        context.Client.GameState.KeyOwner = r.ReadByteString();

                        var status = Statuses.Success;
                        byte[] info = new byte[0];

                        var requiredKeyCount = GameKey.RequiredKeyCount(context.Client.GameState.Product);
                        if (context.Client.GameState.GameKeys.Count < requiredKeyCount)
                        {
                            // Incorrect number of keys
                            status = Statuses.GameKeyProductMismatch;
                            if (context.Client.GameState.GameKeys.Count >= 1)
                                status |= Statuses.GameKeyExpansion;
                        }

                        return new SID_AUTH_CHECK().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>(){
                            { "status", status }, { "info", info }
                        }));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Result
                         * (STRING) Additional Information
                         */

                        var status = (uint)(Statuses)context.Arguments["status"];
                        var info = (byte[])context.Arguments["info"];

                        Buffer = new byte[5 + info.Length];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)status);
                        w.WriteByteString(info);

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));

                        return status == (uint)Statuses.Success;
                    }
            }

            return false;
        }
    }
}
