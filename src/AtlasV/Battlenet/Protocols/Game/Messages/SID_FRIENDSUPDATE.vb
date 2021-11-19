Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_FRIENDSUPDATE
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_FRIENDSUPDATE)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_FRIENDSUPDATE)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            If context.Direction <> MessageDirection.ServerToClient Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from server to client")
            Dim entry = CByte(context.Arguments("entry"))
            Dim [friend] = CType(context.Arguments("friend"), [Friend])
            Dim status = CByte([friend].StatusId)
            Dim location = CByte([friend].LocationId)
            Dim product = CUInt([friend].ProductCode)
            Dim locationStr = CType([friend].LocationString, Byte())
            Dim bufferSize = CUInt((8 + locationStr.Length))
            Buffer = New Byte(bufferSize - 1) {}

            Using m = New MemoryStream(Buffer)
                Using w = New BinaryWriter(m)
                    w.Write(entry)
                    w.Write(status)
                    w.Write(location)
                    w.Write(product)
                    w.WriteByteString(locationStr)
                End Using
            End Using

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            context.Client.Send(ToByteArray(context.Client.ProtocolType))
            Return True
        End Function
    End Class
End Namespace
