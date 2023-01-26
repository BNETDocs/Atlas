Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_SYSTEMINFO
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_SYSTEMINFO)
            Buffer = New Byte(15) {}
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_SYSTEMINFO)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If context.Direction <> MessageDirection.ClientToServer Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server")
            If Buffer.Length <> 28 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 28 bytes")

            Using m = New MemoryStream(Buffer)
                Using r = New BinaryReader(m)
                    Dim cpuCount = r.ReadUInt32()
                    Dim cpuArch = r.ReadUInt32()
                    Dim cpuLevel = r.ReadUInt32()
                    Dim cpuTiming = r.ReadUInt32()
                    Dim totalRAM = r.ReadUInt32()
                    Dim totalSwap = r.ReadUInt32()
                    Dim freeDiskSpace = r.ReadUInt32()
                End Using
            End Using

            Return True
        End Function
    End Class
End Namespace
