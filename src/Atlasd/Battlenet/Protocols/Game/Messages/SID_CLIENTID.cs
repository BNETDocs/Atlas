using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CLIENTID : Message
    {
        public SID_CLIENTID()
        {
            Id = (byte)MessageIds.SID_CLIENTID;
            Buffer = new byte[16];
        }

        public SID_CLIENTID(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CLIENTID;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_CLIENTID ({4 + Buffer.Length} bytes)");

            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        /**
                         * (UINT32) Registration Version
                         * (UINT32) Registration Authority
                         * (UINT32) Account Number
                         * (UINT32) Registration Token
                         * (STRING) LAN Computer Name
                         * (STRING) LAN Username
                         */

                        if (Buffer.Length < 18)
                            throw new GameProtocolViolationException(context.Client, "SID_CLIENTID buffer must be at least 18 bytes");

                        /*var m = new MemoryStream(Buffer);
                        var r = new BinaryReader(m);

                        var registrationVersion = r.ReadUInt32();
                        var registrationAuthority = r.ReadUInt32();
                        var accountNumber = r.ReadUInt32();
                        var registrationToken = r.ReadUInt32();
                        var pcComputerName = r.ReadString();
                        var pcUserName = r.ReadString();

                        r.Close();
                        m.Close();*/

                        return new SID_CLIENTID().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient)) &&
                            new SID_LOGONCHALLENGEEX().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient)) &&
                            new SID_PING().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new System.Collections.Generic.Dictionary<string, object>() { { "token", context.Client.GameState.PingToken } }));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Registration Version
                         * (UINT32) Registration Authority
                         * (UINT32) Account Number
                         * (UINT32) Registration Token 
                         */

                        Buffer = new byte[16];

                        var m = new MemoryStream(Buffer);
                        var w = new BinaryWriter(m);

                        w.Write((UInt32)0); // Registration version (Defunct)
                        w.Write((UInt32)0); // Registration authority (Defunct)
                        w.Write((UInt32)0); // Account number (Defunct)
                        w.Write((UInt32)0); // Registration token (Defunct)

                        w.Close();
                        m.Close();

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_CLIENTID ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}