using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using Atlasd.Localization;
using System;
using System.IO;
using System.Text;

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
                throw new GameProtocolViolationException(context.Client, $"Server isn't allowed to send {MessageName(Id)}");

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (Buffer.Length < 5)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 5 bytes");

            /**
              * (UINT32) Flags
              * (STRING) Channel name
              */

            using var m = new MemoryStream(Buffer);
            using var r = new BinaryReader(m);

            var flags = (Flags)r.ReadUInt32();
            var channelName = r.ReadByteString();

            if (channelName.Length > 31) channelName = channelName[0..31];

            foreach (byte c in channelName)
            {
                if (c < 31) throw new GameProtocolViolationException(context.Client, "Channel name must not have ASCII control characters");
            }

            var userCountryAbbr = string.Empty;
            var userFlags = Account.Flags.None;
            var userPing = (int)-1;
            var userName = (string)null;
            var userGame = Product.ProductCode.None;

            try
            {
                lock (context.Client.GameState)
                {
                    userCountryAbbr = " " + context.Client.GameState.Locale.CountryNameAbbreviated;
                    userFlags = (Account.Flags)context.Client.GameState.ActiveAccount.Get(Account.FlagsKey);
                    userPing = context.Client.GameState.Ping;
                    userName = context.Client.GameState.OnlineName;
                    userGame = context.Client.GameState.Product;
                }
            }
            catch (Exception ex)
            {
                if (!(ex is ArgumentNullException || ex is NullReferenceException)) throw;
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"{ex.GetType().Name} error occurred while processing {MessageName(Id)} for GameState object");
                return false;
            }

            var channelNameStr = Encoding.ASCII.GetString(channelName);

            var firstJoin = flags == Flags.First || flags == Flags.First_D2;
            if (firstJoin || channelName.Length == 0)
            {
                channelNameStr = $"{Product.ProductChannelName(userGame)}{userCountryAbbr}-1";
                channelName = Encoding.ASCII.GetBytes(channelNameStr);
            }

            var ignoreLimits = userFlags.HasFlag(Account.Flags.Employee) || userFlags.HasFlag(Account.Flags.Admin);
            var channel = Channel.GetChannelByName(channelNameStr, false);

            if (channel == null && flags == Flags.NoCreate)
            {
                new ChatEvent(ChatEvent.EventIds.EID_CHANNELNOTFOUND, Channel.Flags.None, userPing, userName, channelName).WriteTo(context.Client);
                return true;
            }

            if (channel == null) channel = Channel.GetChannelByName(channelNameStr, true);
            channel.AcceptUser(context.Client.GameState, ignoreLimits, true);

            if (!firstJoin) return true; // Return here if not first-join, the rest is setup for the server motd, etc.

            Account account;
            Account.Flags activeUserFlags;
            int activeUserPing;
            DateTime lastLogon;
            string onlineName;

            try
            {
                var gameState = context.Client.GameState;

                lock (gameState)
                {
                    account = gameState.ActiveAccount;
                    activeUserFlags = gameState.ChannelFlags;
                    activeUserPing = gameState.Ping;
                    lastLogon = gameState.LastLogon;
                    onlineName = gameState.OnlineName;
                }
            }
            catch (Exception ex)
            {
                if (!(ex is ArgumentNullException || ex is NullReferenceException)) throw;
                Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"{ex.GetType().Name} error occurred while processing {MessageName(Id)} for GameState object");
                return false;
            }

            var serverGreeting = Battlenet.Common.GetServerGreeting(context.Client).Split(Battlenet.Common.NewLine);

            // Welcome to Battle.net!
            foreach (var line in serverGreeting)
                new ChatEvent(ChatEvent.EventIds.EID_INFO, channel.ActiveFlags, 0, channel.Name, line).WriteTo(context.Client);

            if (Product.IsChatRestricted(userGame))
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, activeUserFlags, activeUserPing, onlineName, Resources.GameProductIsChatRestricted).WriteTo(context.Client);
            }

            // Send EID_INFO "Last logon: ..."
            new ChatEvent(ChatEvent.EventIds.EID_INFO, activeUserFlags, activeUserPing, onlineName, Resources.LastLogonInfo.Replace("{timestamp}", lastLogon.ToString(Common.HumanDateTimeFormat))).WriteTo(context.Client);

            var failedLogins = context.Client.GameState.FailedLogons;
            context.Client.GameState.FailedLogons = 0;

            if (failedLogins > 0)
            {
                new ChatEvent(ChatEvent.EventIds.EID_ERROR, activeUserFlags, activeUserPing, onlineName, Resources.FailedLogonAttempts.Replace("{count}", failedLogins.ToString("##,0"))).WriteTo(context.Client);
            }

            return true;
        }
    }
}
