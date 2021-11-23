Imports AtlasV.Battlenet.Protocols.Game.Messages
Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game.ChatCommands
    Class AdminSpoofUserNameCommand
        Inherits ChatCommand

        Public Sub New(ByVal varRawBuffer As Byte(), ByVal varArguments As List(Of String))
            MyBase.New(varRawBuffer, varArguments)
        End Sub

        Public Overrides Function CanInvoke(ByVal varContext As ChatCommandContext) As Boolean
            Return varContext IsNot Nothing AndAlso varContext.GameState IsNot Nothing AndAlso varContext.GameState.ActiveAccount IsNot Nothing
        End Function

        Public Overrides Sub Invoke(ByVal varContext As ChatCommandContext)
            Dim r As String = String.Empty
            Dim n1 As String = If(Arguments.Count < 1, String.Empty, Arguments(0))
            Dim n2 As String = If(Arguments.Count < 2, String.Empty, Arguments(1))
            Dim target As GameState = Nothing

            If n1.Length = 0 OrElse Not Battlenet.Common.GetClientByOnlineName(n1, target) OrElse target Is Nothing Then
                r = Resources.UserNotLoggedOn

                For Each kv In varContext.Environment
                    r = r.Replace("{" & kv.Key & "}", kv.Value)
                Next

                For Each line In r.Split(Battlenet.Common.NewLine)
                    Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, line).WriteTo(varContext.GameState.Client)
                Next

                Return
            End If

            If n2.Length = 0 OrElse (n2.Contains("#") AndAlso n2.Substring(0, n2.IndexOf("#")).Length = 0) Then
                r = Resources.AdminSpoofUserNameCommandBadValue

                For Each kv In varContext.Environment
                    r = r.Replace("{" & kv.Key & "}", kv.Value)
                Next

                For Each line In r.Split(Battlenet.Common.NewLine)
                    Call New ChatEvent(ChatEvent.EventIds.EID_ERROR, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, line).WriteTo(varContext.GameState.Client)
                Next

                Return
            End If

            Arguments.RemoveAt(1)
            Arguments.RemoveAt(0)
            RawBuffer = RawBuffer.Skip((Encoding.UTF8.GetByteCount(n1) + Encoding.UTF8.GetByteCount(n2) + (If(Arguments.Count > 0, 1, 0)) + (If(Arguments.Count > 1, 1, 0)))).ToArray()
            varContext.Environment.Add("name1", n1)
            varContext.Environment.Add("name2", n2)
            Dim oldOnlineName As String = target.OnlineName
            Dim oldFlags As Account.Flags = target.ChannelFlags
            Dim activeChannel As Channel = target.ActiveChannel

            If activeChannel IsNot Nothing Then
                activeChannel.RemoveUser(target)
            End If

            SyncLock Battlenet.Common.ActiveAccounts
                Dim searchName As String = If(n2.Contains("#") = True, n2.Substring(0, n2.IndexOf("#")), n2) '= If(n2.Contains("#") = True, n2(0..n2.IndexOf("#")), n2)
                Dim serial As Integer = 1

                If n2.Contains("#") Then
                    Dim fields = n2.Split("#")
                    Dim unused = Integer.TryParse(fields(1), serial) 'check the return
                    If serial < 1 Then serial = 1
                End If

                Dim onlineName = If(serial = 1, searchName, $"{searchName}#{serial}")

                While Battlenet.Common.ActiveAccounts.ContainsKey(onlineName.ToLower())
                    onlineName = $"{searchName}#{System.Threading.Interlocked.Increment(serial)}"
                End While

                SyncLock target.ActiveAccount
                    Battlenet.Common.ActiveAccounts.Remove(oldOnlineName.ToLower())
                    target.OnlineName = onlineName
                    Battlenet.Common.ActiveAccounts.Add(onlineName.ToLower(), target.ActiveAccount)
                End SyncLock
            End SyncLock

            SyncLock Battlenet.Common.ActiveGameStates
                Battlenet.Common.ActiveGameStates.Remove(oldOnlineName.ToLower())
                Battlenet.Common.ActiveGameStates.Add(target.OnlineName, target)
            End SyncLock

            '// send a New SID_ENTERCHAT to target
            Call New SID_ENTERCHAT().Invoke(New MessageContext(target.Client, MessageDirection.ServerToClient, New Dictionary(Of String, Object)()))

            target.ChannelFlags = oldFlags
            activeChannel.AcceptUser(target, True)

            For Each kv In varContext.Environment
                r = r.Replace("{" & kv.Key & "}", kv.Value)
            Next

            For Each line In r.Split(Battlenet.Common.NewLine)
                Call New ChatEvent(ChatEvent.EventIds.EID_INFO, varContext.GameState.ChannelFlags, varContext.GameState.Ping, varContext.GameState.OnlineName, line).WriteTo(varContext.GameState.Client)
            Next
        End Sub
    End Class
End Namespace
