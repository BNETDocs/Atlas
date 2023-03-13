using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Linq;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_STOPADV : Message
    {
        public SID_STOPADV()
        {
            Id = (byte)MessageIds.SID_STOPADV;
            Buffer = new byte[0];
        }

        public SID_STOPADV(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_STOPADV;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server");

            if (Buffer.Length != 0)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 0 bytes");

            GameState gs = context.Client.GameState;

            if (gs == null)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} was received without an active GameState");

            if (gs.GameAd == null)
                return true; // No game advertisement to stop. No action to do.

            bool gameAdOwner = gs.GameAd != null && gs.GameAd.Owner == gs;
            if (!gameAdOwner)
                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"{MessageName(Id)} was received but they are not the owner of the game advertisement");
            else
                Battlenet.Common.ActiveGameAds.Remove(gs.GameAd);

            return true;
        }
    }
}
