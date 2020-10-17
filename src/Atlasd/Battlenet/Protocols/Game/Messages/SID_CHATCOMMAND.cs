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
                throw new GameProtocolViolationException(context.Client, "SID_CHATCOMMAND may only be transmitted from client to server");

            if (Buffer.Length < 1)
                throw new GameProtocolViolationException(context.Client, "SID_CHATCOMMAND buffer must be at least 1 bytes");

            var text = Encoding.ASCII.GetString(Buffer, 0, Buffer.Length - 1);

            if (text[0] != '/')
            {
                if (context.Client.GameState.ActiveChannel == null)
                    throw new GameProtocolViolationException(context.Client, "Cannot send message, user is not in a channel");

                if (context.Client.GameState.ActiveChannel.Count <= 1 || context.Client.GameState.ActiveChannel.ActiveFlags.HasFlag(Channel.Flags.Silent))
                    Channel.WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, context.Client.GameState.ActiveChannel.ActiveFlags, 0, context.Client.GameState.ActiveChannel.Name, "No one hears you."), context.Client);
                else
                    context.Client.GameState.ActiveChannel.WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_TALK, context.Client.GameState.ChannelFlags, context.Client.GameState.Ping, context.Client.GameState.OnlineName, text));

                return true;
            }

            if (text[0..2] == "/j" || text[0..5] == "/join" || text[0..8] == "/channel")
            {
                var channelName = "";

                if (text[0..2] == "/j") channelName = text[3..];
                if (text[0..5] == "/join") channelName = text[6..];
                if (text[0..8] == "/channel") channelName = text[9..];

                var channel = Channel.GetChannelByName(channelName);
                if (channel == null) channel = new Channel(channelName, Channel.Flags.None);

                channel.AcceptUser(context.Client.GameState);
                return true;
            }

            var command = ChatCommand.FromString(text[1..]);
            var commandContext = new ChatCommandContext(command, context.Client.GameState);

            if (!command.CanInvoke(commandContext))
            {
                Channel.WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.Client.GameState.ChannelFlags, context.Client.GameState.Ping, context.Client.GameState.OnlineName, "That command is not available at the moment."), context.Client);
                return true;
            }

            command.Invoke(commandContext);
            return true;
        }
    }
}
