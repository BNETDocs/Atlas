﻿using Atlasd.Localization;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands
{
    class DesignateCommand : ChatCommand
    {
        public DesignateCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

        public override bool CanInvoke(ChatCommandContext context)
        {
            return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
        }

        public override void Invoke(ChatCommandContext context)
        {
            if (context.GameState.ActiveChannel == null)
            {
                new InvalidCommand(RawBuffer, Arguments).Invoke(context);
                return;
            }

            if (!(context.GameState.ChannelFlags.HasFlag(Account.Flags.Employee)
                || context.GameState.ChannelFlags.HasFlag(Account.Flags.ChannelOp)
                || context.GameState.ChannelFlags.HasFlag(Account.Flags.Admin)))
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, Resources.YouAreNotAChannelOperator).WriteTo(context.GameState.Client);
                return;
            }

            var target = "";
            if (Arguments.Count > 0)
            {
                target = Arguments[0];
                Arguments.RemoveAt(0);
                RawBuffer = RawBuffer[(Encoding.UTF8.GetByteCount(target) + (Arguments.Count > 0 ? 1 : 0))..];
            }

            if (string.IsNullOrEmpty(target)
                || !Battlenet.Common.ActiveGameStates.TryGetValue(target, out var targetState)
                || targetState == null)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, Resources.UserNotLoggedOn).WriteTo(context.GameState.Client);
                return;
            }

            if (targetState.ActiveChannel != context.GameState.ActiveChannel)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, Resources.InvalidUser).WriteTo(context.GameState.Client);
                return;
            }

            context.GameState.ActiveChannel.Designate(context.GameState, targetState);
            new ChatEvent(ChatEvent.EventIds.EID_INFO, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, Resources.DesignateCommand.Replace("{user}", targetState.OnlineName)).WriteTo(context.GameState.Client);
        }
    }
}
