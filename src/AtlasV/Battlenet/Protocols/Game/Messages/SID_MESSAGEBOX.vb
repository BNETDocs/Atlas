Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_MESSAGEBOX
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_MESSAGEBOX)
            Buffer = New Byte(5) {}
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_MESSAGEBOX)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If context.Direction <> MessageDirection.ServerToClient Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from server to client")
            If Buffer.Length < 6 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 6 bytes")
            Dim style = CUInt(context.Arguments("style"))
            Dim text = CStr(context.Arguments("text"))
            Dim caption = CStr(context.Arguments("caption"))
            Buffer = New Byte(6 + Encoding.UTF8.GetByteCount(text) + Encoding.UTF8.GetByteCount(caption) - 1) {}

            Using m = New MemoryStream(Buffer)
                Using w = New BinaryWriter(m)
                    w.Write(CUInt(style))
                    w.Write(CStr(text))
                    w.Write(CStr(caption))
                End Using
            End Using

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            context.Client.Send(ToByteArray(context.Client.ProtocolType))
            Return True
        End Function
    End Class
End Namespace
