using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
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
                throw new ProtocolViolationException(ProtocolType.Game, "Server isn't allowed to send SID_JOINCHANNEL");

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_JOINCHANNEL (" + (4 + Buffer.Length) + " bytes)");

            if (Buffer.Length < 5)
                throw new ProtocolViolationException(context.Client.ProtocolType, "SID_JOINCHANNEL buffer must be at least 5 bytes");

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

            if (channelName.Length < 1) throw new ProtocolViolationException(context.Client.ProtocolType, "Channel name must be greater than zero");

            if (channelName.Length > 31) channelName = channelName.Substring(0, 31);

            foreach (var c in channelName)
            {
                if ((uint)c < 31) throw new ProtocolViolationException(context.Client.ProtocolType, "Channel name must not have ASCII control characters");
            }

            var userFlags = (Account.Flags)context.Client.GameState.ActiveAccount.Get(Account.FlagsKey);
            var ignoreLimits = userFlags.HasFlag(Account.Flags.Employee);

            var channel = Channel.GetChannelByName(channelName);

            if (channel == null && flags == Flags.NoCreate)
            {
                Channel.WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_CHANNELFULL, Channel.Flags.None, context.Client.GameState.Ping, context.Client.GameState.OnlineName, channelName), context.Client);
                return true;
            }

            if (channel == null) channel = new Channel(channelName, 0);
            channel.AcceptUser(context.Client.GameState, ignoreLimits);

            if (flags == Flags.First || flags == Flags.First_D2)
            {
                var strGame = Product.ProductName(context.Client.GameState.Product, true);
                var numGameOnline = Battlenet.Common.ActiveAccounts.Count;
                var numGameAdvertisements = 0;
                var numTotalOnline = Battlenet.Common.ActiveAccounts.Count;
                var numTotalAdvertisements = 0;

                Channel.WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, channel.ActiveFlags, 0, channel.Name, "Welcome to Battle.net!"), context.Client);
                Channel.WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, channel.ActiveFlags, 0, channel.Name, "This server is hosted by BNETDocs."), context.Client);
                Channel.WriteChatEvent(new ChatEvent(ChatEvent.EventIds.EID_INFO, channel.ActiveFlags, 0, channel.Name, string.Format("There are currently {0:D} users playing {1:D} games of {2}, and {3:D} users playing {4:D} games on Battle.net.", numGameOnline, numGameAdvertisements, strGame, numTotalOnline, numTotalAdvertisements)), context.Client);
            }

            return true;
        }
    }
}
