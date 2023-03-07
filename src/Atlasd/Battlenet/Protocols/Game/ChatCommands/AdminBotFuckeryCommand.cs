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
            var t = Arguments.Count == 0 ? string.Empty : Arguments[0]; // target
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

            var random = new Random();
            int choice1 = (Arguments.Count > 0 ? int.Parse(Arguments[0]) : random.Next(0, 4));

            switch (choice1)
            {
                case 0: // What if the server sent client out-of-sequence spoofed SID_AUTH_CHECK result?
                {
                    int choice2 = (Arguments.Count > 1 ? int.Parse(Arguments[1]) : random.Next(0, 12));
                    context.Environment["message"] = "SID_AUTH_CHECK";
                    SID_AUTH_CHECK.Statuses status = choice2 switch
                    {
                         0 => SID_AUTH_CHECK.Statuses.Success,
                         1 => SID_AUTH_CHECK.Statuses.GameKeyBanned,
                         2 => SID_AUTH_CHECK.Statuses.GameKeyBanned | SID_AUTH_CHECK.Statuses.GameKeyExpansion,
                         3 => SID_AUTH_CHECK.Statuses.GameKeyInUse,
                         4 => SID_AUTH_CHECK.Statuses.GameKeyInUse | SID_AUTH_CHECK.Statuses.GameKeyExpansion,
                         5 => SID_AUTH_CHECK.Statuses.GameKeyInvalid,
                         6 => SID_AUTH_CHECK.Statuses.GameKeyInvalid | SID_AUTH_CHECK.Statuses.GameKeyExpansion,
                         7 => SID_AUTH_CHECK.Statuses.GameKeyProductMismatch,
                         8 => SID_AUTH_CHECK.Statuses.GameKeyProductMismatch | SID_AUTH_CHECK.Statuses.GameKeyExpansion,
                         9 => SID_AUTH_CHECK.Statuses.InvalidVersion,
                        10 => SID_AUTH_CHECK.Statuses.VersionTooNew,
                        11 => SID_AUTH_CHECK.Statuses.VersionTooOld,
                         _ => SID_AUTH_CHECK.Statuses.InvalidVersion,
                    };
                    new SID_AUTH_CHECK().Invoke(new MessageContext(target.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>(){
                        { "status", status }, { "info", Array.Empty<byte>() }
                    }));
                    break;
                }
                case 1: // What if server sent client unsolicited SID_QUERYREALMS2 with fake realm names?
                {
                    context.Environment["message"] = "SID_QUERYREALMS2";
                    var rand20HumanBytes = new byte[20];
                    for (int i = 0; i < rand20HumanBytes.Length; i++) rand20HumanBytes[i] = (byte)random.Next((byte)'A', 1 + (byte)'Z');
                    new SID_QUERYREALMS2().Invoke(new MessageContext(target.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>(){
                        { "realms", new Dictionary<byte[], byte[]>(){{ Encoding.UTF8.GetBytes("Botfuckery"), rand20HumanBytes }} }
                    }));
                    break;
                }
                case 2: // What if server sent client out-of-sequence spoofed SID_LOGONRESPONSE2 result?
                {
                    int choice2 = (Arguments.Count > 1 ? int.Parse(Arguments[1]) : random.Next(0, 4));
                    context.Environment["message"] = "SID_LOGONRESPONSE2";
                    SID_LOGONRESPONSE2.Statuses status = choice2 switch
                    {
                         0 => SID_LOGONRESPONSE2.Statuses.Success,
                         1 => SID_LOGONRESPONSE2.Statuses.BadPassword,
                         2 => SID_LOGONRESPONSE2.Statuses.AccountNotFound,
                         3 => SID_LOGONRESPONSE2.Statuses.AccountClosed,
                         _ => SID_LOGONRESPONSE2.Statuses.AccountNotFound,
                    };
                    var rand20HumanBytes = new byte[20];
                    for (int i = 0; i < rand20HumanBytes.Length; i++) rand20HumanBytes[i] = (byte)random.Next((byte)'A', 1 + (byte)'Z');
                    byte[] info = status == SID_LOGONRESPONSE2.Statuses.AccountClosed ? rand20HumanBytes : Array.Empty<byte>();
                    new SID_LOGONRESPONSE2().Invoke(new MessageContext(target.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>(){
                        { "status", status }, { "info", info }
                    }));
                    break;
                }
                case 3: // What if server asked for some registry info?
                {
                    context.Environment["message"] = "SID_REGISTRY";
                    new SID_REGISTRY().Invoke(new MessageContext(target.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>(){
                        { "cookie", (UInt32)random.Next(-0x80000000, 0x7FFFFFFF) },
                        { "hiveKeyId", (UInt32)0x80000001 }, // HKEY_CURRENT_USER
                        { "keyPath", (string)"Software\\Battle.net\\Configuration" },
                        { "keyName", (string)"Battle.net gateways" },
                    }));
                    break;
                }
            }

            r = Resources.AdminBotFuckeryCommand;
            foreach (var kv in targetEnv.Concat(context.Environment)) r = r.Replace("{" + kv.Key + "}", kv.Value);
            foreach (var line in r.Split(Battlenet.Common.NewLine))
                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }
    }
}
 