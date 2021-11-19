Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class AdminMoveUserCommand
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
            Dim target As GameState = Nothing

            If t.Length = 0 OrElse Not Battlenet.Common.GetClientByOnlineName(t, target) OrElse target Is Nothing Then
                r = Resources.UserNotLoggedOn

                For Each line In r.Split(Battlenet.Common.NewLine)
                    Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, line).WriteTo(varContext.GameState.Client)
                Next

                Return
            End If

            Arguments.RemoveAt(0)
            'RawBuffer = RawBuffer((Encoding.UTF8.GetByteCount(t) + (If(Arguments.Count > 0, 1, 0)))..)
            RawBuffer = RawBuffer.Skip(Encoding.UTF8.GetByteCount(t) + (If(Arguments.Count > 0, 1, 0))).ToArray()

            If target.ActiveChannel Is Nothing Then
                r = Resources.UserNotInChannel

                For Each line In r.Split(Battlenet.Common.NewLine)
                    Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, line).WriteTo(varContext.GameState.Client)
                Next

                Return
            End If

            Channel.MoveUser(target, String.Join(" ", Arguments), True)
            Dim targetEnv = New Dictionary(Of String, String)() From {
                {"accountName", target.Username},
                {"channel", If(target.ActiveChannel Is Nothing, "(null)", target.ActiveChannel.Name)},
                {"game", Product.ProductName(target.Product, True)},
                {"host", "BNETDocs"},
                {"localTime", target.LocalTime.ToString(Common.HumanDateTimeFormat).Replace(" 0", "  ")},
                {"name", target.OnlineName},
                {"onlineName", target.OnlineName},
                {"realm", "BNETDocs"},
                {"realmTime", DateTime.Now.ToString(Common.HumanDateTimeFormat).Replace(" 0", "  ")},
                {"realmTimezone", $"UTC{DateTime.Now}"},
                {"user", target.OnlineName},
                {"username", target.OnlineName},
                {"userName", target.OnlineName}
            }
            Dim env = targetEnv.Concat(varContext.Environment)
            r = Resources.AdminMoveUserCommand

            For Each kv In env
                r = r.Replace("{" & kv.Key & "}", kv.Value)
            Next

            For Each line In r.Split(Battlenet.Common.NewLine)
                Call New ChatEvent(ChatEvent.EventIds.EID_INFO, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, line).WriteTo(varContext.GameState.Client)
            Next
        End Sub
    End Class
End Namespace
