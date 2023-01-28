using Atlasd.Localization;
using System;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class AdminChannelCommand : ChatCommand
    {
        public AdminChannelCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            var subcommand = Arguments.Count > 0 ? Arguments[0] : string.Empty;
            if (!string.IsNullOrEmpty(subcommand))
            {
                Arguments.RemoveAt(0);
            }

            var eventId = ChatEvent.EventIds.EID_ERROR;
            string reply = Resources.InvalidAdminCommand;
            var channel = context.GameState.ActiveChannel;

            if (channel != null)
            {
                switch (subcommand.ToLower())
                {
                    case "disband":
                        {
                            var destinationName = string.Join(" ", Arguments);
                            if (string.IsNullOrEmpty(destinationName)) destinationName = Resources.TheVoid;
                            var destination = Channel.GetChannelByName(destinationName, true);
                            channel.DisbandInto(destination);
                            eventId = ChatEvent.EventIds.EID_INFO;
                            reply = Resources.ChannelWasDisbanded.Replace("{oldName}", channel.Name).Replace("{newName}", destination.Name);
                            break;
                        }
                    case "flags":
                    case "flag":
                        {
                            if (Arguments.Count < 1)
                            {
                                eventId = ChatEvent.EventIds.EID_ERROR;
                                reply = Resources.InvalidAdminCommand;
                            }
                            else
                            {
                                int.TryParse(Arguments[0], out var flags);
                                reply = string.Empty;
                                channel.SetActiveFlags((Channel.Flags)flags);
                            }
                            break;
                        }
                    case "rename":
                    case "name":
                        {
                            var newName = string.Join(" ", Arguments);
                            if (!string.IsNullOrEmpty(newName))
                            {
                                reply = string.Empty;
                                lock (Battlenet.Common.ActiveChannels)
                                {
                                    Battlenet.Common.ActiveChannels.Remove(channel.Name);
                                    Battlenet.Common.ActiveChannels.Add(newName, channel);
                                }
                                channel.SetName(newName);
                            }
                            break;
                        }
                    case "maxusers":
                    case "maxuser":
                        {
                            if (Arguments.Count < 1)
                            {
                                eventId = ChatEvent.EventIds.EID_ERROR;
                                reply = Resources.InvalidAdminCommand;
                            }
                            else
                            {
                                int.TryParse(Arguments[0], out var maxUsers);
                                reply = string.Empty;
                                channel.SetMaxUsers(maxUsers);
                            }
                            break;
                        }
                    case "resync":
                    case "sync":
                        {
                            reply = string.Empty;
                            channel.Resync();
                            break;
                        }
                    case "topic":
                        {
                            reply = string.Empty;
                            channel.SetTopic(string.Join(" ", Arguments));
                            break;
                        }
                }
            }

            if (string.IsNullOrEmpty(reply)) return;

            foreach (var kv in context.Environment)
            {
                reply = reply.Replace("{" + kv.Key + "}", kv.Value);
            }

            foreach (var line in reply.Split(Battlenet.Common.NewLine))
                new ChatEvent(eventId, context.GameState.ChannelFlags, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
        }
    }
}
