using Atlasd.Battlenet.Protocols.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet
{
    class Friend
    {
        public enum Location : byte
        {
            Offline = 0x00,
            NotInChat = 0x01,
            InChat = 0x02,
            InPublicGame = 0x03,
            InPrivateGame = 0x04,
            InPasswordGame = 0x05,
        };

        [Flags]
        public enum Status : byte
        {
            None = 0x00,
            Mutual = 0x01,
            DoNotDisturb = 0x02,
            Away = 0x04,
        };

        public Location LocationId { get; private set; }
        public byte[] LocationString { get; private set; }
        public Status StatusId { get; private set; }
        public Product.ProductCode ProductCode { get; private set; }
        public byte[] Username { get; private set; }

        /**
         * <param name="source">The context GameState to sync from.</param>
         * <param name="username">The online name of the user to befriend.</param>
         */
        public Friend(GameState source, byte[] username)
        {
            Username = username;
            Sync(source);
        }

        /**
         * <remarks>Syncs the object properties with the context of the source GameState.</remarks>
         * <param name="source">The context GameState to sync from.</param>
         */
        public void Sync(GameState source)
        {
            LocationId = Location.Offline;
            LocationString = new byte[0];
            ProductCode = Product.ProductCode.None;
            StatusId = Status.None;

            if (source == null || source.ActiveAccount == null ||
                !Common.GetClientByOnlineName(Encoding.UTF8.GetString(Username), out var target) ||
                target == null || target.ActiveAccount == null)
            {
                return;
            }

            lock (source)
            {
                lock (target)
                {
                    var admin = source.HasAdmin();
                    var mutual = false;
                    var sourceFriendStrings = (List<byte[]>)source.ActiveAccount.Get(Account.FriendsKey, new List<byte[]>());
                    var targetFriendStrings = (List<byte[]>)target.ActiveAccount.Get(Account.FriendsKey, new List<byte[]>());

                    foreach (var targetFriendString in targetFriendStrings)
                    {
                        foreach (var sourceFriendString in sourceFriendStrings)
                        {
                            string aString = Encoding.UTF8.GetString(sourceFriendString);
                            string bString = Encoding.UTF8.GetString(targetFriendString);
                            if (string.Equals(aString, bString, StringComparison.CurrentCultureIgnoreCase))
                            {
                                mutual = true;
                                break;
                            }
                        }
                        if (mutual) break;
                    }

                    if (mutual) StatusId |= Status.Mutual;
                    if (!string.IsNullOrEmpty(target.Away)) StatusId |= Status.Away;
                    if (!string.IsNullOrEmpty(target.DoNotDisturb)) StatusId |= Status.DoNotDisturb;

                    if (target.ActiveChannel == null && target.GameAd == null)
                    {
                        LocationId = Location.NotInChat;
                    }
                    else if (target.ActiveChannel != null)
                    {
                        LocationId = Location.InChat;
                        if (mutual || admin) LocationString = Encoding.UTF8.GetBytes(target.ActiveChannel.Name);
                    }
                    else if (target.GameAd != null)
                    {
                        if (!target.GameAd.ActiveStateFlags.HasFlag(GameAd.StateFlags.Private) && target.GameAd.Password.Length == 0)
                        {
                            LocationId = Location.InPublicGame;
                            LocationString = target.GameAd.Name;
                        }
                        else if (!(mutual || admin))
                        {
                            LocationId = Location.InPrivateGame;
                        }
                        else
                        {
                            LocationId = Location.InPasswordGame;
                            LocationString = target.GameAd.Name;
                        }
                    }
                }
            }
        }
    }
}
