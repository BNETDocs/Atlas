using Atlasd.Daemon;
using Atlasd.Localization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class AdminBroadcastCommand : ChatCommand
    {
        public AdminBroadcastCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            Task.Run(() =>
            {
                var maskBroadcaster = Settings.GetBoolean(new string[] { "battlenet", "emulation", "mask_admins_in_broadcasts" }, false);

                var broadcasterFlags = maskBroadcaster ? Account.Flags.Admin : context.GameState.ChannelFlags;
                var broadcasterPing = maskBroadcaster ? -1 : context.GameState.Ping;
                var broadcasterName = maskBroadcaster ? Settings.GetString(new string[] { "battlenet", "realm", "name" }, Resources.Battlenet) : context.GameState.OnlineName;

                var chatEvent = new ChatEvent(ChatEvent.EventIds.EID_BROADCAST, broadcasterFlags, broadcasterPing, broadcasterName, RawBuffer);
                foreach (var gameState in Battlenet.Common.ActiveGameStates.Values) chatEvent.WriteTo(gameState.Client);
            });
        }
    }
}
