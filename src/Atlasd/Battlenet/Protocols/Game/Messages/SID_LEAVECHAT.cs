using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_LEAVECHAT : Message
    {

        public SID_LEAVECHAT()
        {
            Id = (byte)MessageIds.SID_LEAVECHAT;
            Buffer = new byte[0];
        }

        public SID_LEAVECHAT(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_LEAVECHAT;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_LEAVECHAT ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, "SID_LEAVECHAT must be sent from client to server");

            if (Buffer.Length != 0)
                throw new GameProtocolViolationException(context.Client, "SID_LEAVECHAT buffer must be 0 bytes");

            try
            {
                lock (context.Client.GameState.ActiveChannel)
                    context.Client.GameState.ActiveChannel.RemoveUser(context.Client.GameState);
            }
            catch (ArgumentNullException) { }
            catch (NullReferenceException) { }

            return true;
        }
    }
}
