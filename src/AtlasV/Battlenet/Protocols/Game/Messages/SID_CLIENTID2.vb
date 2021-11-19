Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_CLIENTID2
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_CLIENTID2)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_CLIENTID2)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If context.Direction <> MessageDirection.ClientToServer Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server")
            If Buffer.Length < 22 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 22 bytes")

            Dim serverVersion, registrationAuthority, registrationVersion, accountNumber, registrationToken As UInt32
            Dim pcComputerName, pcUserName As String
            Using m = New MemoryStream(Buffer)
                Using r = New BinaryReader(m)
                    serverVersion = r.ReadUInt32()

                    Select Case serverVersion
                        Case 0
                            registrationAuthority = r.ReadUInt32()
                            registrationVersion = r.ReadUInt32()
                            Exit Select
                        Case 1
                            registrationVersion = r.ReadUInt32()
                            registrationAuthority = r.ReadUInt32()
                            Exit Select
                        Case Else
                            Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} has invalid server version [{serverVersion}]")
                    End Select

                    accountNumber = r.ReadUInt32()
                    registrationToken = r.ReadUInt32()
                    pcComputerName = r.ReadString()
                    pcUserName = r.ReadString()
                End Using
            End Using

            Return New SID_CLIENTID().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient)) AndAlso New SID_LOGONCHALLENGEEX().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient)) AndAlso New SID_PING().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New System.Collections.Generic.Dictionary(Of String, Object)() From {
                {"token", context.Client.GameState.PingToken}
            }))
        End Function
    End Class
End Namespace
