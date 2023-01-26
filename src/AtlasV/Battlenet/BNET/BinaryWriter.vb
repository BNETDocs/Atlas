Imports System.IO
Imports System.Text

Namespace AtlasV.Battlenet
    Class BinaryWriter
        Inherits System.IO.BinaryWriter

        Public Sub New(ByVal output As Stream)
            MyBase.New(output, Encoding.UTF8)
        End Sub

        Public Sub New(ByVal output As Stream, ByVal encoding As Encoding)
            MyBase.New(output, encoding)
        End Sub

        Public Sub New(ByVal output As Stream, ByVal encoding As Encoding, ByVal leaveOpen As Boolean)
            MyBase.New(output, encoding, leaveOpen)
        End Sub

        Public Overrides Sub Write(ByVal value As String)
            If value IsNot Nothing Then Write(Encoding.UTF8.GetBytes(value))
            Write(CByte(0))
        End Sub

        Public Sub WriteByteString(ByVal value As Byte())
            If value IsNot Nothing Then Write(value)
            Write(CByte(0))
        End Sub
    End Class
End Namespace