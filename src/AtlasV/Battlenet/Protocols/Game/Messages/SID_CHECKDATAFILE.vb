Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_CHECKDATAFILE
        Inherits Message

        Public Enum RequestIds As UInt32
            TermsOfService_usa = &H1
            BnServerListW3 = &H3
            rereTermsOfService_USA = &H1A
            BnServerList = &H1B
            IconsSC = &H1D
            BnServerListD2 = &H80000004UI
            ExtraOptionalWorkIX86 = &H80000005UI
            ExtraRequiredWorkIX86 = &H80000006UI
        End Enum

        Public Sub New()
            Id = CByte(MessageIds.SID_CHECKDATAFILE)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_CHECKDATAFILE)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length < 21 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 21 bytes")

                    Using m = New MemoryStream(Buffer) 'Apparently you dont know this, but this does nothing
                        Using r = New BinaryReader(m)
                            Dim fileChecksum = r.ReadBytes(20)
                            Dim fileName = r.ReadByteString()
                        End Using
                    End Using

                    Return New SID_CHECKDATAFILE().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient))
                Case MessageDirection.ServerToClient
                    Buffer = New Byte(3) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CUInt(0))
                        End Using
                    End Using

                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    context.Client.Send(ToByteArray(context.Client.ProtocolType))
                    Return True
            End Select

            Return False
        End Function
    End Class
End Namespace
