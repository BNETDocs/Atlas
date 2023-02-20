using Atlasd.Battlenet.Protocols.Game.Messages;
using Atlasd.Battlenet.Exceptions;
using System;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game
{
    abstract class Message
    {
        public byte Id;
        public byte[] Buffer { get; protected set; }

        public static Message FromByteArray(byte[] buffer)
        {
            if (buffer[0] != 0xFF)
                throw new GameProtocolViolationException(null, "Invalid message header");

            byte id = buffer[1];
            UInt16 length = (UInt16)((buffer[3] << 8) + buffer[2]);
            byte[] body = new byte[length - 4];

            System.Buffer.BlockCopy(buffer, 4, body, 0, length - 4);

            return FromByteArray(id, body);
        }

        public static Message FromByteArray(byte id, byte[] buffer)
        {
            return ((MessageIds)id) switch
            {
                MessageIds.SID_NULL => new SID_NULL(buffer),
                MessageIds.SID_STOPADV => new SID_STOPADV(buffer),
                MessageIds.SID_CLIENTID => new SID_CLIENTID(buffer),
                MessageIds.SID_STARTVERSIONING => new SID_STARTVERSIONING(buffer),
                MessageIds.SID_REPORTVERSION => new SID_REPORTVERSION(buffer),
                MessageIds.SID_STARTADVEX => new SID_STARTADVEX(buffer),
                MessageIds.SID_GETADVLISTEX => new SID_GETADVLISTEX(buffer),
                MessageIds.SID_ENTERCHAT => new SID_ENTERCHAT(buffer),
                MessageIds.SID_GETCHANNELLIST => new SID_GETCHANNELLIST(buffer),
                MessageIds.SID_JOINCHANNEL => new SID_JOINCHANNEL(buffer),
                MessageIds.SID_CHATCOMMAND => new SID_CHATCOMMAND(buffer),
                MessageIds.SID_CHATEVENT => new SID_CHATEVENT(buffer),
                MessageIds.SID_LEAVECHAT => new SID_LEAVECHAT(buffer),
                MessageIds.SID_LOCALEINFO => new SID_LOCALEINFO(buffer),
                MessageIds.SID_FLOODDETECTED => new SID_FLOODDETECTED(buffer),
                MessageIds.SID_UDPPINGRESPONSE => new SID_UDPPINGRESPONSE(buffer),
                MessageIds.SID_CHECKAD => new SID_CHECKAD(buffer),
                MessageIds.SID_CLICKAD => new SID_CLICKAD(buffer),
                MessageIds.SID_READMEMORY => new SID_READMEMORY(buffer),
                MessageIds.SID_REGISTRY => new SID_REGISTRY(buffer),
                MessageIds.SID_MESSAGEBOX => new SID_MESSAGEBOX(buffer),
                MessageIds.SID_STARTADVEX2 => new SID_STARTADVEX2(buffer),
                MessageIds.SID_GAMEDATAADDRESS => new SID_GAMEDATAADDRESS(buffer),
                MessageIds.SID_STARTADVEX3 => new SID_STARTADVEX3(buffer),
                MessageIds.SID_LOGONCHALLENGEEX => new SID_LOGONCHALLENGEEX(buffer),
                MessageIds.SID_CLIENTID2 => new SID_CLIENTID2(buffer),
                MessageIds.SID_LEAVEGAME => new SID_LEAVEGAME(buffer),
                MessageIds.SID_DISPLAYAD => new SID_DISPLAYAD(buffer),
                MessageIds.SID_NOTIFYJOIN => new SID_NOTIFYJOIN(buffer),
                MessageIds.SID_PING => new SID_PING(buffer),
                MessageIds.SID_READUSERDATA => new SID_READUSERDATA(buffer),
                MessageIds.SID_WRITEUSERDATA => new SID_WRITEUSERDATA(buffer),
                MessageIds.SID_LOGONCHALLENGE => new SID_LOGONCHALLENGE(buffer),
                MessageIds.SID_LOGONRESPONSE => new SID_LOGONRESPONSE(buffer),
                MessageIds.SID_CREATEACCOUNT => new SID_CREATEACCOUNT(buffer),
                MessageIds.SID_SYSTEMINFO => new SID_SYSTEMINFO(buffer),
                MessageIds.SID_GAMERESULT => new SID_GAMERESULT(buffer),
                MessageIds.SID_GETICONDATA => new SID_GETICONDATA(buffer),
                MessageIds.SID_CDKEY => new SID_CDKEY(buffer),
                MessageIds.SID_CHANGEPASSWORD => new SID_CHANGEPASSWORD(buffer),
                MessageIds.SID_CHECKDATAFILE => new SID_CHECKDATAFILE(buffer),
                MessageIds.SID_GETFILETIME => new SID_GETFILETIME(buffer),
                MessageIds.SID_QUERYREALMS => new SID_QUERYREALMS(buffer),
                MessageIds.SID_CDKEY2 => new SID_CDKEY2(buffer),
                MessageIds.SID_LOGONRESPONSE2 => new SID_LOGONRESPONSE2(buffer),
                MessageIds.SID_CHECKDATAFILE2 => new SID_CHECKDATAFILE2(buffer),
                MessageIds.SID_CREATEACCOUNT2 => new SID_CREATEACCOUNT2(buffer),
                MessageIds.SID_LOGONREALMEX => new SID_LOGONREALMEX(buffer),
                MessageIds.SID_QUERYREALMS2 => new SID_QUERYREALMS2(buffer),
                MessageIds.SID_QUERYADURL => new SID_QUERYADURL(buffer),
                MessageIds.SID_WARCRAFTGENERAL => new SID_WARCRAFTGENERAL(buffer),
                MessageIds.SID_NEWS_INFO => new SID_NEWS_INFO(buffer),
                MessageIds.SID_AUTH_INFO => new SID_AUTH_INFO(buffer),
                MessageIds.SID_AUTH_CHECK => new SID_AUTH_CHECK(buffer),
                MessageIds.SID_SETEMAIL => new SID_SETEMAIL(buffer),
                MessageIds.SID_FRIENDSLIST => new SID_FRIENDSLIST(buffer),
                MessageIds.SID_FRIENDSUPDATE => new SID_FRIENDSUPDATE(buffer),
                MessageIds.SID_FRIENDSADD => new SID_FRIENDSADD(buffer),
                MessageIds.SID_FRIENDSREMOVE => new SID_FRIENDSREMOVE(buffer),
                MessageIds.SID_FRIENDSPOSITION => new SID_FRIENDSPOSITION(buffer),
                MessageIds.SID_CLANINFO => new SID_CLANINFO(buffer),
                MessageIds.SID_CLANMEMBERLIST => new SID_CLANMEMBERLIST(buffer),
                MessageIds.SID_CLANMEMBERSTATUSCHANGE => new SID_CLANMEMBERSTATUSCHANGE(buffer),
                /*
                MessageIds.SID_ => new SID_(buffer),
                */
                _ => null,
            };
        }

        public static string MessageName(byte messageId)
        {
            return ((MessageIds)messageId).ToString();
        }

        public byte[] ToByteArray(ProtocolType protocolType)
        {
            if (protocolType.IsGame())
            {
                var size = (UInt16)(4 + Buffer.Length);
                var buffer = new byte[size];

                buffer[0] = 0xFF;
                buffer[1] = Id;
                buffer[2] = (byte)(size);
                buffer[3] = (byte)(size >> 8);

                System.Buffer.BlockCopy(Buffer, 0, buffer, 4, Buffer.Length);

                return buffer;
            }
            else if (protocolType.IsChat())
            {
                return Encoding.UTF8.GetBytes($"{2000 + Id} {MessageName(Id).Replace("SID_", "")}{Battlenet.Common.NewLine}");
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public abstract bool Invoke(MessageContext context);
    }
}
