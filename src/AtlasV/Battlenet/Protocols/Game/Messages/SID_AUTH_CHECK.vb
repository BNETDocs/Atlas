Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_AUTH_CHECK
        Inherits Message

        Public Enum Statuses As UInt32
            Success = &H0
            VersionTooOld = &H100
            InvalidVersion = &H101
            VersionTooNew = &H102
            GameKeyInvalid = &H200
            GameKeyInUse = &H201
            GameKeyBanned = &H202
            GameKeyProductMismatch = &H203
            GameKeyExpansion = &H10
        End Enum

        Public Sub New()
            Id = CByte(MessageIds.SID_AUTH_CHECK)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_AUTH_CHECK)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length < 22 Then Throw New Exceptions.GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be at least 22 bytes")

                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)

                            context.Client.GameState.ClientToken = r.ReadUInt32()
                            context.Client.GameState.Version.EXERevision = r.ReadUInt32()
                            context.Client.GameState.Version.EXEChecksum = r.ReadUInt32()
                            Dim numKeys = r.ReadUInt32()
                            context.Client.GameState.SpawnKey = (r.ReadUInt32() = 1)

                            For i As Integer = 0 To numKeys - 1
                                Dim keyLength = r.ReadUInt32()
                                Dim productValue = r.ReadUInt32()
                                Dim publicValue = r.ReadUInt32()
                                Dim unknownValue = r.ReadUInt32()
                                Dim hashedKeyData = r.ReadBytes(20)
                                If unknownValue <> 0 Then Throw New GameProtocolViolationException(context.Client, "Invalid game key unknown value")
                                Dim gameKey As GameKey = Nothing

                                Try
                                    gameKey = New GameKey(keyLength, productValue, publicValue, hashedKeyData)
                                Catch __unusedGameProtocolViolationException1__ As GameProtocolViolationException
                                    Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "Received invalid game key")
                                    gameKey = Nothing
                                Finally
                                    context.Client.GameState.GameKeys.Add(gameKey)
                                End Try
                            Next

                            context.Client.GameState.Version.EXEInformation = r.ReadByteString()
                            context.Client.GameState.KeyOwner = r.ReadByteString()

                        End Using
                    End Using

                    Dim status = Statuses.Success
                    Dim info As Byte() = Array.Empty(Of Byte)()
                    Dim requiredKeyCount = GameKey.RequiredKeyCount(context.Client.GameState.Product)

                    If context.Client.GameState.GameKeys.Count < requiredKeyCount Then
                        status = Statuses.GameKeyProductMismatch
                        If context.Client.GameState.GameKeys.Count >= 1 Then status = status Or Statuses.GameKeyExpansion
                    End If

                    Return New SID_AUTH_CHECK().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object)() From {
                        {"status", status},
                        {"info", info}
                    }))
                Case MessageDirection.ServerToClient
                    Dim status = CUInt(CType(context.Arguments("status"), Statuses))
                    Dim info = CType(context.Arguments("info"), Byte())
                    Buffer = New Byte(5 + info.Length - 1) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CUInt(status))
                            w.WriteByteString(info)
                        End Using
                    End Using

                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    context.Client.Send(ToByteArray(context.Client.ProtocolType))
                    Return status = CUInt(Statuses.Success)
            End Select

            Return False
        End Function
    End Class
End Namespace
