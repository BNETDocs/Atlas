Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_LOGONRESPONSE2
        Inherits Message

        Protected Enum Statuses As UInt32
            Success = 0
            AccountNotFound = 1
            BadPassword = 2
            AccountClosed = 6
        End Enum

        Public Sub New()
            Id = CByte(MessageIds.SID_LOGONRESPONSE2)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_LOGONRESPONSE2)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal varContext As MessageContext) As Boolean
            Dim rereAccount As Account = Nothing

            Select Case varContext.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, varContext.Client.RemoteEndPoint, $"[{Common.DirectionToString(varContext.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length < 29 Then Throw New GameProtocolViolationException(varContext.Client, $"{MessageName(Id)} buffer must be at least 29 bytes")

                    Dim clientToken, serverToken As UInt32
                    Dim passwordHash() As Byte
                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            clientToken = r.ReadUInt32()
                            serverToken = r.ReadUInt32()
                            passwordHash = r.ReadBytes(20)
                            varContext.Client.GameState.Username = r.ReadString()
                        End Using
                    End Using

                    Battlenet.Common.AccountsDb.TryGetValue(varContext.Client.GameState.Username, rereAccount)

                    If rereAccount Is Nothing Then
                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, varContext.Client.RemoteEndPoint, $"Account [{varContext.Client.GameState.Username}] does not exist")
                        Return New SID_LOGONRESPONSE2().Invoke(New MessageContext(varContext.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object) From {
                            {"status", Statuses.AccountNotFound}
                        }))
                    End If

                    Dim passwordHashDb = CType(rereAccount.[Get](Account.PasswordKey, New Byte(19) {},), Byte())
                    Dim compareHash = OldAuth.CheckDoubleHashData(passwordHashDb, clientToken, serverToken)

                    If Not compareHash.SequenceEqual(passwordHash) Then
                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, varContext.Client.RemoteEndPoint, $"Account [{varContext.Client.GameState.Username}] logon failed password mismatch")
                        rereAccount.[Set](Account.FailedLogonsKey, (CUInt(rereAccount.[Get](Account.FailedLogonsKey, CUInt(0),))) + 1)
                        Return New SID_LOGONRESPONSE2().Invoke(New MessageContext(varContext.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object) From {
                            {"status", Statuses.BadPassword}
                        }))
                    End If

                    Dim flags As Account.Flags = CType(rereAccount.[Get](Account.FlagsKey, Account.Flags.None,), Account.Flags)

                    If (flags And Account.Flags.Closed) <> 0 Then
                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, varContext.Client.RemoteEndPoint, $"Account [{varContext.Client.GameState.Username}] logon failed account closed")
                        rereAccount.[Set](Account.FailedLogonsKey, (CUInt(rereAccount.[Get](Account.FailedLogonsKey, CUInt(0),))) + 1)
                        Return New SID_LOGONRESPONSE2().Invoke(New MessageContext(varContext.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object) From {
                            {"status", Statuses.AccountClosed}
                        }))
                    End If

                    varContext.Client.GameState.ActiveAccount = rereAccount
                    varContext.Client.GameState.FailedLogons = CUInt(rereAccount.[Get](Account.FailedLogonsKey, CUInt(0),))
                    varContext.Client.GameState.LastLogon = CDate(rereAccount.[Get](Account.LastLogonKey, DateTime.Now,))
                    rereAccount.[Set](Account.FailedLogonsKey, CUInt(0))
                    rereAccount.[Set](Account.IPAddressKey, varContext.Client.RemoteEndPoint.ToString().Split(":")(0))
                    rereAccount.[Set](Account.LastLogonKey, DateTime.Now)
                    rereAccount.[Set](Account.PortKey, varContext.Client.RemoteEndPoint.ToString().Split(":")(1))

                    SyncLock Battlenet.Common.ActiveAccounts
                        Dim serial = 1
                        Dim onlineName = varContext.Client.GameState.Username

                        While Battlenet.Common.ActiveAccounts.ContainsKey(onlineName.ToLower())
                            onlineName = $"{varContext.Client.GameState.Username}#{System.Threading.Interlocked.Increment(serial)}"
                        End While

                        varContext.Client.GameState.OnlineName = onlineName
                        Battlenet.Common.ActiveAccounts.Add(onlineName.ToLower(), rereAccount)
                    End SyncLock

                    varContext.Client.GameState.Username = CStr(rereAccount.[Get](Account.UsernameKey, varContext.Client.GameState.Username))

                    SyncLock Battlenet.Common.ActiveGameStates
                        Battlenet.Common.ActiveGameStates.Add(varContext.Client.GameState.OnlineName, varContext.Client.GameState)
                    End SyncLock

                    Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, varContext.Client.RemoteEndPoint, $"Account [{varContext.Client.GameState.Username}] logon success as [{varContext.Client.GameState.OnlineName}]")
                    Return New SID_LOGONRESPONSE2().Invoke(New MessageContext(varContext.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object) From {
                        {"status", Statuses.Success}
                    }))
                Case MessageDirection.ServerToClient
                    Dim status = CUInt(CType(varContext.Arguments("status"), Statuses))
                    Dim info = CType((If(varContext.Arguments.ContainsKey("info"), varContext.Arguments("info"), Array.Empty(Of Byte)())), Byte())
                    Buffer = New Byte(4 + info.Length - 1) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(status)
                            w.Write(info)
                            If info.Length > 0 Then w.Write(CByte(0))
                        End Using
                    End Using

                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, varContext.Client.RemoteEndPoint, $"[{Common.DirectionToString(varContext.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes) (status: 0x{status})")
                    varContext.Client.Send(ToByteArray(varContext.Client.ProtocolType))
                    Return True
            End Select

            Return False
        End Function
    End Class
End Namespace
