using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_LOCALEINFO : Message
    {
        public SID_LOCALEINFO()
        {
            Id = (byte)MessageIds.SID_LOCALEINFO;
            Buffer = new byte[16];
        }

        public SID_LOCALEINFO(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_LOCALEINFO;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] SID_LOCALEINFO ({4 + Buffer.Length} bytes)");

            if (context.Direction != MessageDirection.ClientToServer)
                throw new GameProtocolViolationException(context.Client, "SID_LOCALEINFO must be sent client to server");

            /**
                * (FILETIME) System time
                * (FILETIME) Local time
                *   (UINT32) Timezone bias
                *   (UINT32) System LCID
                *   (UINT32) User LCID
                *   (UINT32) User language ID
                *   (STRING) Abbreviated language name
                *   (STRING) Country code
                *   (STRING) Abbreviated country name
                *   (STRING) Country name
                */

            if (Buffer.Length < 36)
                throw new GameProtocolViolationException(context.Client, "SID_LOCALEINFO buffer must be at least 36 bytes");

            var m = new MemoryStream(Buffer);
            var r = new BinaryReader(m);

            var systemTime = r.ReadUInt64();
            var localTime = r.ReadUInt64();
            context.Client.GameState.TimezoneBias = r.ReadInt32();
            context.Client.GameState.Locale.SystemLocaleId = r.ReadUInt32();
            context.Client.GameState.Locale.UserLocaleId = r.ReadUInt32();
            context.Client.GameState.Locale.UserLanguageId = r.ReadUInt32();
            context.Client.GameState.Locale.LanguageNameAbbreviated = r.ReadString();
            context.Client.GameState.Locale.CountryCode = r.ReadString();
            context.Client.GameState.Locale.CountryNameAbbreviated = r.ReadString();
            context.Client.GameState.Locale.CountryName = r.ReadString();

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

            r.Close();
            m.Close();

            return true;
        }
    }
}