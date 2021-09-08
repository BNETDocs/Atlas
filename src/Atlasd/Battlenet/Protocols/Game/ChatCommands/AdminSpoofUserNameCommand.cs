using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class AdminSpoofUserNameCommand : ChatCommand
    {
        public AdminSpoofUserNameCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var r = string.Empty; // reply
            var n1 = Arguments.Count < 1 ? string.Empty : Arguments[0]; // target old name
            var n2 = Arguments.Count < 2 ? string.Empty : Arguments[1]; // target new name

            if (n1.Length == 0 || !Battlenet.Common.GetClientByOnlineName(n1, out var target) || target == null)
            {
                r = Resources.UserNotLoggedOn;
                foreach (var kv in context.Environment)
                    r = r.Replace("{" + kv.Key + "}", kv.Value);
                foreach (var line in r.Split(Battlenet.Common.NewLine))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
                return;
            }

            if (n2.Length == 0)
            {
                r = Resources.AdminSpoofUserNameCommandBadValue;
                foreach (var kv in context.Environment)
                    r = r.Replace("{" + kv.Key + "}", kv.Value);
                foreach (var line in r.Split(Battlenet.Common.NewLine))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
                return;
            }

            Arguments.RemoveAt(1); // remove n2
            Arguments.RemoveAt(0); // remove n1
            // Calculates and removes (n1+' '+n2+' ') from (raw) which prints into (newRaw):
            RawBuffer = RawBuffer[(
                Encoding.UTF8.GetByteCount(n1) +
                Encoding.UTF8.GetByteCount(n2) +
                (Arguments.Count > 0 ? 1 : 0) +
                (Arguments.Count > 1 ? 1 : 0))..];

            context.Environment.Add("name1", n1);
            context.Environment.Add("name2", n2);

            // process n1 → n2

            var oldOnlineName = target.OnlineName;
            var activeChannel = target.ActiveChannel;

            // remove user from their channel
            if (activeChannel != null)
            {
                activeChannel.RemoveUser(target);
            }

            // get a new unique name from n2 (instead of target.Username)
            lock (Battlenet.Common.ActiveAccounts)
            {
                var searchName = n2.Contains("#") ? n2[0..n2.IndexOf("#")] : n2;
                int serial = 1;

                var onlineName = searchName;
                while (Battlenet.Common.ActiveAccounts.ContainsKey(onlineName))
                {
                    onlineName = $"{searchName}#{++serial}";
                }

                lock (target.ActiveAccount)
                {
                    Battlenet.Common.ActiveAccounts.Remove(oldOnlineName);
                    target.OnlineName = onlineName;
                    Battlenet.Common.ActiveAccounts.Add(onlineName, target.ActiveAccount);
                }
            }

            // reset active states
            lock (Battlenet.Common.ActiveGameStates)
            {
                Battlenet.Common.ActiveGameStates.Remove(oldOnlineName);
                Battlenet.Common.ActiveGameStates.Add(target.OnlineName, target);
            }

            // send a new SID_ENTERCHAT to target
            new SID_ENTERCHAT().Invoke(new MessageContext(target.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {}));

            // put them back in their channel
            activeChannel.AcceptUser(target, true);

            // print result to admin
            foreach (var kv in context.Environment)
                r = r.Replace("{" + kv.Key + "}", kv.Value);
            foreach (var line in r.Split(Battlenet.Common.NewLine))
                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }
    }
}
