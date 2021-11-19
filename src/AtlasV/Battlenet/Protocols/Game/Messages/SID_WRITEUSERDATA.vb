Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_WRITEUSERDATA
        Inherits Message

        Public Sub New()
            Id = CByte(MessageIds.SID_WRITEUSERDATA)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_WRITEUSERDATA)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
            If context.Direction <> MessageDirection.ClientToServer Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be sent from client to server")
            If Buffer.Length < 8 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 8 bytes")

            Dim numAccounts, numKeys As UInt32
            Dim accounts = New List(Of Byte())()
            Dim keys = New List(Of Byte())()
            Dim values = New List(Of Byte())()
            Using m = New MemoryStream(Buffer)
                Using r = New BinaryReader(m)
                    numAccounts = r.ReadUInt32()
                    numKeys = r.ReadUInt32()

                    While accounts.Count < numAccounts
                        accounts.Add(r.ReadByteString())
                    End While

                    While keys.Count < numKeys
                        keys.Add(r.ReadByteString())
                    End While

                    While values.Count < numKeys
                        values.Add(r.ReadByteString())
                    End While
                End Using
            End Using

            Dim hasSudoPrivs = context.Client.GameState.ChannelFlags.HasFlag(account.Flags.Admin) OrElse context.Client.GameState.ChannelFlags.HasFlag(account.Flags.Employee)
            Dim rereAccount As Account = Nothing, dynvalue As Object = Nothing

            For Each accountNameBytes In accounts
                Dim accountNameStr = Encoding.UTF8.GetString(accountNameBytes)
                Battlenet.Common.AccountsDb.TryGetValue(accountNameStr, rereAccount)

                If rereAccount Is Nothing Then
                    Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "Client attempted to write userdata for an account that does not exist")
                    Return False
                End If

                For i = 0 To numKeys - 1
                    Dim key = Encoding.UTF8.GetString(keys(i))
                    Dim value = values(i)

                    If Not rereAccount.[Get](key, dynvalue) Then
                        Continue For
                    End If

                    Dim kv = TryCast(dynvalue, AccountKeyValue)

                    If Not (kv.Writable = AccountKeyValue.WriteLevel.Any OrElse (kv.Writable = AccountKeyValue.WriteLevel.Owner AndAlso (hasSudoPrivs OrElse Object.Equals(context.Client.GameState.ActiveAccount, rereAccount) = True))) Then
                        Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Client attempted to write userdata to account [{accountNameStr}] key [{key}] but they have no privilege to do so [hasSudoPrivs: {hasSudoPrivs}]")
                        Return False
                    End If

                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Client wrote userdata to account [{accountNameStr}] key [{key}] with [hasSudoPrivs: {hasSudoPrivs}]")
                    kv.Value = value
                Next
            Next

            Return True
        End Function
    End Class
End Namespace
