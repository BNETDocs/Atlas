Imports System.Collections.Generic

Namespace AtlasV.Battlenet.Protocols.Game
    Class MessageContext
        Public Property Arguments As Dictionary(Of String, Object)
        Public Property Client As ClientState
        Public Property Direction As MessageDirection

        Public Sub New(ByVal varClient As ClientState,
                       ByVal varDirection As MessageDirection,
                       ByVal Optional varArguments As Dictionary(Of String, Object) = Nothing)
            Client = varClient
            Direction = varDirection
            Arguments = varArguments
        End Sub
    End Class
End Namespace