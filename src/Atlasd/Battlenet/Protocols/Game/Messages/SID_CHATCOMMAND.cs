using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;

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
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} may only be transmitted from client to server");

            if (context.Client.GameState == null)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} requires a GameState object");

            if (Buffer.Length < 2)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 2 bytes");

            if (Buffer.Length > 224)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at most 224 bytes");

            var raw = Buffer[0..^1]; // remove null-terminator before processing

            foreach (var c in raw)
            {
                if (c < 32)
                {
                    // this message contains a control character and should be dropped per protocol
                    return true;
                }
            }

            // Treat this as a chat message if not starting with slash:
            if (raw[0] != '/')
            {
                if (context.Client.GameState.ActiveChannel == null)
                    throw new GameProtocolViolationException(context.Client, "Cannot send message, user is not in a channel");

                if (context.Client.GameState.ActiveChannel.Count <= 1 || context.Client.GameState.ActiveChannel.ActiveFlags.HasFlag(Channel.Flags.Silent))
                {
                    new ChatEvent(ChatEvent.EventIds.EID_INFO, context.Client.GameState.ActiveChannel.ActiveFlags, 0, context.Client.GameState.ActiveChannel.Name, Resources.NoOneHearsYou).WriteTo(context.Client);
                }
                else
                {
                    context.Client.GameState.ActiveChannel.WriteChatMessage(context.Client.GameState, raw, false);
                }

                return true;
            }

            var onlineName = context.Client.GameState.OnlineName;
            var command = ChatCommand.FromByteArray(raw[1..]); // remove slash before calling FromByteArray()
            var commandEnvironment = new Dictionary<string, string>()
            {
                { "accountName", context.Client.GameState.Username },
                { "channel", context.Client.GameState.ActiveChannel == null ? "(null)" : context.Client.GameState.ActiveChannel.Name },
                { "game", Product.ProductName(context.Client.GameState.Product, true) },
                { "host", "BNETDocs" },
                { "localTime", context.Client.GameState.LocalTime.ToString(Common.HumanDateTimeFormat) },
                { "name", onlineName },
                { "onlineName", onlineName },
                { "realm", "Battle.net" },
                { "realmTime", DateTime.Now.ToString(Common.HumanDateTimeFormat) },
                { "realmTimezone", $"UTC{DateTime.Now:zzz}" },
                { "user", onlineName },
                { "username", onlineName },
                { "userName", onlineName },
            };
            var commandContext = new ChatCommandContext(command, commandEnvironment, context.Client.GameState);

            if (!command.CanInvoke(commandContext))
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.Client.GameState.ChannelFlags, context.Client.GameState.Ping, context.Client.GameState.OnlineName, Resources.ChatCommandUnavailable).WriteTo(context.Client);
                return true;
            }

            command.Invoke(commandContext);
            return true;
        }
    }
}
