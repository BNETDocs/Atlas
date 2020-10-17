using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_READUSERDATA : Message
    {

        public SID_READUSERDATA()
        {
            Id = (byte)MessageIds.SID_READUSERDATA;
            Buffer = new byte[0];
        }

        public SID_READUSERDATA(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_READUSERDATA;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_READUSERDATA (" + (4 + Buffer.Length) + " bytes)");

                        /**
                         * (UINT32)   Number of Accounts
                         * (UINT32)   Number of Keys
                         * (UINT32)   Request ID
                         * (STRING)[] Requested Accounts
                         * (STRING)[] Requested Keys
                         */

                        if (Buffer.Length < 12)
                            throw new GameProtocolViolationException(context.Client, "SID_READUSERDATA buffer must be at least 12 bytes");

                        if (context.Client.GameState == null || context.Client.GameState.ActiveAccount == null)
                            throw new GameProtocolViolationException(context.Client, "SID_READUSERDATA cannot be processed without an active login");

                        var m = new MemoryStream(Buffer);
                        var r = new BinaryReader(m);

                        var numAccounts = r.ReadUInt32();
                        var numKeys = r.ReadUInt32();
                        var requestId = r.ReadUInt32();

                        var accounts = new List<string>();
                        var keys = new List<string>();

                        for (var i = 0; i < numAccounts; i++)
                            accounts.Add(r.ReadString());

                        for (var i = 0; i < numKeys; i++)
                            keys.Add(r.ReadString());

                        r.Close();
                        m.Close();

                        if (numKeys > 31)
                            throw new GameProtocolViolationException(context.Client, "SID_READUSERDATA must request no more than 31 keys");

                        return new SID_READUSERDATA().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object> {
                            { "requestId", requestId },
                            { "accounts", accounts },
                            { "keys", keys },
                        }));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32)    Number of accounts
                         * (UINT32)    Number of keys
                         * (UINT32)    Request ID
                         * (STRING) [] Requested Key Values
                         */

                        var accounts = new List<string>();
                        var keys = new List<string>();
                        var values = new List<string>();

                        Buffer = new byte[12];

                        var m = new MemoryStream(Buffer);
                        var w = new BinaryWriter(m);

                        w.Write((UInt32)accounts.Count);
                        w.Write((UInt32)keys.Count);
                        w.Write((UInt32)context.Arguments["requestId"]);

                        foreach (var value in values)
                            w.Write((string)value);

                        w.Close();
                        m.Close();

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_READUSERDATA (" + (4 + Buffer.Length) + " bytes)");
                        context.Client.Send(ToByteArray());
                        return true;
                    }
            }

            return false;
        }
    }
}
