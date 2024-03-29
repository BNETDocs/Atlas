﻿using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_CHATEVENT : Message
    {
        public SID_CHATEVENT()
        {
            Id = (byte)MessageIds.SID_CHATEVENT;
            Buffer = new byte[26];
        }

        public SID_CHATEVENT(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_CHATEVENT;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            if (context.Direction == MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, $"Client is not allowed to send {MessageName(Id)}");

            var chatEvent = (ChatEvent)context.Arguments["chatEvent"];
            Buffer = chatEvent.ToByteArray(context.Client.ProtocolType.Type);

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)}: {ChatEvent.EventIdToString(chatEvent.EventId)} ({4 + Buffer.Length:D} bytes)");

            return true;
        }

        public new byte[] ToByteArray(ProtocolType protocolType)
        {
            if (protocolType.IsChat())
            {
                return Buffer;
            }
            else
            {
                return base.ToByteArray(protocolType);
            }
        }
    }
}
