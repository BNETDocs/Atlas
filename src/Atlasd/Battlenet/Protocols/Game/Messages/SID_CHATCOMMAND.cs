using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CHATCOMMAND : Message
    {
        public SID_CHATCOMMAND()
        {
            Id = (byte)MessageIds.SID_CHATCOMMAND;
            Buffer = new byte[0];
        }

        public SID_CHATCOMMAND(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CHATCOMMAND;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_CHATCOMMAND (" + (4 + Buffer.Length) + " bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new ProtocolViolationException(context.Client.ProtocolType, "SID_CHATCOMMAND may only be transmitted from client to server");

            if (Buffer.Length < 1)
                throw new ProtocolViolationException(context.Client.ProtocolType, "SID_CHATCOMMAND buffer must be at least 1 bytes");

            string command = Encoding.ASCII.GetString(Buffer, 0, Buffer.Length - 1);

            if (command[0] != '/')
            {
                if (context.Client.GameState.ActiveChannel == null)
                    throw new ProtocolViolationException(context.Client.ProtocolType, "Cannot send message, user is not in a channel");

                context.Client.GameState.ActiveChannel.WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_TALK, context.Client.GameState.ChannelFlags, context.Client.GameState.Ping, context.Client.GameState.OnlineName, command), context.Client);
                return true;
            }

            Channel.WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.Client.GameState.ChannelFlags, context.Client.GameState.Ping, context.Client.GameState.OnlineName, "That is not a valid command. Type /help or /? for more info."), context.Client);
            return true;
        }
    }
}
