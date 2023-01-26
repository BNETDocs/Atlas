Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO
Imports System.Net

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_GAMEDATAADDRESS
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_GAMEDATAADDRESS)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_GAMEDATAADDRESS)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If context.Direction <> MessageDirection.ClientToServer Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server")
            If Buffer.Length <> 16 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 16 bytes")

            Dim unknown0, port As UInt16
            Dim address() As Byte
            Dim unknown1, unknown2 As UInt32
            Using m = New MemoryStream(Buffer)
                Using r = New BinaryReader(m)
                    unknown0 = r.ReadUInt16()
                    port = r.ReadUInt16()
                    address = r.ReadBytes(4)
                    unknown1 = r.ReadUInt32()
                    unknown2 = r.ReadUInt32()
                End Using
            End Using

            context.Client.GameState.GameDataAddress = New IPAddress(address)
            context.Client.GameState.GameDataPort = port
            context.Client.Send(ToByteArray(context.Client.ProtocolType))
            Return True
        End Function
    End Class
End Namespace
