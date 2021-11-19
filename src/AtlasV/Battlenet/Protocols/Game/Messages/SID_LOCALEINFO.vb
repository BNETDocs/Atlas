Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO
Imports System.Threading

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_LOCALEINFO
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_LOCALEINFO)
            Buffer = New Byte(15) {}
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_LOCALEINFO)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If context.Direction <> MessageDirection.ClientToServer Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent client to server")
            If Buffer.Length < 36 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 36 bytes")

            Using m = New MemoryStream(Buffer)
                Using r = New BinaryReader(m)
                    Dim systemTime = r.ReadUInt64()
                    Dim localTime = r.ReadUInt64()
                    context.Client.GameState.TimezoneBias = r.ReadInt32()
                    context.Client.GameState.Locale.SystemLocaleId = r.ReadUInt32()
                    context.Client.GameState.Locale.UserLocaleId = r.ReadUInt32()
                    context.Client.GameState.Locale.UserLanguageId = r.ReadUInt32()
                    context.Client.GameState.Locale.LanguageNameAbbreviated = r.ReadString()
                    context.Client.GameState.Locale.CountryCode = r.ReadString()
                    context.Client.GameState.Locale.CountryNameAbbreviated = r.ReadString()
                    context.Client.GameState.Locale.CountryName = r.ReadString()
                End Using
            End Using

            context.Client.GameState.SetLocale()
            Return True
        End Function
    End Class
End Namespace
