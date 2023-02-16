using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class AdminClanListCommand : ChatCommand
    {
        public AdminClanListCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var clanList = Battlenet.Common.ActiveClans.ToArray();
            Array.Sort(clanList, (x, y) => string.Compare(Encoding.UTF8.GetString(x.Key), Encoding.UTF8.GetString(y.Key), StringComparison.OrdinalIgnoreCase));

            ChatEvent.EventIds replyEventId;
            string reply;

            if (clanList.Length == 0)
            {
                replyEventId = ChatEvent.EventIds.EID_ERROR;
                reply = Resources.AdminClanListCommandEmpty;
            }
            else
            {
                replyEventId = ChatEvent.EventIds.EID_INFO;
                reply = Resources.AdminClanListCommand;
                foreach (var pair in clanList)
                {
                    var clan = pair.Value;
                    var clanName = Encoding.UTF8.GetString(clan.Name).Replace((char)0x00, (char)0x20).Trim();
                    var clanTag = Encoding.UTF8.GetString(clan.Tag).Replace((char)0x00, (char)0x20).Trim();

                    reply += $"{Battlenet.Common.NewLine}[{clanTag}] {clanName}, {clan.Count} members";
                }
            }

            var gameState = context.GameState;
            foreach (var kv in context.Environment) reply = reply.Replace("{" + kv.Key + "}", kv.Value);
            foreach (var line in reply.Split(Battlenet.Common.NewLine))
                new ChatEvent(replyEventId, gameState.ChannelFlags, gameState.Client.RemoteIPAddress, gameState.Ping, gameState.OnlineName, line).WriteTo(gameState.Client);
        }
    }
}
