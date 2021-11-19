Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_LOGONRESPONSE
        Inherits Message

        Protected Enum Statuses As UInt32
            Failure = 0
            Success = 1
        End Enum

        Public Sub New()
            Id = CByte(MessageIds.SID_LOGONRESPONSE)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_LOGONRESPONSE)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Dim rereAccount As Account = Nothing

            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length < 29 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} buffer must be at least 29 bytes")

                    Dim clientToken, serverToken As UInt32
                    Dim passwordHash() As Byte
                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            clientToken = r.ReadUInt32()
                            serverToken = r.ReadUInt32()
                            passwordHash = r.ReadBytes(20)
                            context.Client.GameState.Username = r.ReadString()
                        End Using
                    End Using

                    Battlenet.Common.AccountsDb.TryGetValue(context.Client.GameState.Username, rereAccount)

                    If rereAccount Is Nothing Then
                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Account [{context.Client.GameState.Username}] does not exist")
                        Return New SID_LOGONRESPONSE().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object) From {
                            {"status", Statuses.Failure}
                        }))
                    End If

                    Dim passwordHashDb = CType(rereAccount.[Get](Account.PasswordKey, New Byte(19) {},), Byte())
                    Dim compareHash = OldAuth.CheckDoubleHashData(passwordHashDb, clientToken, serverToken)

                    If Not compareHash.SequenceEqual(passwordHash) Then
                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Account [{context.Client.GameState.Username}] logon failed password mismatch")
                        rereAccount.[Set](Account.FailedLogonsKey, (CUInt(rereAccount.[Get](Account.FailedLogonsKey, CUInt(0),))) + 1)
                        Return New SID_LOGONRESPONSE().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object) From {
                            {"status", Statuses.Failure}
                        }))
                    End If

                    Dim flags = CType(rereAccount.[Get](Account.FlagsKey, Account.Flags.None,), Account.Flags)

                    If (flags And Account.Flags.Closed) <> 0 Then
                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Account [{context.Client.GameState.Username}] logon failed account closed")
                        rereAccount.[Set](Account.FailedLogonsKey, (CUInt(rereAccount.[Get](Account.FailedLogonsKey, CUInt(0),))) + 1)
                        Return New SID_LOGONRESPONSE().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object) From {
                            {"status", Statuses.Failure}
                        }))
                    End If

                    context.Client.GameState.ActiveAccount = rereAccount
                    context.Client.GameState.FailedLogons = CUInt(rereAccount.[Get](Account.FailedLogonsKey, CUInt(0),))
                    context.Client.GameState.LastLogon = CDate(rereAccount.[Get](Account.LastLogonKey, DateTime.Now,))
                    rereAccount.[Set](Account.FailedLogonsKey, CUInt(0))
                    rereAccount.[Set](Account.IPAddressKey, context.Client.RemoteEndPoint.ToString().Split(":")(0))
                    rereAccount.[Set](Account.LastLogonKey, DateTime.Now)
                    rereAccount.[Set](Account.PortKey, context.Client.RemoteEndPoint.ToString().Split(":")(1))

                    SyncLock Battlenet.Common.ActiveAccounts
                        Dim serial = 1
                        Dim onlineName = context.Client.GameState.Username

                        While Battlenet.Common.ActiveAccounts.ContainsKey(onlineName)
                            onlineName = $"{context.Client.GameState.Username}#{System.Threading.Interlocked.Increment(serial)}"
                        End While

                        context.Client.GameState.OnlineName = onlineName
                        Battlenet.Common.ActiveAccounts.Add(onlineName, rereAccount)
                    End SyncLock

                    context.Client.GameState.Username = CStr(rereAccount.[Get](Account.UsernameKey, context.Client.GameState.Username))

                    SyncLock Battlenet.Common.ActiveGameStates
                        Battlenet.Common.ActiveGameStates.Add(context.Client.GameState.OnlineName, context.Client.GameState)
                    End SyncLock

                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"Account [{context.Client.GameState.Username}] logon success as [{context.Client.GameState.OnlineName}]")
                    Return New SID_LOGONRESPONSE().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object) From {
                        {"status", Statuses.Success}
                    }))
                Case MessageDirection.ServerToClient
                    Dim status = CUInt(CType(context.Arguments("status"), Statuses))
                    Buffer = New Byte(3) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(status)
                        End Using
                    End Using

                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes) (status: 0x{status})")
                    context.Client.Send(ToByteArray(context.Client.ProtocolType))
                    Return True
            End Select

            Return False
        End Function
    End Class
End Namespace
