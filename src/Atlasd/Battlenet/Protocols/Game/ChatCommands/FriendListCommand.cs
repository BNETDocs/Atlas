using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.ChatCommands;

class FriendListCommand : ChatCommand
{
    public FriendListCommand(byte[] rawBuffer, List<string> arguments) : base(rawBuffer, arguments) { }

    public override bool CanInvoke(ChatCommandContext context)
    {
        return context != null && context.GameState != null && context.GameState.ActiveAccount != null;
    }

    public override void Invoke(ChatCommandContext context)
    {
        var replyEventId = ChatEvent.EventIds.EID_INFO;
        var reply = Resources.YourFriendsList;
        var friends = (List<byte[]>)context.GameState.ActiveAccount.Get(Account.FriendsKey, new List<byte[]>());

        var friendCount = 0;
        foreach (var friend in friends)
        {
            if (friendCount++ == 0) reply += Battlenet.Common.NewLine;
            var friendString = Encoding.UTF8.GetString(friend);

            var detailString = "offline";
            if (Battlenet.Common.GetClientByOnlineName(friendString, out var friendGameState) && friendGameState != null)
            {
                detailString = $"using {Battlenet.Product.ProductName(friendGameState.Product, true)}";

                if (friendGameState.ActiveChannel == null)
                    detailString += " in Battle.net"; // emulation note: Blizzard servers use a server-specific realm name here.

                if (friendGameState.ActiveChannel != null && friendGameState.ActiveChannel.IsPublic())
                    detailString += $" in the channel {friendGameState.ActiveChannel.Name}.";

                if (friendGameState.ActiveChannel != null && !friendGameState.ActiveChannel.IsPublic())
                    detailString += $" in a private channel.";
            }
            if (!string.IsNullOrEmpty(detailString)) detailString = $", {detailString}";

            reply += $"{friendCount}: {friendString}{detailString}{Battlenet.Common.NewLine}";
        }
        if (friendCount > 0) reply = reply[0..(reply.Length - Battlenet.Common.NewLine.Length)]; // strip last newline

        if (string.IsNullOrEmpty(reply)) return;
        foreach (var kv in context.Environment) reply = reply.Replace("{" + kv.Key + "}", kv.Value);
        foreach (var line in reply.Split(Battlenet.Common.NewLine))
            new ChatEvent(replyEventId, context.GameState.ChannelFlags, context.GameState.Client.RemoteIPAddress, context.GameState.Ping, context.GameState.OnlineName, line).WriteTo(context.GameState.Client);
    }
}
