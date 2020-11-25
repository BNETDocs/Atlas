using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
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
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_FRIENDSLIST ({4 + Buffer.Length} bytes)");

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

                        var size = (uint)1;
                        var friends = new List<Friend>();
                        var friendStrings = (List<string>)context.Client.GameState.ActiveAccount.Get(Account.FriendsKey);

                        foreach (var friendString in friendStrings)
                        {
                            var friend = new Friend(friendString);
                            friends.Add(friend);
                            size += (uint)(8 + Encoding.ASCII.GetByteCount(friend.Username) + Encoding.ASCII.GetByteCount(friend.GetLocationString()));

                            if (friends.Count == 255) // Hard limit based on counter in message format
                            {
                                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Game, $"Hard limit of 255 friends reached, dropping remaining {friends.Count - 255} friends from SID_FRIENDSLIST reply");
                                break;
                            }
                        }

                        Buffer = new byte[size];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((byte)friends.Count);
                        foreach (var friend in friends)
                        {
                            w.Write(friend.Username);
                            w.Write((byte)friend.GetStatus());
                            w.Write((byte)friend.GetLocation());
                            w.Write((uint)friend.GetProductCode());
                            w.Write(friend.GetLocationString());
                        }

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_FRIENDSLIST ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
