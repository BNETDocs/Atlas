Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_STARTADVEX2
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_STARTADVEX2)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_STARTADVEX2)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length < 23 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 23 bytes")
                    If context.Client.GameState.ActiveAccount Is Nothing Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} was received before logon")

                    Dim gameState, gameElapsedTime, viewingFilter, portNumber As UInt32
                    Dim gameType, subGameType As UInt16
                    Dim gameName(), gamePassword(), gameStatstring() As Byte
                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            gameState = r.ReadUInt32()
                            gameElapsedTime = r.ReadUInt32()
                            gameType = r.ReadUInt16()
                            subGameType = r.ReadUInt16()
                            viewingFilter = r.ReadUInt32()
                            portNumber = r.ReadUInt32()
                            gameName = r.ReadByteString()
                            gamePassword = r.ReadByteString()
                            gameStatstring = r.ReadByteString()
                        End Using
                    End Using

                    Dim gameAds = Battlenet.Common.ActiveGameAds.ToArray()
                    Dim gameAd As GameAd = Nothing

                    For Each _pair In gameAds

                        If Object.Equals(_pair.Value.Client, context.Client.GameState) = True Then
                            gameAd = _pair.Value
                            Exit For
                        End If
                    Next

                    If gameAd Is Nothing Then
                        gameAd = New GameAd(context.Client.GameState, gameName, gamePassword, gameStatstring, 6112, CType(gameType, GameAd.GameTypes), subGameType, context.Client.GameState.Version.VersionByte)
                        context.Client.GameState.GameAd = gameAd
                        Battlenet.Common.ActiveGameAds.TryAdd(gameName, gameAd)
                    End If

                    gameAd.SetActiveStateFlags(CType(gameState, GameAd.StateFlags))
                    gameAd.SetElapsedTime(gameElapsedTime)
                    gameAd.SetGameType(CType(gameType, GameAd.GameTypes))
                    gameAd.SetName(gameName)
                    gameAd.SetPassword(gamePassword)
                    gameAd.SetPort(portNumber)
                    gameAd.SetStatstring(gameStatstring)
                    Return New SID_STARTADVEX2().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient))
                Case MessageDirection.ServerToClient
                    Buffer = New Byte(3) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CUInt(1))
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
