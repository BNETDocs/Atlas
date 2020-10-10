using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_JOINCHANNEL : Message
    {
        public enum Flags
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

            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_JOINCHANNEL (" + (4 + Buffer.Length) + " bytes)");

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

            var channel = Channel.GetChannelByName(channelName);

            if (channel == null && flags == Flags.NoCreate)
            {
                Channel.WriteChatEvent(context.Client, SID_CHATEVENT.EventIds.EID_CHANNELNOTFOUND, 0, 0, context.Client.State.OnlineName, channelName);
                return true;
            }

            if (channel == null)
            {
                channel = new Channel(channelName, 0);
                Battlenet.Common.ActiveChannels.Add(channelName, channel);
            }

            channel.AcceptUser(context.Client.State);
            return true;
        }
    }
}
