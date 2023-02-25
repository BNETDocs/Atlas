using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

        private void CollectValues(GameState requester, List<byte[]> accounts, List<byte[]> keys, out List<byte[]> values)
        {
            values = new List<byte[]>();

            var emptyByteString = new byte[0];

            var defaultAccountStr = string.Empty;
            if (!string.IsNullOrEmpty(requester.OnlineName)) defaultAccountStr = requester.OnlineName;
            if (!string.IsNullOrEmpty(requester.Username)) defaultAccountStr = requester.Username;

            for (var i = 0; i < accounts.Count; i++)
            {
                var accountNameStr = Encoding.UTF8.GetString(accounts[i]);

                if (string.IsNullOrEmpty(accountNameStr)) accountNameStr = defaultAccountStr;
                if (accountNameStr.Contains("*")) accountNameStr = accountNameStr[(accountNameStr.IndexOf("*") + 1)..]; // Strip Diablo II character names
                if (accountNameStr.Contains("#")) accountNameStr = accountNameStr[0..accountNameStr.IndexOf("#")]; // Strip serial number

                if (!Battlenet.Common.AccountsDb.TryGetValue(accountNameStr, out Account account) || account == null)
                {
                    Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Game, requester.Client.RemoteEndPoint, $"Client tried reading userdata for an account that does not exist [account: {accountNameStr}]");
                    for (var j = 0; j < keys.Count; j++) values.Add(emptyByteString);
                    continue;
                }
                bool owner = account == requester.ActiveAccount;

                foreach (var reqKey in keys)
                {
                    if (!account.Get(reqKey, out var kv) || kv == null)
                    {
                        values.Add(emptyByteString);
                        continue;
                    }

                    if (kv.Readable != AccountKeyValue.ReadLevel.Any)
                    {
                        if (!(kv.Readable == AccountKeyValue.ReadLevel.Owner && owner))
                        {
                            // No permission
                            values.Add(emptyByteString);
                            continue;
                        }
                    }

                    try
                    {
                        if (kv.Value is string @str)
                        {
                            values.Add(Encoding.UTF8.GetBytes(@str));
                        }
                        else if (kv.Value is long @long)
                        {
                            values.Add(Encoding.UTF8.GetBytes(@long.ToString()));
                        }
                        else if (kv.Value is ulong @ulong)
                        {
                            values.Add(Encoding.UTF8.GetBytes(@ulong.ToString()));
                        }
                        else if (kv.Value is int @int)
                        {
                            values.Add(Encoding.UTF8.GetBytes(@int.ToString()));
                        }
                        else if (kv.Value is uint @uint)
                        {
                            values.Add(Encoding.UTF8.GetBytes(@uint.ToString()));
                        }
                        else if (kv.Value is short @short)
                        {
                            values.Add(Encoding.UTF8.GetBytes(@short.ToString()));
                        }
                        else if (kv.Value is ushort @ushort)
                        {
                            values.Add(Encoding.UTF8.GetBytes(@ushort.ToString()));
                        }
                        else if (kv.Value is byte @byte)
                        {
                            values.Add(Encoding.UTF8.GetBytes(@byte.ToString()));
                        }
                        else if (kv.Value is bool @bool)
                        {
                            values.Add(Encoding.UTF8.GetBytes(@bool ? "1" : "0"));
                        }
                        else if (kv.Value is DateTime @dateTime)
                        {
                            var _value = dateTime.ToFileTime();
                            var high = (uint)(_value >> 32);
                            var low = (uint)_value;
                            values.Add(Encoding.UTF8.GetBytes(high.ToString() + " " + low.ToString()));
                        }
                        else if (kv.Value is byte[] @bytestring)
                        {
                            values.Add(@bytestring);
                        }
                        else
                        {
                            values.Add(emptyByteString);
                        }
                    }
                    catch (Exception)
                    {
                        values.Add(emptyByteString);
                    }
                }
            }
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                        /**
                         * (UINT32)   Number of Accounts
                         * (UINT32)   Number of Keys
                         * (UINT32)   Request ID
                         * (STRING)[] Requested Accounts
                         * (STRING)[] Requested Keys
                         */

                        if (Buffer.Length < 12)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 12 bytes");

                        if (context.Client.GameState == null || context.Client.GameState.Version == null || context.Client.GameState.Version.VersionByte == 0)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} cannot be processed without an active version");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var numAccounts = r.ReadUInt32();
                        var numKeys = r.ReadUInt32();
                        var requestId = r.ReadUInt32();

                        var accounts = new List<byte[]>();
                        var keys = new List<byte[]>();

                        for (var i = 0; i < numAccounts; i++)
                            accounts.Add(r.ReadByteString());

                        for (var i = 0; i < numKeys; i++)
                            keys.Add(r.ReadByteString());

                        if (numAccounts > 1)
                        {
                            accounts = new List<byte[]>();
                            keys = new List<byte[]>();
                        }

                        if (numKeys > 31)
                        {
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must request no more than 31 keys");
                        }

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

                        var accounts = (List<byte[]>)context.Arguments["accounts"];
                        var keys = (List<byte[]>)context.Arguments["keys"];
                        CollectValues(context.Client.GameState, accounts, keys, out var values);

                        var size = 12;
                        foreach (var value in values) size += 1 + value.Length;

                        Buffer = new byte[size];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)accounts.Count);
                        w.Write((UInt32)keys.Count);
                        w.Write((UInt32)context.Arguments["requestId"]);
                        foreach (var value in values) w.WriteByteString(value);

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
