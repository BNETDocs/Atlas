using Atlasd.Localization;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class WhisperCommand : ChatCommand
    {
        public WhisperCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            if (Arguments.Count < 1)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, Resources.UserNotLoggedOn).WriteTo(context.GameState.Client);
                return;
            }

            var target = Arguments[0];
            Arguments.RemoveAt(0);
            // Calculates and removes (target+' ') from (raw) which prints into (newRaw):
            RawBuffer = RawBuffer[(Encoding.UTF8.GetByteCount(target) + (Arguments.Count > 0 ? 1 : 0))..];

            // Check for empty message
            if (RawBuffer.Length == 0)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, Resources.WhisperCommandEmptyMessage).WriteTo(context.GameState.Client);
                return;
            }

            // Get the target state, or return not logged on
            if (!Battlenet.Common.GetClientByOnlineName(target, out var targetState) || targetState == null)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, Resources.UserNotLoggedOn).WriteTo(context.GameState.Client);
                return;
            }

            // Check if target is asking to not be disturbed
            if (!string.IsNullOrEmpty(targetState.DoNotDisturb))
            {
                var r = Resources.WhisperCommandUserIsDoNotDisturb;

                r = r.Replace("{user}", targetState.OnlineName);
                r = r.Replace("{message}", targetState.Away);

                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, r).WriteTo(context.GameState.Client);

                return; // Target has asked to not be disturbed, discontinue
            }

            // Notify the source we whispered the target
            new ChatEvent(ChatEvent.EventIds.EID_WHISPERTO, Channel.RenderChannelFlags(context.GameState, targetState), targetState.Ping, Channel.RenderOnlineName(context.GameState, targetState), RawBuffer).WriteTo(context.GameState.Client);

            // Check if target is marked away
            if (!string.IsNullOrEmpty(targetState.Away))
            {
                var r = Resources.WhisperCommandUserIsAway;

                r = r.Replace("{user}", targetState.OnlineName);
                r = r.Replace("{message}", targetState.Away);

                new ChatEvent(ChatEvent.EventIds.EID_INFO, Channel.RenderChannelFlags(context.GameState, targetState), context.GameState.Ping, Channel.RenderOnlineName(context.GameState, targetState), r).WriteTo(context.GameState.Client);
            }

            // Notify the target they were whispered by the source
            new ChatEvent(ChatEvent.EventIds.EID_WHISPERFROM, Channel.RenderChannelFlags(targetState, context.GameState), context.GameState.Ping, Channel.RenderOnlineName(targetState, context.GameState), RawBuffer).WriteTo(targetState.Client);
        }
    }
}
