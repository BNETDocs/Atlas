using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_UDPPINGRESPONSE : Message
    {
        public SID_UDPPINGRESPONSE()
        {
            Id = (byte)MessageIds.SID_UDPPINGRESPONSE;
            Buffer = new byte[0];
        }

        public SID_UDPPINGRESPONSE(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_UDPPINGRESPONSE;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_UDPPINGRESPONSE ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, "SID_UDPPINGRESPONSE must be sent from client to server");

            if (Buffer.Length != 4)
                throw new GameProtocolViolationException(context.Client, "SID_UDPPINGRESPONSE buffer must be 4 bytes");

            // UDP reply must be given between S>C SID_AUTH_INFO and C>S SID_ENTERCHAT, not
            // before or after.Technically, we could allow this while in chat and proceed then
            // by sending a chat event for the update, but the server by convention is not
            // supposed to allow it and should instead disconnect the client.
            if (!(context.Client.GameState.Statstring == null || context.Client.GameState.Statstring.Length == 0))
                throw new GameProtocolViolationException(context.Client, "SID_UDPPINGRESPONSE cannot be sent while in chat");

            UInt32 udpToken;
            using (var m = new MemoryStream(Buffer))
            using (var r = new BinaryReader(m))
                udpToken = r.ReadUInt32();

            context.Client.GameState.UDPSupported = udpToken == 0x626E6574; // "bnet"
            return true;
        }
    }
}
