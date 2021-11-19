Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Daemon
Imports System
Imports System.IO
Imports System.Linq

Namespace AtlasV.Battlenet.Protocols.Game.Messages
    Class SID_CDKEY
        Inherits Message

        Public Const KEYOWNER_TOOMANYSPAWNS As String = "TOO MANY SPAWNS"
        Public Const KEYOWNER_NOSPAWNING As String = "NO SPAWNING"

        Public Enum Statuses As UInt32
            Success = 1
            InvalidKey = 2
            BadProduct = 3
            Banned = 4
            InUse = 5
        End Enum

        Public Sub New()
            Id = CByte(MessageIds.SID_CDKEY)
            Buffer = Array.Empty(Of Byte)()
        End Sub

        Public Sub New(ByVal varBuffer As Byte())
            Id = CByte(MessageIds.SID_CDKEY)
            Buffer = varBuffer
        End Sub

        Public Overrides Function Invoke(ByVal context As MessageContext) As Boolean
            Select Case context.Direction
                Case MessageDirection.ClientToServer
                    Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, $"[{Common.DirectionToString(context.Direction)}] {MessageName(Id)} ({4 + Buffer.Length} bytes)")
                    If Buffer.Length < 6 Then Throw New GameProtocolViolationException(context.Client, $"{MessageName(Id)} must be at least 6 bytes")

                    Using m = New MemoryStream(Buffer)
                        Using r = New BinaryReader(m)
                            context.Client.GameState.SpawnKey = r.ReadUInt32() = 1
                            Dim unused = context.Client.GameState.GameKeys.Append(New GameKey(r.ReadString()))
                            context.Client.GameState.KeyOwner = r.ReadByteString()
                        End Using
                    End Using

                    Return New SID_CDKEY().Invoke(New MessageContext(context.Client, MessageDirection.ServerToClient))
                Case MessageDirection.ServerToClient
                    Buffer = New Byte(4) {}

                    Using m = New MemoryStream(Buffer)
                        Using w = New BinaryWriter(m)
                            w.Write(CUInt(Statuses.Success))
                            w.Write(CByte(0))
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
