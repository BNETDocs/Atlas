Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet
    Class BinaryReader
        Inherits System.IO.BinaryReader

        Public Sub New(ByVal input As Stream)
            MyBase.New(input, Encoding.UTF8)
        End Sub

        Public Sub New(ByVal input As Stream, ByVal encoding As Encoding)
            MyBase.New(input, encoding)
        End Sub

        Public Sub New(ByVal input As Stream, ByVal encoding As Encoding, ByVal leaveOpen As Boolean)
            MyBase.New(input, encoding, leaveOpen)
        End Sub

        Public Function GetNextNull() As Long
            Dim lastPosition As Long = BaseStream.Position

            While BaseStream.CanRead

                If ReadByte() = 0 Then
                    Dim r As Long = BaseStream.Position
                    BaseStream.Position = lastPosition
                    Return r
                End If
            End While

            Return -1
        End Function

        Public Function ReadByteString() As Byte()
            Dim size = GetNextNull() - BaseStream.Position
            Return ReadBytes(size - 1)
        End Function

        Public Overrides Function ReadString() As String
            Dim str As String = ""
            Dim chr As Char = ReadChar()

            While chr <> vbNullChar
                str += chr
                chr = ReadChar()
            End While

            Return str
        End Function

    End Class

End Namespace