using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.Game
{
    class OldAuth
    {
        public static byte[] CheckDoubleHashData(byte[] data, uint clientToken, uint serverToken)
        {
            var buf = new byte[28];
            using var m = new MemoryStream(buf);
            using var w = new BinaryWriter(m);

            w.Write(clientToken);
            w.Write(serverToken);
            w.Write(data);

            return MBNCSUtil.XSha1.CalculateHash(buf);
        }
    }
}
