using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CLIENTID2 : Message
    {
        public SID_CLIENTID2()
        {
            Id = (byte)MessageIds.SID_CLIENTID2;
            Buffer = new byte[16];
        }

        public SID_CLIENTID2(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CLIENTID2;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_CLIENTID2 (" + (4 + Buffer.Length) + " bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, "SID_CLIENTID2 must be sent from client to server");

            /**
             * (UINT32) Server version
             * 
             * For server version 0:
             *   (UINT32) Registration authority
             *   (UINT32) Registration version
             * 
             * For server version 1:
             *   (UINT32) Registration version
             *   (UINT32) Registration authority
             * 
             * (UINT32) Account number
             * (UINT32) Registration token
             * (STRING) LAN computer name
             * (STRING) LAN username
             */

            if (Buffer.Length < 22)
                throw new GameProtocolViolationException(context.Client, "SID_CLIENTID2 buffer must be at least 22 bytes");

            var m = new MemoryStream(Buffer);
            var r = new BinaryReader(m);
                        
            var serverVersion = r.ReadUInt32();

            UInt32 registrationAuthority;
            UInt32 registrationVersion;

            switch (serverVersion)
            {
                case 0: {
                    registrationAuthority = r.ReadUInt32();
                    registrationVersion = r.ReadUInt32();
                    break;
                }
                case 1: {
                    registrationVersion = r.ReadUInt32();
                    registrationAuthority = r.ReadUInt32();
                    break;
                }
                default:
                    throw new GameProtocolViolationException(context.Client, string.Format("SID_CLIENTID2 has invalid server version [{0:d}]", serverVersion));
            }

            var accountNumber = r.ReadUInt32();
            var registrationToken = r.ReadUInt32();
            var pcComputerName = r.ReadString();
            var pcUserName = r.ReadString();

            r.Close();
            m.Close();

            return new SID_CLIENTID().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient)) &&
                new SID_LOGONCHALLENGEEX().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient));
        }
    }
}