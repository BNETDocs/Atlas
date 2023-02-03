using Atlasd.Localization;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class EmoteCommand : ChatCommand
    {
        public EmoteCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            if (context.GameState.ActiveChannel == null)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, Resources.InvalidChatCommand).WriteTo(context.GameState.Client);
                return;
            }

            context.GameState.ActiveChannel.WriteChatMessage(context.GameState, RawBuffer, true);

            if (context.GameState.ActiveChannel.Count <= 1 || context.GameState.ActiveChannel.ActiveFlags.HasFlag(Channel.Flags.Silent))
            {
                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ActiveChannel.ActiveFlags, 0, context.GameState.ActiveChannel.Name, Resources.NoOneHearsYou).WriteTo(context.GameState.Client);
            }
        }
    }
}
