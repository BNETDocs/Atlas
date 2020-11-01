using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_STARTADVEX3 : Message
    {
        public enum Statuses : UInt32
        {
            Success = 0, // Ok
            GameNameExists = 1, // A game by that name already exists!
            GameTypeUnavailable = 2, // Unable to create game because the selected game type is currently unavailable.
            Error = 3, // An error occurred while trying to create the game.
            Error_Alt1 = 4, // An error occurred while trying to create the game.
            Error_Alt2 = 5, // An error occurred while trying to create the game.
        };

        public SID_STARTADVEX3()
        {
            Id = (byte)MessageIds.SID_STARTADVEX3;
            Buffer = new byte[0];
        }

        public SID_STARTADVEX3(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_STARTADVEX3;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_STARTADVEX3 ({4 + Buffer.Length} bytes)");

                        /**
                         * (UINT32) Game State
                         * (UINT32) Game Elapsed Time (in seconds)
                         * (UINT16) Game Type
                         * (UINT16) Sub Game Type
                         * (UINT32) Provider Version Constant
                         * (UINT32) Ladder Type
                         * (STRING) Game Name
                         * (STRING) Game Password
                         * (STRING) Game Statstring
                         */

                        if (Buffer.Length < 23)
                            throw new GameProtocolViolationException(context.Client, "SID_STARTADVEX3 buffer must be at least 23 bytes");

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var gameState = r.ReadUInt32();
                        var gameElapsedTime = r.ReadUInt32();
                        var gameType = r.ReadUInt16();
                        var subGameType = r.ReadUInt16();
                        var providerVersionConstant = r.ReadUInt32();
                        var ladderType = r.ReadUInt32();
                        var gameName = r.ReadString();
                        var gamePassword = r.ReadString();
                        var gameStatstring = r.ReadString();

                        var gameAds = Battlenet.Common.ActiveGameAds.ToArray();
                        GameAd gameAd = null;

                        foreach (var _g in gameAds)
                        {
                            if (_g.Client == context.Client.GameState)
                            {
                                gameAd = _g;
                                break;
                            }
                        }

                        if (gameAd == null)
                        {
                            gameAd = new GameAd(context.Client.GameState, gameName, gamePassword, gameStatstring, 6112, (GameAd.GameTypes)gameType, providerVersionConstant);
                            Battlenet.Common.ActiveGameAds.Add(gameAd);
                        }

                        gameAd.SetActiveStateFlags((GameAd.StateFlags)gameState);
                        gameAd.SetElapsedTime(gameElapsedTime);
                        gameAd.SetGameType((GameAd.GameTypes)gameType);
                        gameAd.SetName(gameName);
                        gameAd.SetPassword(gamePassword);
                        gameAd.SetPort(6112);
                        gameAd.SetStatstring(gameStatstring);

                        return new SID_STARTADVEX3().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (UINT32) Status
                         */

                        Buffer = new byte[4];

                        using var m = new MemoryStream(Buffer);
                        using var w = new BinaryWriter(m);

                        w.Write((UInt32)Statuses.Success);

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_STARTADVEX3 ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
