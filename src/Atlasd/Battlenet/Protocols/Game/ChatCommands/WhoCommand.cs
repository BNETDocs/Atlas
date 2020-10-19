using Atlasd.Localization;
using System.Collections.Generic;
using System.Linq;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class WhoCommand : ChatCommand
    {
        public WhoCommand(List<string> arguments) : base(arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var channelName = string.Join(" ", Arguments);
            var ch = channelName.Length > 0 ? Channel.GetChannelByName(channelName) : context.GameState.ActiveChannel;
            string r; // reply

            if (ch == null)
            {
                r = Resources.ChannelNotFound;
                foreach (var line in r.Split("\r\n"))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
                return;
            }

            if (ch.ActiveFlags.HasFlag(Channel.Flags.Restricted))
            {
                r = Resources.ChannelIsRestricted;
                foreach (var line in r.Split("\r\n"))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, ch.ActiveFlags, 0, ch.Name, line).WriteTo(context.GameState.Client);
                return;
            }

            r = Resources.WhoCommand;

            r = r.Replace("{channel}", ch == null ? "(null)" : ch.Name);
            r = r.Replace("{users}", ch.GetUsersAsString());

            foreach (var line in r.Split("\r\n"))
                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }
    }
}
