Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Runtime.InteropServices

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_READUSERDATA
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_READUSERDATA)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_READUSERDATA)
            Buffer = varBuffer
        End Sub

#Disable Warning CA1822 ' Mark members as static
        Private Sub CollectValues(ByVal requester As GameState, ByVal accounts As List(Of String), ByVal keys As List(Of String), <Out> ByRef values As List(Of String))
#Enable Warning CA1822 ' Mark members as static
            values = New List(Of String)()

            For i = 0 To accounts.Count - 1
                Dim accountName = accounts(i)
                Dim rereAccount As Account = Nothing

                SyncLock Battlenet.Common.AccountsDb
                    Battlenet.Common.AccountsDb.TryGetValue(accountName, rereAccount)
                End SyncLock

                If rereAccount Is Nothing Then

                    For j = 0 To keys.Count - 1
                        values.Add("")
                    Next

                    Continue For
                End If

                For Each reqKey In keys
                    Dim kv As AccountKeyValue = Nothing
                    rereAccount.[Get](reqKey, kv)

                    If kv Is Nothing Then
                        values.Add("")
                        Continue For
                    End If

                    If kv.Readable <> AccountKeyValue.ReadLevel.Any Then

                        If Not (kv.Readable = AccountKeyValue.ReadLevel.Owner AndAlso Object.Equals(rereAccount, requester.ActiveAccount) = True) Then
                            values.Add("")
                            Continue For
                        End If
                    End If

                    Try

                        If kv.Value IsNot Nothing Then
                            If TypeOf kv.Value Is String Then
                                values.Add(kv.Value)
                            ElseIf TypeOf kv.Value Is Long Then
                                values.Add(kv.Value.ToString())
                            ElseIf TypeOf kv.Value Is ULong Then
                                values.Add(kv.Value.ToString())
                            ElseIf TypeOf kv.Value Is Integer Then
                                values.Add(kv.Value.ToString())
                            ElseIf TypeOf kv.Value Is UInteger Then
                                values.Add(kv.Value.ToString())
                            ElseIf TypeOf kv.Value Is Short Then
                                values.Add(kv.Value.ToString())
                            ElseIf TypeOf kv.Value Is UShort Then
                                values.Add(kv.Value.ToString())
                            ElseIf TypeOf kv.Value Is Byte Then
                                values.Add(kv.Value.ToString())
                            ElseIf TypeOf kv.Value Is Boolean Then
                                values.Add(If(kv.Value, "1", "0"))
                            ElseIf TypeOf kv.Value Is DateTime Then
                                Dim _value = kv.Value.ToFileTime()
                                Dim high = CUInt(_value >> 32)
                                Dim low = CUInt(_value)
                                values.Add(high.ToString() & " " & low.ToString())
                            Else
                                values.Add("")
                            End If
                        Else
                            values.Add("")
                        End If

                    Catch __unusedException1__ As Exception
                        values.Add("")
                    End Try
                Next
            Next
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Dim values As List(Of String) = Nothing

            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length < 12 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 12 bytes")
                    If context.Client.GameState Is Nothing OrElse context.Client.GameState.Version Is Nothing OrElse context.Client.GameState.Version.VersionByte = 0 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} cannot be processed without an active version")

                    Dim numAccounts, numKeys, requestId As UInt32
                    Dim accounts = New List(Of String)()
                    Dim keys = New List(Of String)()
                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            numAccounts = r.ReadUInt32()
                            numKeys = r.ReadUInt32()
                            requestId = r.ReadUInt32()

                            For i = 0 To numAccounts - 1
                                accounts.Add(r.ReadString())
                            Next

                            For i = 0 To numKeys - 1
                                keys.Add(r.ReadString())
                            Next
                        End Using
                    End Using

                    If numAccounts > 1 Then
                        accounts = New List(Of String)()
                        keys = New List(Of String)()
                    End If

                    If numKeys > 31 Then
                        Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must request no more than 31 keys")
                    End If

                    Return New SID_READUSERDATA().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object) From {
                        {"requestId", requestId},
                        {"accounts", accounts},
                        {"keys", keys}
                    }))
                Case MessageDirection.ServerToClient
                    Dim accounts = CType(context.Arguments("accounts"), List(Of String))
                    Dim keys = CType(context.Arguments("keys"), List(Of String))
                    CollectValues(context.Client.GameState, accounts, keys, values)
                    Dim size = 12

                    For Each value In values
                        size += 1 + Encoding.UTF8.GetByteCount(value)
                    Next

                    Buffer = New Byte(size - 1) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CUInt(accounts.Count))
                            w.Write(CUInt(keys.Count))
                            w.Write(CUInt(context.Arguments("requestId")))

                            For Each value In values
                                w.Write(CStr(value))
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
