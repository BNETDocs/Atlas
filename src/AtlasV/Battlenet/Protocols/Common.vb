

Namespace AtlasV.Battlenet.Protocols
    Class Common
        Public Const HumanDateTimeFormat As String = "ddd MMM dd hh:mm tt"

        Public Shared Function DirectionToString(ByVal varDirection As MessageDirection) As String
            Select Case varDirection
                Case MessageDirection.ClientToServer : Return "C>S"
                Case MessageDirection.ServerToClient : Return "S>C"
                Case MessageDirection.PeerToPeer : Return "P2P"
                Case Else : Return "???"
            End Select
        End Function
    End Class
End Namespace
