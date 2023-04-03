using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Atlasd.Helpers;
using Atlasd.Utilities;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_LOGONREALMEX : Message
    {
        public enum Statuses : UInt32
        {
            RealmUnavailable = 0x80000001,
            RealmLogonFailed = 0x80000002,
        }

        public SID_LOGONREALMEX()
        {
            Id = (byte)MessageIds.SID_LOGONREALMEX;
            Buffer = new byte[0];
        }

        public SID_LOGONREALMEX(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_LOGONREALMEX;
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

                        if (!Product.IsDiabloII(gameState.Product))
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from D2DV or D2XP");

                        if (Buffer.Length < 25)
                            throw new GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be at least 25 bytes, got {Buffer.Length}");

                        /**
                         * (UINT32) Client Token
                         * (UINT8)[20] Hashed realm password
                         * (STRING) Realm title
                         */

                        using var m = new MemoryStream(Buffer);
                        using var r = new BinaryReader(m);

                        var clientToken = r.ReadUInt32();
                        var inPassword = r.ReadBytes(20);
                        var realmTitle = r.ReadByteString();

                        return new SID_LOGONREALMEX().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, dynamic>{
                            { "clientToken", clientToken }, { "inPassword", inPassword }, { "realmTitle", realmTitle }
                        }));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * [Note this format is slightly different from BNETDocs reference as of 2023-02-18]
                         * (UINT32)     MCP Cookie (Client Token)
                         * (UINT32)     MCP Status
                         * (UINT32)[2]  MCP Chunk 1
                         * (UINT32)     IP
                         * (UINT32)     Port
                         * (UINT32)[12] MCP Chunk 2
                         * (STRING)     Battle.net unique name (* as of D2 1.14d, this is empty)
                         */

                        if (((byte[])context.Arguments["realmTitle"]).AsString() == "Olympus")
                        {
                            Buffer = new byte[18 * 4 + 1];

                            using var m = new MemoryStream(Buffer);
                            using var w = new BinaryWriter(m);

                            var cookie = (UInt32)(new Random().Next(int.MinValue, int.MaxValue));

                            w.Write((UInt32)cookie);
                            w.Write((UInt32)0x00000000); // status

                            var chunk1 = new UInt32[]
                            {
                                0x33316163,
                                0x65303830
                            }.GetBytes();

                            w.Write(chunk1);

#if DEBUG
                            string ipString = "127.0.0.1";
#else
                            string ipString = NetworkUtilities.GetPublicAddress();
#endif

                            IPAddress ipAddress = IPAddress.Parse(ipString);
                            byte[] ipBytes = ipAddress.GetAddressBytes();
                            int ipInt = BitConverter.ToInt32(ipBytes, 0);
                            int networkOrderInt = IPAddress.NetworkToHostOrder(ipInt);
                            byte[] bytes = BitConverter.GetBytes(networkOrderInt).Reverse().ToArray();

                            w.Write(bytes);

                            Settings.State.RootElement.TryGetProperty("battlenet", out var battlenetJson);
                            battlenetJson.TryGetProperty("realm_listener", out var listenerJson);
                            listenerJson.TryGetProperty("interface", out var interfaceJson);
                            listenerJson.TryGetProperty("port", out var portJson);

                            portJson.TryGetUInt16(out var port);

                            ushort hostOrderPort = port;
                            UInt32 networkOrderPort = (UInt32)IPAddress.HostToNetworkOrder((short)hostOrderPort);

                            w.Write(networkOrderPort);

                            // this could use some love
                            var chunk2 = new UInt32[]
                            {
                                0x66663162,
                                0x34613566,
                                0x64326639,
                                0x63336330,
                                0x38326135,
                                0x39663937,
                                0x62653134,
                                0x36313861,
                                0x36353032,
                                0x31353066,
                                0x00000000,
                                0x00000000
                            }.GetBytes();

                            w.Write(chunk2);
                            w.WriteByteString(Encoding.UTF8.GetBytes(""));

                            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)");

                            // to allow associating game and realm connections after MCP_STARTUP
                            Battlenet.Common.RealmClientStates.TryAdd(cookie, context.Client);
                        }
                        else
                        {
                            Buffer = new byte[4];

                            using var m = new MemoryStream(Buffer);
                            using var w = new BinaryWriter(m);

                            w.Write((UInt32)Statuses.RealmUnavailable);
                        }

                        context.Client.Send(ToByteArray(context.Client.ProtocolType));
                        return true;
                    }
            }

            return false;
        }
    }
}
