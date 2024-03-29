﻿using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_GETCHANNELLIST : Message
    {

        public SID_GETCHANNELLIST()
        {
            Id = (byte)MessageIds.SID_GETCHANNELLIST;
            Buffer = new byte[0];
        }

        public SID_GETCHANNELLIST(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_GETCHANNELLIST;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                        if (Buffer.Length != 4)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 4 bytes");

                        /**
                         * (UINT32) Product Id
                         */

                        var channels = new List<byte[]>();

                        foreach (var channel in Battlenet.Common.ActiveChannels.Values)
                        {
                            //if ((channel.ActiveFlags & Channel.Flags.Public) > 0)
                            channels.Add(Encoding.UTF8.GetBytes(channel.Name));
                        }

                        return new SID_GETCHANNELLIST().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {
                            { "channels", channels },
                        }));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (STRING) [] Channel name
                         */

                        var channels = (List<byte[]>)context.Arguments["channels"];
                        var size = (uint)1;

                        foreach (var channel in channels)
                            size += (uint)(1 + channel.Length);

                        Buffer = new byte[size];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        foreach (var channel in channels) w.WriteByteString(channel);
                        w.Write((byte)0); // Official Blizzard servers end list with an empty string.

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
