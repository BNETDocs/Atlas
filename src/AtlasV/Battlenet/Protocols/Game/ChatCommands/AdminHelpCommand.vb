Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class AdminHelpCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            Dim r = String.Join(Battlenet.Common.NewLine, New List(Of String)() From {
                {Resources.AdminHelpCommand},
                {"/admin ? (alias: /admin help)"},
                {"/admin announce (alias: /admin broadcast)"},
                {"/admin broadcast <message>"},
                {"/admin channel flags <integer>"},
                {"/admin channel maxusers <max|-1>"},
                {"/admin channel rename <new name...>"},
                {"/admin channel resync"},
                {"/admin channel topic <new topic...>"},
                {"/admin dc (alias: /admin disconnect)"},
                {"/admin disconnect <user> [reason]"},
                {"/admin help (this text)"},
                {"/admin move (alias: /admin moveuser)"},
                {"/admin moveuser <user> <channel>"},
                {"/admin shutdown [(cancel [message])|(delay-seconds|30 [message])]"},
                {"/admin spoofuserflag (alias: /admin spoofuserflags)"},
                {"/admin spoofuserflags <user> <flags>"},
                {"/admin spoofusergame <user> <game>"},
                {"/admin spoofusername <oldname> <newname>"},
                {"/admin spoofuserping <user> <ping>"},
                {""}
            })

            For Each kv In varContext.Environment
                r = r.Replace("{" & kv.Key & "}", kv.Value)
            Next

            For Each line In r.Split(Battlenet.Common.NewLine)
                Call New ChatEvent(ChatEvent.EventIds.EID_INFO, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, line).WriteTo(varContext.GameState.Client)
            Next
        End Sub
    End Class
End Namespace
