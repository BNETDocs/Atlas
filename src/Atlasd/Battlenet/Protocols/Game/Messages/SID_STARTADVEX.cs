using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_STARTADVEX : Message
    {
        public SID_STARTADVEX()
        {
            Id = (byte)MessageIds.SID_STARTADVEX;
            Buffer = new byte[0];
        }

        public SID_STARTADVEX(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_STARTADVEX;
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
                         * (UINT32) Game State
                         * (UINT32) Game Elapsed Time (in seconds)
                         * (UINT16) Game Type
                         * (UINT16) Parameter [editor's note: probably sub game type, see SID_GETADVLISTEX]
                         * (UINT32) Unknown (0x00) [editor's note: probably viewing filter, see SID_GETADVLISTEX]
                         * (UINT32) Unknown (Likely ladder, but will always be 0x00 because there is no SSHR ladder) [editor's note: probably "unknown" in SID_GETADVLISTEX]
                         * (STRING) Game name
                         * (STRING) Game password
                         * (STRING) Game Statstring
                         */

                        if (Buffer.Length < 23)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 23 bytes");

                        if (context.Client.GameState.ActiveAccount == null)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} was received before logon");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var gameState = r.ReadUInt32();
                        var gameElapsedTime = r.ReadUInt32();
                        var gameType = r.ReadUInt16();
                        var subGameType = r.ReadUInt16();
                        var viewingFilter = r.ReadUInt32();
                        var reserved = r.ReadUInt32();
                        var gameName = r.ReadByteString();
                        var gamePassword = r.ReadByteString();
                        var gameStatstring = r.ReadByteString();

                        var gameAds = Battlenet.Common.ActiveGameAds.ToArray();
                        GameAd gameAd = null;

                        foreach (var _pair in gameAds)
                        {
                            if (_pair.Value != null && _pair.Value.HasClient(context.Client.GameState))
                            {
                                gameAd = _pair.Value;
                                break;
                            }
                        }

                        if (gameAd == null)
                        {
                            gameAd = new GameAd(context.Client.GameState, gameName, gamePassword, gameStatstring, 6112, (GameAd.GameTypes)gameType, subGameType, context.Client.GameState.Version.VersionByte);
                            context.Client.GameState.GameAd = gameAd;
                            Battlenet.Common.ActiveGameAds.TryAdd(gameName, gameAd);
                        }

                        gameAd.SetActiveStateFlags((GameAd.StateFlags)gameState);
                        gameAd.SetElapsedTime(gameElapsedTime);
                        gameAd.SetGameType((GameAd.GameTypes)gameType);
                        gameAd.SetName(gameName);
                        gameAd.SetPassword(gamePassword);
                        gameAd.SetPort(6112);
                        gameAd.SetStatstring(gameStatstring);

                        return new SID_STARTADVEX().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Status (0x00 Failed, 0x01 Success)
                         */

                        Buffer = new byte[4];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)1); // success = 1

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
