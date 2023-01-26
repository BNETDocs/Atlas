Imports AtlasV.Localization
Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class WhereIsCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            Dim t = If(Arguments.Count = 0, "", Arguments(0))
            Dim r As String

            If t.ToLower() = varContext.GameState.OnlineName.ToLower() Then
                Call New WhoAmICommand(RawBuffer, Arguments).Invoke(varContext)
                Return
            End If

            Dim target As GameState = Nothing

            If Not Battlenet.Common.GetClientByOnlineName(t, target) OrElse target Is Nothing Then
                r = Resources.UserNotLoggedOn

                For Each line In r.Split(Battlenet.Common.NewLine)
                    Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, line).WriteTo(varContext.GameState.Client)
                Next

                Return
            End If

            If Object.Equals(target, varContext.GameState) = True Then
                Call New WhoAmICommand(RawBuffer, Arguments).Invoke(varContext)
                Return
            End If

            Dim ch = target.ActiveChannel
            Dim str = If(ch Is Nothing, Resources.UserIsUsingGameInRealm, Resources.UserIsUsingGameInTheChannel)

            If target.Away IsNot Nothing Then
                str += Battlenet.Common.NewLine + Resources.AwayCommandStatus.Replace("{awayMessage}", target.Away)
            End If

            Dim targetEnv = New Dictionary(Of String, String)() From {
                {"accountName", target.Username},
                {"channel", If(target.ActiveChannel Is Nothing, "(null)", target.ActiveChannel.Name)},
                {"game", Product.ProductName(target.Product, True)},
                {"host", "BNETDocs"},
                {"localTime", target.LocalTime.ToString(Common.HumanDateTimeFormat).Replace(" 0", "  ")},
                {"name", Channel.RenderOnlineName(varContext.GameState, target)},
                {"onlineName", Channel.RenderOnlineName(varContext.GameState, target)},
                {"realm", "BNETDocs"},
                {"realmTime", DateTime.Now.ToString(Common.HumanDateTimeFormat).Replace(" 0", "  ")},
                {"realmTimezone", $"UTC{DateTime.Now}"},
                {"user", Channel.RenderOnlineName(varContext.GameState, target)},
                {"username", Channel.RenderOnlineName(varContext.GameState, target)},
                {"userName", Channel.RenderOnlineName(varContext.GameState, target)}
            }
            Dim env = targetEnv.Concat(varContext.Environment)

            For Each kv In env
                str = str.Replace("{" & kv.Key & "}", kv.Value)
            Next

            Call New ChatEvent(ChatEvent.EventIds.EID_INFO, Channel.RenderChannelFlags(varContext.GameState, target), target.Ping, Channel.RenderOnlineName(varContext.GameState, target), str).WriteTo(varContext.GameState.Client)
        End Sub
    End Class
End Namespace
