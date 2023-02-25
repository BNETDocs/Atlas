using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_FRIENDSUPDATE : Message
    {

        public SID_FRIENDSUPDATE()
        {
            Id = (byte)MessageIds.SID_FRIENDSUPDATE;
            Buffer = new byte[0];
        }

        public SID_FRIENDSUPDATE(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_FRIENDSUPDATE;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            if (context == null || context.Client == null || !context.Client.Connected || context.Client.GameState == null) return false;

            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                {
                    /**
                     * (UINT8) Entry number
                     */

                    if (Buffer.Length != 1) throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be exactly 1 byte");

                    using var m = new MemoryStream(Buffer);
                    using var r = new BinaryReader(m);

                    var entry = r.ReadByte();

                    var friendStrings = (List<byte[]>)context.Client.GameState.ActiveAccount.Get(Account.FriendsKey, new List<byte[]>());
                    var friendString = friendStrings[entry];
                    var friend = new Friend(context.Client.GameState, friendString);
                    friend.Sync(context.Client.GameState);

                    return new SID_FRIENDSUPDATE().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>{
                        { "entry", entry }, { "friend", friend }
                    }));
                }
                case MessageDirection.ServerToClient:
                {
                    /**
                     *  (UINT8) Entry number
                     *  (UINT8) Status
                     *  (UINT8) Location id
                     * (UINT32) Product id
                     * (STRING) Location name
                     */

                    var entry = (byte)context.Arguments["entry"];
                    var friend = (Friend)context.Arguments["friend"];
                    var status = (byte)friend.StatusId;
                    var location = (byte)friend.LocationId;
                    var product = (UInt32)friend.ProductCode;
                    var locationStr = (byte[])friend.LocationString;

                    var bufferSize = (uint)(8 + locationStr.Length);

                    Buffer = new byte[bufferSize];

                    using var m = new MemoryStream(Buffer);
                    using var w = new BinaryWriter(m);

                    w.Write(entry);
                    w.Write(status);
                    w.Write(location);
                    w.Write(product);
                    w.WriteByteString(locationStr);

                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                    context.Client.Send(ToByteArray(context.Client.ProtocolType));
                    return true;
                }
                default:
                {
                    throw new GameProtocolException(context.Client);
                }
            }
        }
    }
}
