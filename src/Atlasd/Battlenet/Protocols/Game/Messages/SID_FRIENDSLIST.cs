using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_FRIENDSLIST : Message
    {

        public SID_FRIENDSLIST()
        {
            Id = (byte)MessageIds.SID_FRIENDSLIST;
            Buffer = new byte[0];
        }

        public SID_FRIENDSLIST(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_FRIENDSLIST;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_FRIENDSLIST (" + (4 + Buffer.Length) + " bytes)");

                        if (Buffer.Length != 0)
                            throw new GameProtocolViolationException(context.Client, "SID_FRIENDSLIST buffer must be 0 bytes");

                        if (context.Client.GameState == null || context.Client.GameState.ActiveAccount == null)
                            throw new GameProtocolViolationException(context.Client, "SID_FRIENDSLIST cannot be processed without an active login");

                        return new SID_FRIENDSLIST().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT8) Number of entries
                         *
                         * For each entry:
                         *   (STRING) Account name
                         *    (UINT8) Status
                         *    (UINT8) Location id
                         *    UINT32) Product id
                         *   (STRING) Location name
                         */

                        var size = (uint)0;
                        var friends = new List<string>();

                        if (context.Client.GameState.ActiveAccount.ContainsKey(Account.FriendsKey))
                            friends = (List<string>)context.Client.GameState.ActiveAccount.Get(Account.FriendsKey);

                        foreach (var friend in friends)
                            size += (uint)(1 + Encoding.ASCII.GetByteCount(friend));

                        Buffer = new byte[size];

                        var m = new MemoryStream(Buffer);
                        var w = new BinaryWriter(m);

                        foreach (var friend in friends)
                            w.Write((string)friend);

                        w.Close();
                        m.Close();

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_FRIENDSLIST (" + (4 + Buffer.Length) + " bytes)");
                        context.Client.Send(ToByteArray());
                        return true;
                    }
            }

            return false;
        }
    }
}
