Imports System
Imports System.IO

Namespace AtlasV.Battlenet.Protocols.Game
    Class OldAuth
        Public Shared Function CheckDoubleHashData(ByVal varData As Byte(),
                                                   ByVal varClientToken As UInteger,
                                                   ByVal varServerToken As UInteger) As Byte()
            Dim buf = New Byte(27) {}

            Using m = New MemoryStream(buf)
                Using w = New BinaryWriter(m)
                    w.Write(varClientToken)
                    w.Write(varServerToken)
                    w.Write(varData)
                End Using
            End Using

            Return MBNCSUtil.XSha1.CalculateHash(buf)
        End Function
    End Class
End Namespace