using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_NOTIFYJOIN : Message
    {

        public SID_NOTIFYJOIN()
        {
            Id = (byte)MessageIds.SID_NOTIFYJOIN;
            Buffer = new byte[0];
        }

        public SID_NOTIFYJOIN(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_NOTIFYJOIN;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            if (context == null || context.Client == null || !context.Client.Connected) return false;
            var gameState = context.Client.GameState;

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server");

            if (gameState == null || gameState.ActiveAccount == null)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} received but client not logged into an account");

            if (Buffer.Length < 10)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 10 bytes");

            using var m = new MemoryStream(Buffer);
            using var r = new BinaryReader(m);

            var productId = r.ReadUInt32();
            var productVersion = r.ReadUInt32();
            var gameName = r.ReadByteString();
            var gamePassword = r.ReadByteString();

            if (gameState.ActiveChannel != null)
                gameState.ActiveChannel.RemoveUser(gameState);

            lock (Battlenet.Common.ActiveGameAds)
            {
                foreach (var gameAd in Battlenet.Common.ActiveGameAds)
                {
                    if (gameAd.Name.SequenceEqual(gameName))
                    {
                        if (gameAd.HasClient(gameState) || gameAd.AddClient(gameState))
                            gameState.GameAd = gameAd;
                        break;
                    }
                }
            }

            var mutualFriend = false;
            var friendByteStrings = (List<byte[]>)gameState.ActiveAccount.Get(Account.FriendsKey, new List<byte[]>());
            foreach (var friendByteString in friendByteStrings)
            {
                if (Battlenet.Common.GetClientByOnlineName(Encoding.UTF8.GetString(friendByteString), out var friendGameState) && friendGameState != null && friendGameState.ActiveAccount != null)
                {
                    mutualFriend = false;
                    var subfriendByteStrings = (List<byte[]>)friendGameState.ActiveAccount.Get(Account.FriendsKey, new List<byte[]>());
                    foreach (var subfriendByteString in subfriendByteStrings)
                    {
                        if (Battlenet.Common.GetClientByOnlineName(Encoding.UTF8.GetString(subfriendByteString), out var subfriendGameState) && subfriendGameState != null)
                        {
                            mutualFriend = (subfriendGameState == gameState);
                            if (mutualFriend) break;
                        }
                    }
                    if (mutualFriend)
                    {
                        var buffer = Resources.YourFriendEnteredGame;
                        buffer = buffer.Replace("{friend}", gameState.Username, true, CultureInfo.InvariantCulture);
                        buffer = buffer.Replace("{game}", Product.ProductName(gameState.Product, true), true, CultureInfo.InvariantCulture);
                        buffer = buffer.Replace("{gameAd}", Encoding.UTF8.GetString(gameState.GameAd.Name), true, CultureInfo.InvariantCulture);
                        foreach (var line in buffer.Split(Battlenet.Common.NewLine))
                            new ChatEvent(ChatEvent.EventIds.EID_WHISPERFROM, gameState.ChannelFlags, gameState.Ping, Channel.RenderOnlineName(friendGameState, gameState), buffer).WriteTo(friendGameState.Client);
                    }
                }
            }

            return true;
        }
    }
}
