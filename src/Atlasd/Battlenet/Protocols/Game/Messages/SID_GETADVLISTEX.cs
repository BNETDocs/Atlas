using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_GETADVLISTEX : Message
    {
        public SID_GETADVLISTEX()
        {
            Id = (byte)MessageIds.SID_GETADVLISTEX;
            Buffer = new byte[0];
        }

        public SID_GETADVLISTEX(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_GETADVLISTEX;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                        /**
                         * (UINT16) Game Type
                         * (UINT16) Sub Game Type
                         * (UINT32) Viewing Filter
                         * (UINT32) Reserved (0)
                         * (UINT32) Number of Games
                         * (STRING) Game Name
                         * (STRING) Game Password
                         * (STRING) Game Statstring
                         */

                        if (Buffer.Length < 19)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 19 bytes");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var gameType = r.ReadUInt16();
                        var subGameType = r.ReadUInt16();
                        var viewingFilter = r.ReadUInt32();
                        var reserved = r.ReadUInt32();
                        var numberOfGames = r.ReadUInt32();
                        var gameName = r.ReadByteString();
                        var gamePassword = r.ReadByteString();
                        var gameStatstring = r.ReadByteString();

                        var gameAds = new List<GameAd>();

                        lock (Battlenet.Common.ActiveGameAds)
                        {
                            IList<GameAd> toDelete = new List<GameAd>();
                            foreach (var gameAd in Battlenet.Common.ActiveGameAds)
                            {
                                if (gameAd.Clients.Count == 0)
                                {
                                    toDelete.Add(gameAd);
                                    continue;
                                }

                                if (gameAd.Product != context.Client.GameState.Product) continue;

                                if (viewingFilter == 0xFFFF || viewingFilter == 0x30)
                                {
                                    if (gameType != 0 && gameType != (ushort)gameAd.GameType) continue;
                                    if (subGameType != 0 && subGameType != gameAd.SubGameType) continue;
                                }
                                else if (viewingFilter == 0xFF80) { }

                                gameAds.Add(gameAd);
                            }
                            while (toDelete.Count > 0)
                            {
                                Battlenet.Common.ActiveGameAds.Remove(toDelete[0]);
                                toDelete.RemoveAt(0);
                            }
                        }

                        return new SID_GETADVLISTEX().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>(){
                            { "gameAds", gameAds }
                        }));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Number of games
                         *
                         * If count is 0:
                         *    (UINT32) Status
                         *
                         * Otherwise, games are listed thus:
                         *    For each list item:
                         *       (UINT32) Game settings
                         *       (UINT32) Language ID
                         *       (UINT16) Address Family (Always AF_INET)
                         *       (UINT16) Port
                         *       (UINT32) Host's IP
                         *       (UINT32) sin_zero (0)
                         *       (UINT32) sin_zero (0)
                         *       (UINT32) Game status
                         *       (UINT32) Elapsed time (in seconds)
                         *       (STRING) Game name
                         *       (STRING) Game password
                         *       (STRING) Game statstring
                         */

                        var gameAds = (List<GameAd>)context.Arguments["gameAds"];
                        var size = (ulong)0;

                        foreach (var gameAd in gameAds)
                        {
                            size += 35 + (ulong)gameAd.Name.Length + (ulong)gameAd.Password.Length + (ulong)gameAd.Statstring.Length;
                        }

                        if (size == 0)
                        {
                            size = 4; // status
                        }

                        Buffer = new byte[4 + size];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)gameAds.Count); // number of games
                        if (gameAds.Count == 0)
                        {
                            w.Write((UInt32)0); // status 0 = success
                        } else
                        {
                            foreach (var gameAd in gameAds)
                            {
                                w.Write(((UInt32)gameAd.GameType) | (((UInt32)gameAd.SubGameType) << 16));
                                w.Write((UInt32)gameAd.Locale.UserLanguageId);
                                w.Write((UInt16)System.Net.Sockets.AddressFamily.InterNetwork); // always AF_INET
                                UInt16 Port;
                                if (gameAd.Clients.Count > 0 && gameAd.Clients[0].GameDataPort != 0)
                                {
                                    Port = gameAd.Clients[0].GameDataPort;
                                }
                                else
                                {
                                    Port = (UInt16) gameAd.GamePort;
                                }
                                // because this is a dumped sockaddr_in structure, the port is in reverse byte order
                                w.Write((UInt16)((Port << 8) | (Port >> 8)));
                                Byte[] bytes;
                                if (gameAd.Clients[0].GameDataAddress != null)
                                {
                                    bytes = gameAd.Clients[0].GameDataAddress.MapToIPv4().GetAddressBytes();
                                }
                                else
                                {
                                    IPEndPoint ipEndPoint = gameAd.Clients[0].Client.RemoteEndPoint as IPEndPoint;
                                    bytes = ipEndPoint.Address.MapToIPv4().GetAddressBytes();
                                }
                                System.Diagnostics.Debug.Assert(bytes.Length == 4);
                                for (int i = 0; i < bytes.Length; i++) w.Write(bytes[i]);
                                w.Write((UInt32)0);
                                w.Write((UInt32)0);
                                w.Write((UInt32)0); // TODO
                                w.Write((UInt32)0); // TODO
                                w.WriteByteString(gameAd.Name);
                                w.WriteByteString(gameAd.Password);
                                w.WriteByteString(gameAd.Statstring);
                            }
                        }

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
