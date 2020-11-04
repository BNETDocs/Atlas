using Atlasd.Battlenet.Exceptions;
using System;
using System.Reflection;
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
                MessageIds.SID_NULL => new Messages.SID_NULL(buffer),
                MessageIds.SID_STOPADV => new Messages.SID_STOPADV(buffer),
                MessageIds.SID_CLIENTID => new Messages.SID_CLIENTID(buffer),
                MessageIds.SID_STARTVERSIONING => new Messages.SID_STARTVERSIONING(buffer),
                MessageIds.SID_REPORTVERSION => new Messages.SID_REPORTVERSION(buffer),
                MessageIds.SID_STARTADVEX => new Messages.SID_STARTADVEX(buffer),
                MessageIds.SID_GETADVLISTEX => new Messages.SID_GETADVLISTEX(buffer),
                MessageIds.SID_ENTERCHAT => new Messages.SID_ENTERCHAT(buffer),
                MessageIds.SID_GETCHANNELLIST => new Messages.SID_GETCHANNELLIST(buffer),
                MessageIds.SID_JOINCHANNEL => new Messages.SID_JOINCHANNEL(buffer),
                MessageIds.SID_CHATCOMMAND => new Messages.SID_CHATCOMMAND(buffer),
                MessageIds.SID_CHATEVENT => new Messages.SID_CHATEVENT(buffer),
                MessageIds.SID_LEAVECHAT => new Messages.SID_LEAVECHAT(buffer),
                MessageIds.SID_LOCALEINFO => new Messages.SID_LOCALEINFO(buffer),
                MessageIds.SID_FLOODDETECTED => new Messages.SID_FLOODDETECTED(buffer),
                MessageIds.SID_UDPPINGRESPONSE => new Messages.SID_UDPPINGRESPONSE(buffer),
                MessageIds.SID_CHECKAD => new Messages.SID_CHECKAD(buffer),
                MessageIds.SID_CLICKAD => new Messages.SID_CLICKAD(buffer),
                MessageIds.SID_READMEMORY => new Messages.SID_READMEMORY(buffer),
                MessageIds.SID_REGISTRY => new Messages.SID_REGISTRY(buffer),
                MessageIds.SID_MESSAGEBOX => new Messages.SID_MESSAGEBOX(buffer),
                MessageIds.SID_STARTADVEX2 => new Messages.SID_STARTADVEX2(buffer),
                MessageIds.SID_GAMEDATAADDRESS => new Messages.SID_GAMEDATAADDRESS(buffer),
                MessageIds.SID_STARTADVEX3 => new Messages.SID_STARTADVEX3(buffer),
                MessageIds.SID_LOGONCHALLENGEEX => new Messages.SID_LOGONCHALLENGEEX(buffer),
                MessageIds.SID_CLIENTID2 => new Messages.SID_CLIENTID2(buffer),
                MessageIds.SID_DISPLAYAD => new Messages.SID_DISPLAYAD(buffer),
                MessageIds.SID_NOTIFYJOIN => new Messages.SID_NOTIFYJOIN(buffer),
                MessageIds.SID_PING => new Messages.SID_PING(buffer),
                MessageIds.SID_READUSERDATA => new Messages.SID_READUSERDATA(buffer),
                MessageIds.SID_WRITEUSERDATA => new Messages.SID_WRITEUSERDATA(buffer),
                MessageIds.SID_LOGONCHALLENGE => new Messages.SID_LOGONCHALLENGE(buffer),
                MessageIds.SID_LOGONRESPONSE => new Messages.SID_LOGONRESPONSE(buffer),
                MessageIds.SID_CREATEACCOUNT => new Messages.SID_CREATEACCOUNT(buffer),
                MessageIds.SID_SYSTEMINFO => new Messages.SID_SYSTEMINFO(buffer),
                MessageIds.SID_GETICONDATA => new Messages.SID_GETICONDATA(buffer),
                MessageIds.SID_CDKEY => new Messages.SID_CDKEY(buffer),
                MessageIds.SID_CHECKDATAFILE => new Messages.SID_CHECKDATAFILE(buffer),
                MessageIds.SID_GETFILETIME => new Messages.SID_GETFILETIME(buffer),
                MessageIds.SID_CDKEY2 => new Messages.SID_CDKEY2(buffer),
                MessageIds.SID_LOGONRESPONSE2 => new Messages.SID_LOGONRESPONSE2(buffer),
                MessageIds.SID_CHECKDATAFILE2 => new Messages.SID_CHECKDATAFILE2(buffer),
                MessageIds.SID_CREATEACCOUNT2 => new Messages.SID_CREATEACCOUNT2(buffer),
                MessageIds.SID_QUERYADURL => new Messages.SID_QUERYADURL(buffer),
                MessageIds.SID_NEWS_INFO => new Messages.SID_NEWS_INFO(buffer),
                MessageIds.SID_AUTH_INFO => new Messages.SID_AUTH_INFO(buffer),
                MessageIds.SID_AUTH_CHECK => new Messages.SID_AUTH_CHECK(buffer),
                MessageIds.SID_FRIENDSLIST => new Messages.SID_FRIENDSLIST(buffer),
                /*
                MessageIds.SID_ => new Messages.SID_(buffer),
                */
                _ => null,
            };
        }

        public static string MessageName(byte messageId)
        {
            return (MessageIds)messageId switch
            {
                MessageIds.SID_NULL => "SID_NULL",
                MessageIds.SID_CLIENTID => "SID_CLIENTID",
                MessageIds.SID_STARTVERSIONING => "SID_STARTVERSIONING",
                MessageIds.SID_REPORTVERSION => "SID_REPORTVERSION",
                MessageIds.SID_STARTADVEX => "SID_STARTADVEX",
                MessageIds.SID_GETADVLISTEX => "SID_GETADVLISTEX",
                MessageIds.SID_ENTERCHAT => "SID_ENTERCHAT",
                MessageIds.SID_GETCHANNELLIST => "SID_GETCHANNELLIST",
                MessageIds.SID_JOINCHANNEL => "SID_JOINCHANNEL",
                MessageIds.SID_CHATCOMMAND => "SID_CHATCOMMAND",
                MessageIds.SID_CHATEVENT => "SID_CHATEVENT",
                MessageIds.SID_LEAVECHAT => "SID_LEAVECHAT",
                MessageIds.SID_LOCALEINFO => "SID_LOCALEINFO",
                MessageIds.SID_FLOODDETECTED => "SID_FLOODDETECTED",
                MessageIds.SID_UDPPINGRESPONSE => "SID_UDPPINGRESPONSE",
                MessageIds.SID_CHECKAD => "SID_CHECKAD",
                MessageIds.SID_CLICKAD => "SID_CLICKAD",
                MessageIds.SID_READMEMORY => "SID_READMEMORY",
                MessageIds.SID_REGISTRY => "SID_REGISTRY",
                MessageIds.SID_MESSAGEBOX => "SID_MESSAGEBOX",
                MessageIds.SID_STARTADVEX2 => "SID_STARTADVEX2",
                MessageIds.SID_GAMEDATAADDRESS => "SID_GAMEDATAADDRESS",
                MessageIds.SID_STARTADVEX3 => "SID_STARTADVEX3",
                MessageIds.SID_LOGONCHALLENGEEX => "SID_LOGONCHALLENGEEX",
                MessageIds.SID_CLIENTID2 => "SID_CLIENTID2",
                MessageIds.SID_DISPLAYAD => "SID_DISPLAYAD",
                MessageIds.SID_NOTIFYJOIN => "SID_NOTIFYJOIN",
                MessageIds.SID_PING => "SID_PING",
                MessageIds.SID_READUSERDATA => "SID_READUSERDATA",
                MessageIds.SID_WRITEUSERDATA => "SID_WRITEUSERDATA",
                MessageIds.SID_LOGONCHALLENGE => "SID_LOGONCHALLENGE",
                MessageIds.SID_LOGONRESPONSE => "SID_LOGONRESPONSE",
                MessageIds.SID_CREATEACCOUNT => "SID_CREATEACCOUNT",
                MessageIds.SID_SYSTEMINFO => "SID_SYSTEMINFO",
                MessageIds.SID_GETICONDATA => "SID_GETICONDATA",
                MessageIds.SID_CDKEY => "SID_CDKEY",
                MessageIds.SID_GETFILETIME => "SID_GETFILETIME",
                MessageIds.SID_CDKEY2 => "SID_CDKEY2",
                MessageIds.SID_LOGONRESPONSE2 => "SID_LOGONRESPONSE2",
                MessageIds.SID_CREATEACCOUNT2 => "SID_CREATEACCOUNT2",
                MessageIds.SID_QUERYADURL => "SID_QUERYADURL",
                MessageIds.SID_NEWS_INFO => "SID_NEWS_INFO",
                MessageIds.SID_AUTH_INFO => "SID_AUTH_INFO",
                MessageIds.SID_AUTH_CHECK => "SID_AUTH_CHECK",
                MessageIds.SID_FRIENDSLIST => "SID_FRIENDSLIST",
                _ => null,
            };
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
                return Encoding.UTF8.GetBytes($"{2000 + Id} {MessageName(Id).Replace("SID_", "")}");
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public abstract bool Invoke(MessageContext context);
    }
}
