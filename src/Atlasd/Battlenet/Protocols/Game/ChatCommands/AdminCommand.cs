﻿using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class AdminCommand : ChatCommand
    {
        public AdminCommand(List<string> arguments) : base(arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var hasSudo = false;

            lock (context.GameState)
            {
                hasSudo = context.GameState.ChannelFlags.HasFlag(Account.Flags.Admin)
                    || context.GameState.ChannelFlags.HasFlag(Account.Flags.Employee);
            }

            if (!hasSudo)
            {
                new InvalidCommand(Arguments).Invoke(context);
                return;
            }

            string cmd;

            if (Arguments.Count == 0)
            {
                cmd = "";
            } else
            {
                cmd = Arguments[0];
                Arguments.RemoveAt(0);
            }

            string r;

            switch (cmd.ToLower())
            {
                case "disconnect":
                    r = "/admin disconnect <user>";
                    break;
                case "help":
                case "?":
                    r = string.Join("\r\n", new List<string>() {
                        { "/admin disconnect" },
                        { "/admin help" },
                        { "/admin spoofuserflag" },
                        { "/admin spoofuserflags" },
                        { "/admin spoofusergame" },
                        { "" },
                    });
                    break;
                case "moveuser":
                case "move":
                    new AdminMoveUserCommand(Arguments).Invoke(context); return;
                case "spoofuserflag":
                case "spoofuserflags":
                    r = "/admin spoofuserflags <user> <flags>";
                    break;
                case "spoofusergame":
                    r = "/admin spoofusergame <user> <game>\r\n"
                      + "(This will preserve their statstring!)";
                    break;
                case "spoofusername":
                    r = "/admin spoofusername <oldname> <newname>";
                    break;
                case "spoofuserping":
                    r = "/admin spoofuserping <user> <ping>";
                    break;
                default:
                    r = "That is not a valid admin command. Type /admin help or /admin ? for more info.";
                    break;
            }

            foreach (var kv in context.Environment)
            {
                r = r.Replace("{" + kv.Key + "}", kv.Value);
            }

            foreach (var line in r.Split("\r\n"))
                new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }
    }
}
