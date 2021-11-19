Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_FRIENDSLIST
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_FRIENDSLIST)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_FRIENDSLIST)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length <> 0 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be 0 bytes")
                    If context.Client.GameState Is Nothing OrElse context.Client.GameState.ActiveAccount Is Nothing Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} cannot be processed without an active login")
                    Return New SID_FRIENDSLIST().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient))
                Case MessageDirection.ServerToClient
                    Dim bufferSize = CUInt(1)
                    Dim friends = New List(Of [Friend])()
                    Dim friendStrings = CType(context.Client.GameState.ActiveAccount.[Get](Account.FriendsKey, New List(Of Byte())(),), List(Of Byte()))

                    For Each friendString In friendStrings
                        Dim [friend] = New [Friend](context.Client.GameState, friendString)
                        friends.Add([friend])
                        bufferSize += CUInt((8 + [friend].Username.Length + [friend].LocationString.Length))

                        If friends.Count = 255 Then
                            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Game, $"Hard limit of 255 friends reached, dropping remaining {friendStrings.Count - 255} friends from {MessageName(Id)} reply")
                            Exit For
                        End If
                    Next

                    Buffer = New Byte(bufferSize - 1) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CByte(friends.Count))

                            For Each [friend] In friends
                                w.WriteByteString([friend].Username)
                                w.Write(CByte([friend].StatusId))
                                w.Write(CByte([friend].LocationId))
                                w.Write(CUInt([friend].ProductCode))
                                w.WriteByteString([friend].LocationString)
                            Next
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
