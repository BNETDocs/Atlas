using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_AUTH_INFO : Message
    {
        public SID_AUTH_INFO()
        {
            Id = (byte)MessageIds.SID_AUTH_INFO;
            Buffer = new byte[0];
        }

        public SID_AUTH_INFO(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_AUTH_INFO;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_AUTH_INFO ({4 + Buffer.Length} bytes)");

                        if (Buffer.Length < 38)
                            throw new Exceptions.GameProtocolViolationException(context.Client, "SID_AUTH_INFO must be at least 38 bytes");
                        /**
                         * (UINT32) Protocol ID
                         * (UINT32) Platform code
                         * (UINT32) Product code
                         * (UINT32) Version byte
                         * (UINT32) Language code
                         * (UINT32) Local IP
                         * (UINT32) Time zone bias
                         * (UINT32) MPQ locale ID
                         * (UINT32) User language ID
                         * (STRING) Country abbreviation
                         * (STRING) Country
                         */

                        var m = new MemoryStream(Buffer);
                        var r = new BinaryReader(m);

                        context.Client.GameState.ProtocolId = r.ReadUInt32();
                        context.Client.GameState.Platform = (Platform.PlatformCode)r.ReadUInt32();
                        context.Client.GameState.Product = (Product.ProductCode)r.ReadUInt32();
                        context.Client.GameState.Version.VersionByte = r.ReadUInt32();
                        context.Client.GameState.Locale.LanguageCode = r.ReadUInt32();
                        context.Client.GameState.LocalIPAddress = IPAddress.Parse(r.ReadUInt32().ToString());
                        context.Client.GameState.TimezoneBias = r.ReadInt32();
                        context.Client.GameState.Locale.UserLocaleId = r.ReadUInt32();
                        context.Client.GameState.Locale.UserLanguageId = r.ReadUInt32();
                        context.Client.GameState.Locale.CountryNameAbbreviated = r.ReadString();
                        context.Client.GameState.Locale.CountryName = r.ReadString();

                        r.Close();
                        m.Close();

                        try
                        {
                            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] Setting client locale...");
                            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo((int)context.Client.GameState.Locale.UserLocaleId);
                        }
                        catch (Exception ex)
                        {
                            if (!(ex is ArgumentOutOfRangeException || ex is CultureNotFoundException)) throw;

                            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] Error setting client locale to [{(int)context.Client.GameState.Locale.UserLocaleId}], using default");
                        }

                        var _ping = new SID_PING().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient, new Dictionary<string, object>() {{ "token", context.Client.GameState.PingToken }} ));

                        var _auth_info = new SID_AUTH_INFO().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient));

                        return _ping && _auth_info;
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         *    (UINT32) Logon type
                         *    (UINT32) Server token
                         *    (UINT32) UDP value
                         *  (FILETIME) CheckRevision MPQ filetime
                         *    (STRING) CheckRevision MPQ filename
                         *    (STRING) CheckRevision Formula
                         *
                         *  WAR3/W3XP Only:
                         *      (VOID) 128-byte Server signature
                         */

                        var MPQFiletime = (UInt64)0;
                        var MPQFilename = "ver-IX86-1.mpq";
                        var Formula = "A=3845581634 B=880823580 C=1363937103 4 A=A-S B=B-C C=C-A A=A-B";

                        Buffer = new byte[22 + MPQFilename.Length + Formula.Length + (Product.IsWarcraftIII(context.Client.GameState.Product) ? 128 : 0)];

                        var m = new MemoryStream(Buffer);
                        var w = new BinaryWriter(m);

                        w.Write((UInt32)context.Client.GameState.LogonType);
                        w.Write((UInt32)context.Client.GameState.ServerToken);
                        w.Write((UInt32)context.Client.GameState.UDPToken);
                        w.Write(MPQFiletime);
                        w.Write(MPQFilename);
                        w.Write(Formula);
                        
                        if (Product.IsWarcraftIII(context.Client.GameState.Product))
                            w.Write(new byte[128]);

                        w.Close();
                        m.Close();

                        Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_AUTH_INFO ({4 + Buffer.Length} bytes)");
                        context.Client.Send(ToByteArray());
                        return true;
                    }
            }

            return false;
        }
    }
}
