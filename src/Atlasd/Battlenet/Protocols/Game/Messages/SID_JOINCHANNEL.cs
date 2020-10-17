using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_JOINCHANNEL : Message
    {
        public enum Flags : UInt32
        {
            NoCreate = 0,
            First = 1,
            Forced = 2,
            First_D2 = 5,
        };

        public SID_JOINCHANNEL()
        {
            Id = (byte)MessageIds.SID_JOINCHANNEL;
            Buffer = new byte[0];
        }

        public SID_JOINCHANNEL(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_JOINCHANNEL;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            if (context.Direction == MessageDirection.ServerToClient)
                throw new GameProtocolViolationException(context.Client, "Server isn't allowed to send SID_JOINCHANNEL");

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_JOINCHANNEL (" + (4 + Buffer.Length) + " bytes)");

            if (Buffer.Length < 5)
                throw new GameProtocolViolationException(context.Client, "SID_JOINCHANNEL buffer must be at least 5 bytes");

            /**
              * (UINT32) Flags
              * (STRING) Channel name
              */

            var m = new MemoryStream(Buffer);
            var r = new BinaryReader(m);

            var flags = (Flags)r.ReadUInt32();
            var channelName = r.ReadString();

            r.Close();
            m.Close();

            if (channelName.Length < 1) throw new GameProtocolViolationException(context.Client, "Channel name must be greater than zero");

            if (channelName.Length > 31) channelName = channelName.Substring(0, 31);

            foreach (var c in channelName)
            {
                if ((uint)c < 31) throw new GameProtocolViolationException(context.Client, "Channel name must not have ASCII control characters");
            }

            var firstJoin = flags == Flags.First || flags == Flags.First_D2;
            if (firstJoin) channelName = Product.ProductChannelName(context.Client.GameState.Product) + " " + context.Client.GameState.Locale.CountryNameAbbreviated + "-1";

            var userFlags = (Account.Flags)context.Client.GameState.ActiveAccount.Get(Account.FlagsKey);
            var ignoreLimits = userFlags.HasFlag(Account.Flags.Employee);

            var channel = Channel.GetChannelByName(channelName);

            if (channel == null && flags == Flags.NoCreate)
            {
                new ChatEvent(ChatEvent.EventIds.EID_CHANNELFULL, Channel.Flags.None, context.Client.GameState.Ping, context.Client.GameState.OnlineName, channelName).WriteTo(context.Client);
                return true;
            }

            if (channel == null) channel = new Channel(channelName, firstJoin ? Channel.Flags.Public | Channel.Flags.ProductSpecific : Channel.Flags.None);
            channel.AcceptUser(context.Client.GameState, ignoreLimits, true);

            if (firstJoin)
            {
                var gameState = context.Client.GameState;

                var account = gameState.ActiveAccount;
                var activeUserFlags = gameState.ChannelFlags;
                var activeUserPing = gameState.Ping;
                var lastLogon = gameState.LastLogon;
                var onlineName = gameState.OnlineName;

                Channel.WriteServerStats(context.Client);

                if (Product.IsChatRestricted(gameState.Product))
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, activeUserFlags, activeUserPing, onlineName, Resources.GameProductIsChatRestricted).WriteTo(context.Client);

                var lastLogonTimestamp = lastLogon.ToString("ddd MMM dd hh:mm tt"); // example: "Sat Oct 17  6:11 AM"
                new ChatEvent(ChatEvent.EventIds.EID_INFO, activeUserFlags, activeUserPing, onlineName, Resources.LastLogonInfo.Replace("{timestamp}", lastLogonTimestamp)).WriteTo(context.Client);

                var failedLogins = (UInt32)0;
                if (account.ContainsKey(Account.FailedLogonsKey)) failedLogins = (UInt32)account.Get(Account.FailedLogonsKey);
                account.Set(Account.FailedLogonsKey, (UInt32)0);

                if (failedLogins > 0)
                    new ChatEvent(ChatEvent.EventIds.EID_ERROR, activeUserFlags, activeUserPing, onlineName, Resources.FailedLogonAttempts.Replace("{count}", failedLogins.ToString("##,0"))).WriteTo(context.Client);

            }

            return true;
        }
    }
}
