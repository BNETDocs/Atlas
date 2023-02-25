using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_SETEMAIL : Message
    {
        public SID_SETEMAIL()
        {
            Id = (byte)MessageIds.SID_SETEMAIL;
            Buffer = new byte[0];
        }

        public SID_SETEMAIL(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_SETEMAIL;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            if (context == null || context.Client == null || !context.Client.Connected || context.Client.GameState == null) return false;

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                {
                    if (Buffer.Length < 1)
                        throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 1 bytes");

                    using var m = new MemoryStream(Buffer);
                    using var r = new BinaryReader(m);

                    var emailAddress = r.ReadByteString();
                    if (emailAddress.Length < 0 || emailAddress.Length > 255)
                        throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} email address must be within 0-255 bytes, got {emailAddress.Length}");

                    var gameState = context.Client.GameState;
                    if (gameState.ActiveAccount == null)
                        throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} cannot be sent before logging into an account");

                    gameState.ActiveAccount.Set(Account.EmailKey, emailAddress);
                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Client set email address for account [{gameState.ActiveAccount.Get(Account.UsernameKey, gameState.Username)}]");
                    return true;
                }
                case MessageDirection.ServerToClient:
                {
                    context.Client.Send(ToByteArray(context.Client.ProtocolType));
                    return true;
                }
                default: return false;
            }
        }
    }
}
