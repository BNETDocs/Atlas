Imports AtlasV.Battlenet.Protocols.Game.Messages
Imports AtlasV.Battlenet.Exceptions
Imports System
Imports System.Text

Namespace AtlasV.Battlenet.Protocols.Game
    MustInherit Class Message
        Public Id As Byte
        Public Property Buffer As Byte()

        Public Shared Function FromByteArray(ByVal varBuffer As Byte()) As Message
            If varBuffer(0) <> &HFF Then Throw New GameProtocolViolationException(Nothing, "Invalid message header")
            Dim id As Byte = varBuffer(1)
            Dim length As UInt16 = varBuffer(3) : length <<= 8 : length += varBuffer(2)
            Dim body As Byte() = New Byte(length - 4 - 1) {}
            System.Buffer.BlockCopy(varBuffer, 4, body, 0, length - 4)
            Return FromByteArray(id, body)
        End Function

        Public Shared Function FromByteArray(ByVal id As MessageIds, ByVal buffer As Byte()) As Message
            Select Case id
                Case MessageIds.SID_NULL : Return New SID_NULL(buffer)
                Case MessageIds.SID_STOPADV : Return New SID_STOPADV(buffer)
                Case MessageIds.SID_CLIENTID : Return New SID_CLIENTID(buffer)
                Case MessageIds.SID_STARTVERSIONING : Return New SID_STARTVERSIONING(buffer)
                Case MessageIds.SID_REPORTVERSION : Return New SID_REPORTVERSION(buffer)
                Case MessageIds.SID_STARTADVEX : Return New SID_STARTADVEX(buffer)
                Case MessageIds.SID_GETADVLISTEX : Return New SID_GETADVLISTEX(buffer)
                Case MessageIds.SID_ENTERCHAT : Return New SID_ENTERCHAT(buffer)
                Case MessageIds.SID_GETCHANNELLIST : Return New SID_GETCHANNELLIST(buffer)
                Case MessageIds.SID_JOINCHANNEL : Return New SID_JOINCHANNEL(buffer)
                Case MessageIds.SID_CHATCOMMAND : Return New SID_CHATCOMMAND(buffer)
                Case MessageIds.SID_CHATEVENT : Return New SID_CHATEVENT(buffer)
                Case MessageIds.SID_LEAVECHAT : Return New SID_LEAVECHAT(buffer)
                Case MessageIds.SID_LOCALEINFO : Return New SID_LOCALEINFO(buffer)
                Case MessageIds.SID_FLOODDETECTED : Return New SID_FLOODDETECTED(buffer)
                Case MessageIds.SID_UDPPINGRESPONSE : Return New SID_UDPPINGRESPONSE(buffer)
                Case MessageIds.SID_CHECKAD : Return New SID_CHECKAD(buffer)
                Case MessageIds.SID_CLICKAD : Return New SID_CLICKAD(buffer)
                Case MessageIds.SID_READMEMORY : Return New SID_READMEMORY(buffer) 'Lol guy
                Case MessageIds.SID_REGISTRY : Return New SID_REGISTRY(buffer)
                Case MessageIds.SID_MESSAGEBOX : Return New SID_MESSAGEBOX(buffer)
                Case MessageIds.SID_STARTADVEX2 : Return New SID_STARTADVEX2(buffer)
                Case MessageIds.SID_GAMEDATAADDRESS : Return New SID_GAMEDATAADDRESS(buffer)
                Case MessageIds.SID_STARTADVEX3 : Return New SID_STARTADVEX3(buffer)
                Case MessageIds.SID_LOGONCHALLENGEEX : Return New SID_LOGONCHALLENGEEX(buffer)
                Case MessageIds.SID_CLIENTID2 : Return New SID_CLIENTID2(buffer)
                Case MessageIds.SID_DISPLAYAD : Return New SID_DISPLAYAD(buffer)
                Case MessageIds.SID_NOTIFYJOIN : Return New SID_NOTIFYJOIN(buffer)
                Case MessageIds.SID_PING : Return New SID_PING(buffer)
                Case MessageIds.SID_READUSERDATA : Return New SID_READUSERDATA(buffer)
                Case MessageIds.SID_WRITEUSERDATA : Return New SID_WRITEUSERDATA(buffer)
                Case MessageIds.SID_LOGONCHALLENGE : Return New SID_LOGONCHALLENGE(buffer)
                Case MessageIds.SID_LOGONRESPONSE : Return New SID_LOGONRESPONSE(buffer)
                Case MessageIds.SID_CREATEACCOUNT : Return New SID_CREATEACCOUNT(buffer)
                Case MessageIds.SID_SYSTEMINFO : Return New SID_SYSTEMINFO(buffer)
                Case MessageIds.SID_GAMERESULT : Return New SID_GAMERESULT(buffer)
                Case MessageIds.SID_GETICONDATA : Return New SID_GETICONDATA(buffer)
                Case MessageIds.SID_CDKEY : Return New SID_CDKEY(buffer)
                Case MessageIds.SID_CHECKDATAFILE : Return New SID_CHECKDATAFILE(buffer)
                Case MessageIds.SID_GETFILETIME : Return New SID_GETFILETIME(buffer)
                Case MessageIds.SID_CDKEY2 : Return New SID_CDKEY2(buffer)
                Case MessageIds.SID_LOGONRESPONSE2 : Return New SID_LOGONRESPONSE2(buffer)
                Case MessageIds.SID_CHECKDATAFILE2 : Return New SID_CHECKDATAFILE2(buffer)
                Case MessageIds.SID_CREATEACCOUNT2 : Return New SID_CREATEACCOUNT2(buffer)
                Case MessageIds.SID_QUERYADURL : Return New SID_QUERYADURL(buffer)
                Case MessageIds.SID_WARCRAFTGENERAL : Return New SID_WARCRAFTGENERAL(buffer)
                Case MessageIds.SID_NEWS_INFO : Return New SID_NEWS_INFO(buffer)
                Case MessageIds.SID_AUTH_INFO : Return New SID_AUTH_INFO(buffer)
                Case MessageIds.SID_AUTH_CHECK : Return New SID_AUTH_CHECK(buffer)
                Case MessageIds.SID_FRIENDSLIST : Return New SID_FRIENDSLIST(buffer)
                Case MessageIds.SID_FRIENDSUPDATE : Return New SID_FRIENDSUPDATE(buffer)
                Case MessageIds.SID_FRIENDSADD : Return New SID_FRIENDSADD(buffer)
                Case MessageIds.SID_FRIENDSREMOVE : Return New SID_FRIENDSREMOVE(buffer)
                Case MessageIds.SID_FRIENDSPOSITION : Return New SID_FRIENDSPOSITION(buffer)
                Case MessageIds.SID_CLANINFO : Return New SID_CLANINFO(buffer)
                Case MessageIds.SID_CLANMEMBERLIST : Return New SID_CLANMEMBERLIST(buffer)
                Case MessageIds.SID_CLANMEMBERSTATUSCHANGE : Return New SID_CLANMEMBERSTATUSCHANGE(buffer)
                Case Else : Return Nothing
            End Select
        End Function

        Public Shared Function MessageName(varMessageId As MessageIds) As String
            Return (varMessageId).ToString()
        End Function

        Public Function ToByteArray(ByVal varProtocolType As ProtocolType) As Byte()
            If varProtocolType.IsGame() Then
                Dim size = CUShort((4 + Buffer.Length)) 'you fuckin need to properly name your vars wtf
                Dim rereBuffer = New Byte(size - 1) {}
                rereBuffer(0) = &HFF
                rereBuffer(1) = Id
                rereBuffer(2) = CByte((size))
                rereBuffer(3) = CByte(size >> 8)
                System.Buffer.BlockCopy(Buffer, 0, rereBuffer, 4, Buffer.Length)
                Return rereBuffer
            ElseIf varProtocolType.IsChat() Then
                Return Encoding.UTF8.GetBytes($"{2000 + Id} {MessageName(Id).Replace("SID_", "")}{Battlenet.Common.NewLine}")
            Else
                Throw New NotSupportedException()
            End If
        End Function

        Public MustOverride Function Invoke(ByVal context As MessageContext) As Boolean

    End Class
End Namespace