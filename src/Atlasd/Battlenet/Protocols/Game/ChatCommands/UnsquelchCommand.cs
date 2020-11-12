using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Net;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class UnsquelchCommand : ChatCommand
    {
        public UnsquelchCommand(List<string> arguments) : base(arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            string r; // reply
            string t; // target

            t = Arguments.Count == 0 ? "" : Arguments[0];

            if (!Battlenet.Common.GetClientByOnlineName(t, out var target) || target == null)
            {
                r = Resources.UserNotLoggedOn;
                foreach (var line in r.Split(Resources.NewLine))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
                return;
            }

            var ipAddress = IPAddress.Parse(target.Client.RemoteEndPoint.ToString().Split(":")[0]);

            lock (context.GameState.SquelchedIPs)
            {
                if (context.GameState.SquelchedIPs.Contains(ipAddress))
                {
                    context.GameState.SquelchedIPs.Remove(ipAddress);
                }
            }

            lock (context.GameState.ActiveChannel)
            {
                if (context.GameState.ActiveChannel != null) context.GameState.ActiveChannel.SquelchUpdate(context.GameState);
            }
        }
    }
}
