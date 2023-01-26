Imports System.Collections.Generic

Namespace AtlasV.Battlenet.Protocols.Game
    Class ChatCommandContext
        Public Property Command As ChatCommand
        Public Property Environment As Dictionary(Of String, String)
        Public Property GameState As GameState

        Public Sub New(ByVal varCommand As ChatCommand,
                       ByVal varEnvironment As Dictionary(Of String, String),
                       ByVal varGameState As GameState)
            Command = varCommand
            Environment = varEnvironment
            GameState = varGameState
        End Sub
    End Class
End Namespace