Imports AtlasV.Daemon
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class AdminCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            If Not varContext.GameState.HasAdmin() Then
                Call New InvalidCommand(RawBuffer, Arguments).Invoke(varContext)
                Return
            End If

            Dim cmd As String

            If Arguments.Count = 0 Then
                cmd = ""
            Else
                cmd = Arguments(0)
                Arguments.RemoveAt(0)
            End If

            'RawBuffer = RawBuffer((Encoding.UTF8.GetByteCount(cmd) + (If(Arguments.Count > 0, 1, 0)))..)
            RawBuffer = RawBuffer.Skip(Encoding.UTF8.GetByteCount(cmd) + (If(Arguments.Count > 0, 1, 0))).ToArray()

            Select Case cmd.ToLower()
                Case "announce", "broadcast"
                    Call New AdminBroadcastCommand(RawBuffer, Arguments).Invoke(varContext)
                    Return
                Case "channel", "chan"
                    Call New AdminChannelCommand(RawBuffer, Arguments).Invoke(varContext)
                    Return
                Case "disconnect", "dc"
                    Call New AdminDisconnectCommand(RawBuffer, Arguments).Invoke(varContext)
                    Return
                Case "help", "?"
                    Call New AdminHelpCommand(RawBuffer, Arguments).Invoke(varContext)
                    Return
                Case "moveuser", "move"
                    Call New AdminMoveUserCommand(RawBuffer, Arguments).Invoke(varContext)
                    Return
                Case "reload"
                    Call New AdminReloadCommand(RawBuffer, Arguments).Invoke(varContext)
                    Return
                Case "shutdown"
                    Call New AdminShutdownCommand(RawBuffer, Arguments).Invoke(varContext)
                    Return
                Case "spoofuserflag", "spoofuserflags"
                    Call New AdminSpoofUserFlagsCommand(RawBuffer, Arguments).Invoke(varContext)
                    Return
                Case "spoofusergame"
                    Call New AdminSpoofUserGameCommand(RawBuffer, Arguments).Invoke(varContext)
                    Return
                Case "spoofusername"
                    Call New AdminSpoofUserNameCommand(RawBuffer, Arguments).Invoke(varContext)
                    Return
                Case "spoofuserping"
                    Call New AdminSpoofUserPingCommand(RawBuffer, Arguments).Invoke(varContext)
                    Return
                Case Else
                    Dim r = Localization.Resources.InvalidAdminCommand

                    For Each kv In varContext.Environment
                        r = r.Replace("{" & kv.Key & "}", kv.Value)
                    Next

                    For Each line In r.Split(Battlenet.Common.NewLine)
                        Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, line).WriteTo(varContext.GameState.Client)
                    Next

                    Exit Select
            End Select
        End Sub
    End Class
End Namespace
