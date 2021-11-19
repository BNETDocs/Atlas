Imports AtlasV.Battlenet.Protocols.Game.ChatCommands
Imports AtlasV.Daemon
Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game
    Class ChatCommand
        Public Property Arguments As List(Of String)
        Public Property RawBuffer As Byte()

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            Arguments = varArguments
            RawBuffer = varRawBuffer
        End Sub

        Public Overridable Function CanInvoke(ByVal context As ChatCommandContext) As Boolean
            Return False
        End Function

        Public Overridable Sub Invoke(ByVal context As ChatCommandContext)
            Throw New NotSupportedException("Base ChatCommand class does not Invoke()")
        End Sub

        Public Shared Function FromByteArray(ByVal text As Byte()) As ChatCommand
            Return Parse(Encoding.UTF8.GetString(text), text)
        End Function

        Public Shared Function FromString(ByVal text As String) As ChatCommand
            Return Parse(text, Encoding.UTF8.GetBytes(text))
        End Function

        Private Shared Function Parse(ByVal text As String, ByVal raw As Byte()) As ChatCommand
            Dim args = New List(Of String)(text.Split(" "c))
            Dim cmd = args(0)
            args.RemoveAt(0)
            Dim stripSize = cmd.Length + (If(text.Length - cmd.Length > 0, 1, 0))
            Dim _raw = raw.Skip(stripSize).ToArray()

            Select Case cmd
                Case "admin"
                    Return New AdminCommand(_raw, args)
                Case "away"
                    Return New AwayCommand(_raw, args)
                Case "ban"
                    Return New BanCommand(_raw, args)
                Case "clan"
                    Return New ClanCommand(_raw, args)
                Case "channel", "join", "j"
                    Return New JoinCommand(_raw, args)
                Case "designate"
                    Return New DesignateCommand(_raw, args)
                Case "emote", "me"
                    Return New EmoteCommand(_raw, args)
                Case "friends", "friend", "f"
                    Return New FriendCommand(_raw, args)
                Case "help", "?"
                    Return New HelpCommand(_raw, args)
                Case "ignore", "squelch"
                    Return New SquelchCommand(_raw, args)
                Case "kick"
                    Return New KickCommand(_raw, args)
                Case "rejoin", "rj"
                    Return New ReJoinCommand(_raw, args)
                Case "time"
                    Return New TimeCommand(_raw, args)
                Case "unban"
                    Return New UnBanCommand(_raw, args)
                Case "unignore", "unsquelch"
                    Return New UnsquelchCommand(_raw, args)
                Case "users"
                    Return New UsersCommand(_raw, args)
                Case "version", "ver"
                    Return New VersionCommand(_raw, args)
                Case "whereis", "where", "whois"
                    Return New WhereIsCommand(_raw, args)
                Case "whisper", "msg", "m", "w"
                    Return New WhisperCommand(_raw, args)
                Case "who"
                    Return New WhoCommand(_raw, args)
                Case "whoami"
                    Return New WhoAmICommand(_raw, args)
                Case Else
                    Return New InvalidCommand(_raw, args)
            End Select
        End Function
    End Class
End Namespace
