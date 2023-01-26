Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Net
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_GETADVLISTEX
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_GETADVLISTEX)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_GETADVLISTEX)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length < 19 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 19 bytes")

                    Dim gameType, subGameType As UInt16
                    Dim viewingFilter, reserved, numberOfGames As UInt32
                    Dim gameName(), gamePassword(), gameStatstring() As Byte
                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            gameType = r.ReadUInt16()
                            subGameType = r.ReadUInt16()
                            viewingFilter = r.ReadUInt32()
                            reserved = r.ReadUInt32()
                            numberOfGames = r.ReadUInt32()
                            gameName = r.ReadByteString()
                            gamePassword = r.ReadByteString()
                            gameStatstring = r.ReadByteString()
                        End Using
                    End Using

                    Dim gameAds = New List(Of GameAd)()

                    For Each _pair In Battlenet.Common.ActiveGameAds
                        Dim _ad = _pair.Value
                        If _ad.Client Is Nothing Then Continue For
                        If _ad.Client.Product <> context.Client.GameState.Product Then Continue For

                        If viewingFilter = &HFFFF OrElse viewingFilter = &H30 Then
                            If gameType <> 0 AndAlso gameType <> CUShort(_ad.GameType) Then Continue For
                            If subGameType <> 0 AndAlso subGameType <> _ad.SubGameType Then Continue For
                        ElseIf viewingFilter = &HFF80 Then
                        End If

                        gameAds.Add(_ad)
                    Next

                    Return New SID_GETADVLISTEX().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object)() From {
                        {"gameAds", gameAds}
                    }))
                Case MessageDirection.ServerToClient
                    Dim gameAds = CType(context.Arguments("gameAds"), List(Of GameAd))
                    Dim size = CULng(0)

                    For Each gameAd In gameAds
                        size += 35 + CULng(gameAd.Name.Length) + CULng(gameAd.Password.Length) + CULng(gameAd.Statstring.Length)
                    Next

                    If size = 0 Then
                        size = 4
                    End If

                    Buffer = New Byte(4 + size - 1) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CUInt(gameAds.Count))

                            If gameAds.Count = 0 Then
                                w.Write(CUInt(0))
                            Else

                                For Each gameAd In gameAds
                                    w.Write((gameAd.GameType) Or ((gameAd.SubGameType) << 16))
                                    w.Write(CUInt(gameAd.Client.Locale.UserLanguageId))
                                    w.Write(CUShort(2))
                                    Dim Port As UInt16

                                    If gameAd.Client.GameDataPort <> 0 Then
                                        Port = gameAd.Client.GameDataPort
                                    Else
                                        Port = CUShort(gameAd.GamePort)
                                    End If

                                    w.Write(((Port << 8) Or (Port >> 8)))

                                    Dim bytes As Byte()

                                    If gameAd.Client.GameDataAddress IsNot Nothing Then
                                        bytes = gameAd.Client.GameDataAddress.MapToIPv4().GetAddressBytes()
                                    Else
                                        Dim ipEndPoint As IPEndPoint = TryCast(gameAd.Client.Client.RemoteEndPoint, IPEndPoint)
                                        bytes = ipEndPoint.Address.MapToIPv4().GetAddressBytes()
                                    End If

                                    System.Diagnostics.Debug.Assert(bytes.Length = 4)

                                    For i As Integer = 0 To bytes.Length - 1
                                        w.Write(bytes(i))
                                    Next

                                    w.Write(CUInt(0))
                                    w.Write(CUInt(0))
                                    w.Write(CUInt(0))
                                    w.Write(CUInt(0))
                                    w.WriteByteString(gameAd.Name)
                                    w.WriteByteString(gameAd.Password)
                                    w.WriteByteString(gameAd.Statstring)
                                Next
                            End If
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
