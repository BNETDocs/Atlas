using Atlasd.Localization;
using System;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class WhoCommand : ChatCommand
    {
        public WhoCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var channelName = string.Join(" ", Arguments);
            var ch = channelName.Length > 0 ? Channel.GetChannelByName(channelName, false) : context.GameState.ActiveChannel;
            string r; // reply

            if (ch == null)
            {
                r = Resources.ChannelNotFound;
                foreach (var line in r.Split(Battlenet.Common.NewLine))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
                return;
            }

            if (ch.ActiveFlags.HasFlag(Channel.Flags.Restricted))
            {
                r = Resources.ChannelIsRestricted;
                foreach (var line in r.Split(Battlenet.Common.NewLine))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, ch.ActiveFlags, 0, ch.Name, line).WriteTo(context.GameState.Client);
                return;
            }

            r = Resources.WhoCommand;

            r = r.Replace("{channel}", ch == null ? "(null)" : ch.Name);
            r = r.Replace("{users}", ch.GetUsersAsString(context.GameState));

            foreach (var kv in context.Environment)
            {
                r = r.Replace("{" + kv.Key + "}", kv.Value);
            }

            foreach (var line in r.Split(Battlenet.Common.NewLine))
            {
                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
            }
        }
    }
}
