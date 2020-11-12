using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class AdminReloadCommand : ChatCommand
    {
        public AdminReloadCommand(List<string> arguments) : base(arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            Exception e;
            string r;
            ChatEvent.EventIds eid;

            try
            {
                Settings.Load();
                e = null;
                r = Resources.AdminReloadCommandSuccess;
                eid = ChatEvent.EventIds.EID_INFO;
            }
            catch (Exception ex)
            {
                e = ex;
                r = Resources.AdminReloadCommandFailure.Replace("{exception}", e.GetType().Name);
                eid = ChatEvent.EventIds.EID_ERROR;
            }

            foreach (var kv in context.Environment)
            {
                r = r.Replace("{" + kv.Key + "}", kv.Value);
            }

            foreach (var line in r.Split(Battlenet.Common.NewLine))
                new ChatEvent(eid, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);

            if (e != null)
            {
                throw e;
            }
        }
    }
}
