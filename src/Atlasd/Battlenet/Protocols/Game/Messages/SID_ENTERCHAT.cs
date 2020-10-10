using Atlasd.Battlenet.Exceptions;
using Atlasd.Daemon;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game.Messages
{
    class SID_ENTERCHAT : Message
    {

        public SID_ENTERCHAT()
        {
            Id = (byte)MessageIds.SID_ENTERCHAT;
            Buffer = new byte[0];
        }

        public SID_ENTERCHAT(byte[] buffer)
        {
            Id = (byte)MessageIds.SID_ENTERCHAT;
            Buffer = buffer;
        }

        public override bool Invoke(MessageContext context)
        {
            switch (context.Direction)
            {
                case MessageDirection.ClientToServer:
                    {
                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_ENTERCHAT (" + (4 + Buffer.Length) + " bytes)");

                        if (Buffer.Length < 2)
                            throw new ProtocolViolationException(context.Client.ProtocolType, "SID_ENTERCHAT buffer must be at least 2 bytes");

                        /**
                         * (STRING) Username
                         * (STRING) Statstring
                         */

                        var m = new MemoryStream(Buffer);
                        var r = new BinaryReader(m);

                        context.Client.GameState.OnlineName = r.ReadString();
                        context.Client.GameState.Statstring = Encoding.ASCII.GetBytes(r.ReadString());

                        r.Close();
                        m.Close();

                        return new SID_ENTERCHAT().Invoke(new MessageContext(context.Client, MessageDirection.ServerToClient));
                    }
                case MessageDirection.ServerToClient:
                    {
                        /**
                         * (STRING) Unique name
                         * (STRING) Statstring
                         * (STRING) Account name
                         */

                        Buffer = new byte[3 + context.Client.GameState.OnlineName.Length + context.Client.GameState.Statstring.Length + context.Client.GameState.Username.Length];

                        var m = new MemoryStream(Buffer);
                        var w = new BinaryWriter(m);

                        w.Write((string)context.Client.GameState.OnlineName);
                        w.Write((string)Encoding.ASCII.GetString(context.Client.GameState.Statstring));
                        w.Write((string)context.Client.GameState.Username);

                        w.Close();
                        m.Close();

                        Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_Game, context.Client.RemoteEndPoint, "[" + Common.DirectionToString(context.Direction) + "] SID_ENTERCHAT (" + (4 + Buffer.Length) + " bytes)");
                        context.Client.Client.Client.Send(ToByteArray());
                        return true;
                    }
            }

            return false;
        }
    }
}
