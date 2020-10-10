using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game
{
    class OldAuth
    {
        public static byte[] CheckDoubleHashData(byte[] data, uint clientToken, uint serverToken)
        {
            var buf = new byte[28];
            var m = new MemoryStream(buf);
            var w = new BinaryWriter(m);

            w.Write(clientToken);
            w.Write(serverToken);
            w.Write(data);

            w.Close();
            m.Close();

            return MBNCSUtil.XSha1.CalculateHash(buf);
        }
    }
}
