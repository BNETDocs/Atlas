﻿using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_WRITEUSERDATA : Message
    {
        public SID_WRITEUSERDATA()
        {
            Id = (byte)MessageIds.SID_WRITEUSERDATA;
            Buffer = new byte[0];
        }

        public SID_WRITEUSERDATA(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_WRITEUSERDATA;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server");

            if (Buffer.Length < 8)
                throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 8 bytes");

            using var m = new MemoryStream(Buffer);
            using var r = new BinaryReader(m);

            var numAccounts = r.ReadUInt32();
            var numKeys = r.ReadUInt32();
            var accounts = new List<byte[]>();
            var keys = new List<byte[]>();
            var values = new List<byte[]>();

            while (accounts.Count < numAccounts)
            {
                accounts.Add(r.ReadByteString());
            }

            while (keys.Count < numKeys)
            {
                keys.Add(r.ReadByteString());
            }

            while (values.Count < numKeys)
            {
                values.Add(r.ReadByteString());
            }

            var defaultAccountStr = string.Empty;
            if (!string.IsNullOrEmpty(context.Client.GameState.OnlineName)) defaultAccountStr = context.Client.GameState.OnlineName;
            if (!string.IsNullOrEmpty(context.Client.GameState.Username)) defaultAccountStr = context.Client.GameState.Username;

            var hasSudoPrivs = context.Client.GameState.ChannelFlags.HasFlag(Account.Flags.Admin) ||
                context.Client.GameState.ChannelFlags.HasFlag(Account.Flags.Employee);

            foreach (var accountNameBytes in accounts)
            {
                var accountNameStr = Encoding.UTF8.GetString(accountNameBytes);

                if (string.IsNullOrEmpty(accountNameStr)) accountNameStr = defaultAccountStr;
                if (accountNameStr.Contains("*")) accountNameStr = accountNameStr[(accountNameStr.IndexOf("*") + 1)..]; // Strip Diablo II character names
                if (accountNameStr.Contains("#")) accountNameStr = accountNameStr[0..accountNameStr.IndexOf("#")]; // Strip serial number

                if (!Battlenet.Common.AccountsDb.TryGetValue(accountNameStr, out Account account) || account == null)
                {
                    Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Client tried writing userdata for an account that does not exist [account: {accountNameStr}] [hasSudoPrivs: {hasSudoPrivs}]");
                    return false;
                }
                bool owner = account == context.Client.GameState.ActiveAccount;

                for (var i = 0; i < numKeys; i++)
                {
                    var key = Encoding.UTF8.GetString(keys[i]);
                    var value = values[i];

                    if (!account.Get(key, out AccountKeyValue kv) || kv == null)
                        continue; // do not create client-supplied non-existent userdata keys

                    if (!(kv.Writable == AccountKeyValue.WriteLevel.Any ||
                        (kv.Writable == AccountKeyValue.WriteLevel.Owner && (hasSudoPrivs || owner))))
                    {
                        Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Client tried writing userdata without privileges to do so [account: {accountNameStr}] [key: {key}] [owner: {owner}] [hasSudoPrivs: {hasSudoPrivs}]");
                        return false;
                    }

                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Client wrote userdata [account: {accountNameStr}] [key: {key}] [owner: {owner}] [hasSudoPrivs: {hasSudoPrivs}]");
                    account.Set(kv.Key, value);
                }
            }

            return true;
        }
    }
}
