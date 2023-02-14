﻿using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_ENTERCHAT : Message
    {

        public SID_ENTERCHAT()
        {
            Id = (byte)MessageIds.SID_ENTERCHAT;
            Buffer = new byte[0];
        }

        public SID_ENTERCHAT(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_ENTERCHAT;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            var gameState = context.Client.GameState;

            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                        if (Buffer.Length < 2)
                        {
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 2 bytes");
                        }

                        if (gameState.ActiveAccount == null || string.IsNullOrEmpty(gameState.OnlineName))
                        {
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} received before logon");
                        }

                        /**
                         * (STRING) Username
                         * (STRING) Statstring
                         */

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var username = r.ReadByteString(); // Defunct
                        var statstring = r.ReadByteString();

                        var productId = (UInt32)gameState.Product;

                        // Statstring length is either 0 bytes or 4-128 bytes, not including the null-terminator.
                        if (statstring.Length != 0 && (statstring.Length < 4 || statstring.Length > 128))
                            throw new GameProtocolViolationException(context.Client, $"Client sent invalid statstring size in {MessageName(Id)}");

                        if (statstring.Length < 4)
                        {
                            statstring = new byte[4];
                        }

                        using var _m = new MemoryStream(statstring);
                        using var _w = new BinaryWriter(_m);

                        _w.BaseStream.Position = 0;
                        _w.Write(productId); // ensure first 4 bytes of statstring always matches their agreed upon productId

                        return new SID_ENTERCHAT().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>(){{ "username", username }, { "statstring", statstring }}));
                    }
                case MessageDirection.ServerToClient:
                    {
                        var uniqueName = gameState.OnlineName;
                        var statstring = (byte[])context.Arguments["statstring"];
                        var accountName = gameState.Username;

                        /**
                         * (STRING) Unique name
                         * (STRING) Statstring
                         * (STRING) Account name
                         */

                        Buffer = new byte[3 + Encoding.UTF8.GetByteCount(uniqueName) + statstring.Length + Encoding.UTF8.GetByteCount(accountName)];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((string)uniqueName);
                        w.WriteByteString(statstring);
                        w.Write((string)accountName);

                        // Use their statstring if config -> battlenet -> emulation -> statstring_updates is enabled for this product.
                        // Blizzard servers allowed on the fly statstring updates for Diablo, Diablo II (changing characters), and Warcraft III.
                        if (!GameState.CanStatstringUpdate(gameState.Product)) statstring = gameState.GenerateStatstring();

                        lock (gameState)
                        {
                            if (gameState.ActiveChannel == null)
                            {
                                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Entering chat as [{uniqueName}]");

                                gameState.ChannelFlags = (Account.Flags)gameState.ActiveAccount.Get(Account.FlagsKey);

                                if (Product.IsUDPSupported(gameState.Product)
                                    && !gameState.UDPSupported)
                                {
                                    gameState.ChannelFlags |= Account.Flags.NoUDP;
                                }

                                gameState.Statstring = statstring;
                            }
                            else
                            {
                                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Re-entering chat as [{uniqueName}]");
                                gameState.ActiveChannel.UpdateUser(gameState, statstring); // also sets gameState.Statstring = statstring
                            }
                        }

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }

        public new byte[] ToByteArray(ProtocolType protocolType)
        {
            if (protocolType.IsChat())
            {
                using var m = new MemoryStream(Buffer);
                using var r = new BinaryReader(m);
                return Encoding.UTF8.GetBytes($"{2000 + Id} NAME {Encoding.UTF8.GetString(r.ReadByteString())}{Battlenet.Common.NewLine}");
            }
            else
            {
                return base.ToByteArray(protocolType);
            }
        }
    }
}
