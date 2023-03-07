using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    /**
     * [3/6/2023 5:39:55 PM] islanti: *resurrects author of miragechat to patch msgbox. carl starts disconnecting everybody with post-enterchat auth_check failure
     * [3/6/2023 5:41:48 PM] islanti: have no comb through my code to find all instances of voluntary disconnect and verify it is within-sequence  
     * [3/6/2023 5:41:58 PM] islanti: *to comb
     */
    class AdminBotFuckeryCommand : ChatCommand
    {
        public AdminBotFuckeryCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var t = Arguments.Count == 0 ? "" : Arguments[0]; // target
            string r; // reply

            if (t.Length == 0 || !Battlenet.Common.GetClientByOnlineName(t, out var target) || target == null)
            {
                r = Resources.UserNotLoggedOn;
                foreach (var line in r.Split(Environment.NewLine))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
                return;
            }

            Arguments.RemoveAt(0); // remove target
            // Calculates and removes (target+' ') from (raw) which prints into (newRaw):
            RawBuffer = RawBuffer[(Encoding.UTF8.GetByteCount(t) + (Arguments.Count > 0 ? 1 : 0))..];
            var type = string.Join(' ', Arguments);

            if (string.IsNullOrEmpty(type))
            {
                r = Resources.AdminBotFuckeryCommandInvalid;
                foreach (var line in r.Split(Environment.NewLine))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
                return;
            }

            var realmName = Settings.GetString(new string[] { "battlenet", "realm", "name" }, Resources.Battlenet);
            var targetEnv = new Dictionary<string, string>()
            {
                { "accountName", target.Username },
                { "channel", target.ActiveChannel == null ? "(null)" : target.ActiveChannel.Name },
                { "game", Product.ProductName(target.Product, true) },
                { "host", Settings.GetString(new string[] { "battlenet", "realm", "host" }, "(null)") },
                { "localTime", target.LocalTime.ToString(Common.HumanDateTimeFormat).Replace(" 0", "  ") },
                { "name", target.OnlineName },
                { "onlineName", target.OnlineName },
                { "realm", realmName },
                { "realmTime", DateTime.Now.ToString(Common.HumanDateTimeFormat).Replace(" 0", "  ") },
                { "realmTimezone", $"UTC{DateTime.Now:zzz}" },
                { "user", target.OnlineName },
                { "username", target.OnlineName },
                { "userName", target.OnlineName },
            };
            var env = targetEnv.Concat(context.Environment);

            SID_AUTH_CHECK.Statuses status = type.ToLowerInvariant().Replace(" ", string.Empty) switch
            {
                "bannedkey" => SID_AUTH_CHECK.Statuses.GameKeyBanned,
                "bannedkey2" => SID_AUTH_CHECK.Statuses.GameKeyBanned | SID_AUTH_CHECK.Statuses.GameKeyExpansion,
                "inusekey" => SID_AUTH_CHECK.Statuses.GameKeyInUse,
                "inusekey2" => SID_AUTH_CHECK.Statuses.GameKeyInUse | SID_AUTH_CHECK.Statuses.GameKeyExpansion,
                "invalidkey" => SID_AUTH_CHECK.Statuses.GameKeyInvalid,
                "invalidkey2" => SID_AUTH_CHECK.Statuses.GameKeyInvalid | SID_AUTH_CHECK.Statuses.GameKeyExpansion,
                "invalidversion" => SID_AUTH_CHECK.Statuses.InvalidVersion,
                "success" => SID_AUTH_CHECK.Statuses.Success,
                "toonew" => SID_AUTH_CHECK.Statuses.VersionTooNew,
                "tooold" => SID_AUTH_CHECK.Statuses.VersionTooOld,
                "wronggamekey" => SID_AUTH_CHECK.Statuses.GameKeyProductMismatch,
                "wronggamekey2" => SID_AUTH_CHECK.Statuses.GameKeyProductMismatch | SID_AUTH_CHECK.Statuses.GameKeyExpansion,
                _ => SID_AUTH_CHECK.Statuses.InvalidVersion,
            };

            new SID_AUTH_CHECK().Invoke(new MessageContext(target.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>(){
                { "status", status }, { "info", Array.Empty<byte>() }
            }));

            r = Resources.AdminBotFuckeryCommand;
            foreach (var kv in env) r = r.Replace("{" + kv.Key + "}", kv.Value);
            foreach (var line in r.Split(Battlenet.Common.NewLine))
                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }
    }
}
