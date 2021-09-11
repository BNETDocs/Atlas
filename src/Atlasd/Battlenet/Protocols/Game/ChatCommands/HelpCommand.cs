using Atlasd.Daemon;
using Atlasd.Localization;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class HelpCommand : ChatCommand
    {
        public HelpCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var hasAdmin = ChatCommand.HasAdmin(context.GameState);
            var topic = Arguments.Count > 0 ? Arguments[0] : string.Empty;
            if (!string.IsNullOrEmpty(topic)) Arguments.RemoveAt(0);
            var remarks = hasAdmin ? Resources.HelpCommandRemarksWithAdmin : Resources.HelpCommandRemarks;

            switch (topic.ToLower())
            {
                case "admin":
                    {
                        if (hasAdmin)
                        {
                            new AdminHelpCommand(RawBuffer, Arguments).Invoke(context); return;
                        }
                        break;
                    }
                case "advanced":
                    remarks = Resources.HelpCommandAdvancedRemarks; break;
                case "aliases":
                    remarks = Resources.HelpCommandAliasesRemarks; break;
                case "ban":
                    remarks = Resources.HelpCommandBanRemarks; break;
                case "channel":
                case "join":
                case "j":
                    remarks = Resources.HelpCommandJoinRemarks; break;
                case "commands":
                    remarks = Resources.HelpCommandCommandsRemarks; break;
                case "time":
                    remarks = Resources.HelpCommandTimeRemarks; break;
            }

            foreach (var kv in context.Environment)
            {
                remarks = remarks.Replace("{" + kv.Key + "}", kv.Value);
            }

            foreach (var line in remarks.Split(Battlenet.Common.NewLine))
            {
                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
            }
        }
    }
}
