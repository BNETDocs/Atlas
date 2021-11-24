Imports AtlasV.Battlenet.Exceptions
Imports AtlasV.Battlenet.Protocols.Game.Messages
Imports AtlasV.Localization
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game
    Class ChatEvent
        Public Enum EventIds As UInt32
            EID_USERSHOW = &H1
            EID_USERJOIN = &H2
            EID_USERLEAVE = &H3
            EID_WHISPERFROM = &H4
            EID_TALK = &H5
            EID_BROADCAST = &H6
            EID_CHANNELJOIN = &H7
            EID_USERUPDATE = &H9
            EID_WHISPERTO = &HA
            EID_CHANNELFULL = &HD
            EID_CHANNELNOTFOUND = &HE
            EID_CHANNELRESTRICTED = &HF
            EID_INFO = &H12
            EID_ERROR = &H13
            EID_EMOTE = &H17
        End Enum

        Public Property EventId As EventIds
        Public Property Flags As UInt32
        Public Property Ping As Int32
        Public Property Username As String
        Public Property Text As Byte()

        Public Sub New(ByVal eventId As EventIds, ByVal flags As UInt32, ByVal ping As Int32, ByVal username As String, ByVal text As Byte())
            Initialize(eventId, flags, ping, username, text)
        End Sub

        Public Sub New(ByVal eventId As EventIds, ByVal flags As UInt32, ByVal ping As Int32, ByVal username As String, ByVal text As String)
            Initialize(eventId, flags, ping, username, text)
        End Sub

        Public Sub New(ByVal eventId As EventIds, ByVal flags As Account.Flags, ByVal ping As Int32, ByVal username As String, ByVal text As Byte())
            Initialize(eventId, CUInt(flags), ping, username, text)
        End Sub

        Public Sub New(ByVal eventId As EventIds, ByVal flags As Account.Flags, ByVal ping As Int32, ByVal username As String, ByVal text As String)
            Initialize(eventId, CUInt(flags), ping, username, text)
        End Sub

        Public Sub New(ByVal eventId As EventIds, ByVal flags As Channel.Flags, ByVal ping As Int32, ByVal username As String, ByVal text As Byte())
            Initialize(eventId, CUInt(flags), ping, username, text)
        End Sub

        Public Sub New(ByVal eventId As EventIds, ByVal flags As Channel.Flags, ByVal ping As Int32, ByVal username As String, ByVal text As String)
            Initialize(eventId, CUInt(flags), ping, username, text)
        End Sub

        Public Shared Function EventIdIsChatMessage(ByVal eventId As EventIds) As Boolean
            Select Case eventId
                Case EventIds.EID_WHISPERFROM,
                     EventIds.EID_TALK,
                     EventIds.EID_BROADCAST,
                     EventIds.EID_WHISPERTO,
                     EventIds.EID_INFO,
                     EventIds.EID_ERROR,
                     EventIds.EID_EMOTE
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Public Shared Function EventIdToString(ByVal eventId As EventIds) As String
            Select Case eventId
                Case EventIds.EID_USERSHOW : Return "EID_USERSHOW"
                Case EventIds.EID_USERJOIN : Return "EID_USERJOIN"
                Case EventIds.EID_USERLEAVE : Return "EID_USERLEAVE"
                Case EventIds.EID_WHISPERFROM : Return "EID_WHISPERFROM"
                Case EventIds.EID_TALK : Return "EID_TALK"
                Case EventIds.EID_BROADCAST : Return "EID_BROADCAST"
                Case EventIds.EID_CHANNELJOIN : Return "EID_CHANNELJOIN"
                Case EventIds.EID_USERUPDATE : Return "EID_USERUPDATE"
                Case EventIds.EID_WHISPERTO : Return "EID_WHISPERTO"
                Case EventIds.EID_CHANNELFULL : Return "EID_CHANNELFULL"
                Case EventIds.EID_CHANNELNOTFOUND : Return "EID_CHANNELNOTFOUND"
                Case EventIds.EID_CHANNELRESTRICTED : Return "EID_CHANNELRESTRICTED"
                Case EventIds.EID_INFO : Return "EID_INFO"
                Case EventIds.EID_ERROR : Return "EID_ERROR"
                Case EventIds.EID_EMOTE : Return "EID_EMOTE"
                Case Else
                    Throw New ArgumentOutOfRangeException(String.Format("Unknown Event Id [0x{0:X8}]", eventId))
            End Select
        End Function

        Protected Sub Initialize(ByVal varEventId As EventIds,
                                 ByVal varFlags As UInt32,
                                 ByVal varPing As Int32,
                                 ByVal varUsername As String,
                                 ByVal varText As Byte())
            EventId = varEventId
            Flags = varFlags
            Ping = varPing
            Username = varUsername
            Text = ProfanityFilter.FilterMessage(varText)
        End Sub

        Protected Sub Initialize(ByVal varEventId As EventIds,
                                 ByVal varFlags As UInt32,
                                 ByVal varPing As Int32,
                                 ByVal varUsername As String,
                                 ByVal varText As String)
            Initialize(varEventId, varFlags, varPing, varUsername, ProfanityFilter.FilterMessage(varText)) 'Encoding.UTF8.GetBytes(varText))
        End Sub

        Public Function ToByteArray(ByVal varProtocolType As ProtocolType.Types) As Byte()
            Select Case varProtocolType
                Case ProtocolType.Types.Game
                    Dim buf = New Byte(26 + Encoding.UTF8.GetByteCount(Username) + Text.Length - 1) {}

                    Using m = New MemoryStream(buf)
                        Using w = New BinaryWriter(m)
                            w.Write(CType(EventId, UInt32))
                            w.Write(CType(Flags, UInt32))
                            w.Write(CType(Ping, Int32))
                            w.Write(CType(0, UInt32))
                            w.Write(CType(&HBAADF00DUI, UInt32))
                            w.Write(CType(&HBAADF00DUI, UInt32))
                            w.Write(CStr(Username))
                            w.WriteByteString(Text)
                        End Using
                    End Using
                    Return buf
                Case ProtocolType.Types.Chat, ProtocolType.Types.Chat_Alt1, ProtocolType.Types.Chat_Alt2
                    Dim buf = $"{1000 + EventId} "
                    Dim varProduct = New Byte(3) {}
                    Buffer.BlockCopy(Text, 0, varProduct, 0, Math.Min(4, Text.Length))
                    ' Telnet has these in the opposite direction, good attempt though you get an A for effort.
                    If varProduct(0) <> &H0 Then
                        Array.Reverse(varProduct, 0, varProduct.Length)
                    End If
                    ' Opportunities to be had here
                    '      Extra data can be applied after the string
                    Select Case EventId
                        Case EventIds.EID_USERSHOW, EventIds.EID_USERUPDATE
                            buf += $"USER {Username} {Flags} [{Encoding.UTF8.GetString(varProduct)}]"
                            Exit Select
                        Case EventIds.EID_USERJOIN
                            buf += $"JOIN {Username} {Flags} [{Encoding.UTF8.GetString(varProduct)}]"
                            Exit Select
                        Case EventIds.EID_USERLEAVE
                            buf += $"LEAVE {Username} {Flags}"
                            Exit Select
                        Case EventIds.EID_WHISPERFROM, EventIds.EID_WHISPERTO
                            buf += $"WHISPER {Username} {Flags} ""{Encoding.UTF8.GetString(Text)}"""
                            Exit Select
                        Case EventIds.EID_TALK
                            buf += $"TALK {Username} {Flags} ""{Encoding.UTF8.GetString(Text)}"""
                            Exit Select
                        Case EventIds.EID_BROADCAST
                            buf += $"BROADCAST ""{Encoding.UTF8.GetString(Text)}"""
                            Exit Select
                        Case EventIds.EID_CHANNELJOIN
                            buf += $"CHANNEL ""{Encoding.UTF8.GetString(Text)}"""
                            Exit Select
                        Case EventIds.EID_INFO
                            buf += $"INFO ""{Encoding.UTF8.GetString(Text)}"""
                            Exit Select
                        Case EventIds.EID_ERROR
                            buf += $"ERROR ""{Encoding.UTF8.GetString(Text)}"""
                            Exit Select
                        Case EventIds.EID_EMOTE
                            buf += $"EMOTE {Username} {Flags} ""{Encoding.UTF8.GetString(Text)}"""
                            Exit Select
                        Case Else
                            buf += $"UNKNOWN {Username} {Flags} ""{Encoding.UTF8.GetString(Text)}"""
                            Exit Select
                    End Select

                    buf += Battlenet.Common.NewLine
                    Return Encoding.UTF8.GetBytes(buf)
                Case Else
                    Throw New ProtocolNotSupportedException(varProtocolType, Nothing, $"Unsupported protocol type [0x{CByte(varProtocolType)}]")
            End Select
        End Function

        Public Sub WriteTo(ByVal receiver As ClientState)
            WriteTo(Me, receiver)
        End Sub

        Public Shared Sub WriteTo(ByVal chatEvent As ChatEvent, ByVal receiver As ClientState)
            Dim args = New Dictionary(Of String, Object) From {
                {"chatEvent", chatEvent}
            }
            Dim msg = New SID_CHATEVENT()
            msg.Invoke(New MessageContext(receiver, MessageDirection.ServerToClient, args))
            receiver.Send(msg.ToByteArray(receiver.ProtocolType))
        End Sub
    End Class
End Namespace
