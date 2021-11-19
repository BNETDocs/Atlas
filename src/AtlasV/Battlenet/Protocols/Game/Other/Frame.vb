Imports System
Imports System.Collections.Concurrent

Namespace AtlasV.Battlenet.Protocols.Game
    Class Frame
        Public Property Messages As ConcurrentQueue(Of Message)

        Public Sub New()
            Messages = New ConcurrentQueue(Of Message)()
        End Sub

        Public Sub New(ByVal varMessages As ConcurrentQueue(Of Message))
            Messages = varMessages
        End Sub

        Public Function ToByteArray(ByVal varProtocolType As ProtocolType) As Byte()
            Dim framebuf = Array.Empty(Of Byte)()
            Dim msgs = New ConcurrentQueue(Of Message)(Messages)
            Dim msg = Nothing

            While Not msgs.IsEmpty '.Count > 0
                If Not msgs.TryDequeue(msg) Then Exit While
                Dim messagebuf = msg.ToByteArray(varProtocolType)
                Dim buf = New Byte(framebuf.Length + messagebuf.Length - 1) {}
                Buffer.BlockCopy(framebuf, 0, buf, 0, framebuf.Length)
                Buffer.BlockCopy(messagebuf, 0, buf, framebuf.Length, messagebuf.Length)
                framebuf = buf
            End While

            Return framebuf
        End Function
    End Class
End Namespace
