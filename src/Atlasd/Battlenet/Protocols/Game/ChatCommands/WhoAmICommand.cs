using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class WhoAmICommand : ChatCommand
    {
        public WhoAmICommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var gameState = context.GameState;
            var ch = gameState.ActiveChannel;
            var g = gameState.GameAd;
            var r = ch == null ? Resources.YouAreUsingGameInRealm : Resources.YouAreUsingGameInTheChannel;

            if (g != null) // TODO: Consider private games and friendship.
                r = Resources.UserIsUsingGameInTheGame;
            else if (ch != null)
                r = Resources.UserIsUsingGameInTheChannel;
            else
                r = Resources.UserIsUsingGameInRealm;

            if (gameState.Away != null)
                r += Battlenet.Common.NewLine + Resources.AwayCommandStatusSelf.Replace("{awayMessage}", gameState.Away);

            var env = new Dictionary<string, string>()
            {
                { "accountName", gameState.Username },
                { "channel", ch == null ? "(null)" : ch.Name },
                { "game", Product.ProductName(gameState.Product, true) },
                { "gameAd", g == null ? "(null)" : Encoding.UTF8.GetString(g.Name) },
                { "host", Settings.GetString(new string[] { "battlenet", "realm", "host" }, "(null)") },
                { "localTime", gameState.LocalTime.ToString(Common.HumanDateTimeFormat).Replace(" 0", "  ") },
                { "name", Channel.RenderOnlineName(gameState, gameState) },
                { "onlineName", Channel.RenderOnlineName(gameState, gameState) },
                { "realm", Settings.GetString(new string[] { "battlenet", "realm", "name" }, Resources.Battlenet) },
                { "realmTime", DateTime.Now.ToString(Common.HumanDateTimeFormat).Replace(" 0", "  ") },
                { "realmTimezone", $"UTC{DateTime.Now:zzz}" },
                { "user", Channel.RenderOnlineName(gameState, gameState) },
                { "username", Channel.RenderOnlineName(gameState, gameState) },
                { "userName", Channel.RenderOnlineName(gameState, gameState) },
            };
            var en2v = env.Concat(context.Environment);

            foreach (var kv in env) r = r.Replace("{" + kv.Key + "}", kv.Value);
            new ChatEvent(ChatEvent.EventIds.EID_INFO, gameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, gameState.Ping, gameState.OnlineName, r).WriteTo(gameState.Client);
        }
    }
}
