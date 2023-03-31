using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Linq;
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
            if (context == null || context.Client == null || !context.Client.Connected) return false;
            var gameState = context.Client.GameState;

            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                        if (Buffer.Length < 2)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 2 bytes");

                        if (gameState.ActiveAccount == null || string.IsNullOrEmpty(gameState.OnlineName))
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} received before logon");

                        /**
                         * (STRING) Username
                         * (STRING) Statstring
                         */

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);
                        var username = r.ReadByteString(); // Defunct
                        var statstring = r.ReadByteString();

                        var productId = (UInt32)gameState.Product;

                        // Statstring has a maximum length of 128 bytes
                        if (statstring.Length > 128)
                            throw new GameProtocolViolationException(context.Client, $"Client sent invalid statstring size in {MessageName(Id)}");

                        return new SID_ENTERCHAT().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient,
                            new Dictionary<string, dynamic>(){{ "username", username }, { "statstring", statstring }})
                        );
                    }
                case MessageDirection.ServerToClient:
                    {
                        var uniqueName = gameState.OnlineName;
                        var statstring = context.Arguments.ContainsKey("statstring") ? (byte[])context.Arguments["statstring"] : gameState.Statstring;
                        var accountName = gameState.Username;

                        if (Product.IsDiabloII(gameState.Product))
                        {
                            statstring = Product.ToByteArray(gameState.Product);
                        }

                        // Do not use client-provided statstring if config.battlenet.emulation.statstring_updates is not enabled for this product.
                        // Blizzard servers allowed statstring updates for Diablo, Diablo II (changing characters), Warcraft III (changing icons), and Shareware variants.
                        if (!GameState.CanStatstringUpdate(gameState.Product) || statstring.Length == 0) statstring = gameState.GenerateStatstring();

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

                        lock (gameState)
                        {
                            var newFlags = (Account.Flags)gameState.ActiveAccount.Get(Account.FlagsKey);
                            if (!gameState.UDPSupported && (Product.IsUDPSupported(gameState.Product) || Product.IsChat(gameState.Product))) newFlags |= Account.Flags.NoUDP;

                            var newPing = gameState.Ping;
                            if (Product.IsChat(gameState.Product)) newPing = 0;

                            if (gameState.GameAd != null)
                            {
                                var gameAd = gameState.GameAd;
                                if (gameAd.RemoveClient(gameState)) gameState.GameAd = null;
                                if (gameAd.Clients.Count == 0) lock (Battlenet.Common.ActiveGameAds) Battlenet.Common.ActiveGameAds.Remove(gameAd);
                            }

                            if (gameState.ActiveChannel == null)
                            {
                                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Entering chat as [{uniqueName}]");

                                gameState.ChannelFlags = newFlags;
                                gameState.Ping = newPing;
                                gameState.Statstring = statstring;
                            }
                            else
                            {
                                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Re-entering chat as [{uniqueName}]");
                                gameState.ActiveChannel.UpdateUser(gameState, newFlags, newPing, statstring);
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
                using var _m = new MemoryStream(Buffer);
                using var r = new BinaryReader(_m);
                var username = r.ReadByteString();
                var statstring = r.ReadByteString();

                using var m = new MemoryStream(0xFFFF);
                using var w = new System.IO.BinaryWriter(m);
                w.Write(Encoding.UTF8.GetBytes($"{2000 + Id} NAME "));
                w.Write(username);
                w.Write(Encoding.UTF8.GetBytes(Battlenet.Common.NewLine));
                return m.GetBuffer()[0..(int)w.BaseStream.Length];
            }
            else
            {
                return base.ToByteArray(protocolType);
            }
        }
    }
}
