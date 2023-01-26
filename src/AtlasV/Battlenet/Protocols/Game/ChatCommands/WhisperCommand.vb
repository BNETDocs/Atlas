Imports AtlasV.Localization
Imports System.Collections.Generic
Imports System.Net
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class WhisperCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            If Arguments.Count < 1 Then
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, Resources.UserNotLoggedOn).WriteTo(varContext.GameState.Client)
                Return
            End If

            Dim target = Arguments(0)
            Arguments.RemoveAt(0)
            RawBuffer = RawBuffer.Skip((Encoding.UTF8.GetByteCount(target) + (If(Arguments.Count > 0, 1, 0)))).ToArray()

            If RawBuffer.Length = 0 Then
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, Resources.WhisperCommandEmptyMessage).WriteTo(varContext.GameState.Client)
                Return
            End If

            Dim targetState As GameState = Nothing

            If Not Battlenet.Common.GetClientByOnlineName(target, targetState) OrElse targetState Is Nothing Then
                Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, Resources.UserNotLoggedOn).WriteTo(varContext.GameState.Client)
                Return
            End If

            If Not String.IsNullOrEmpty(targetState.DoNotDisturb) Then
                Dim r = Resources.WhisperCommandUserIsDoNotDisturb
                r = r.Replace("{user}", targetState.OnlineName)
                r = r.Replace("{message}", targetState.Away)
                Call New ChatEvent(ChatEvent.EventIds.EID_INFO, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, r).WriteTo(varContext.GameState.Client)
                Return
            End If

            Call New ChatEvent(ChatEvent.EventIds.EID_WHISPERTO, Channel.RenderChannelFlags(varContext.GameState, targetState), targetState.Ping, Channel.RenderOnlineName(varContext.GameState, targetState), RawBuffer).WriteTo(varContext.GameState.Client)

            If Not String.IsNullOrEmpty(targetState.Away) Then
                Dim r = Resources.WhisperCommandUserIsAway
                r = r.Replace("{user}", targetState.OnlineName)
                r = r.Replace("{message}", targetState.Away)
                Call New ChatEvent(ChatEvent.EventIds.EID_INFO, Channel.RenderChannelFlags(varContext.GameState, targetState), varContext.GameState.Ping, Channel.RenderOnlineName(varContext.GameState, targetState), r).WriteTo(varContext.GameState.Client)
            End If

            If targetState.SquelchedIPs.Contains(IPAddress.Parse(varContext.GameState.Client.RemoteEndPoint.ToString().Split(":"c)(0))) Then
                Return
            End If

            Call New ChatEvent(ChatEvent.EventIds.EID_WHISPERFROM, Channel.RenderChannelFlags(targetState, varContext.GameState), varContext.GameState.Ping, Channel.RenderOnlineName(targetState, varContext.GameState), RawBuffer).WriteTo(targetState.Client)
        End Sub
    End Class
End Namespace
