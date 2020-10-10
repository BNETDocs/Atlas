using System.IO;
using System.Text;

namespace Atlasd.Battlenet
{
    class BinaryReader : System.IO.BinaryReader
    {
        public BinaryReader(Stream input) : base(input) { }
        public BinaryReader(Stream input, Encoding encoding) : base(input, encoding) { }
        public BinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen) { }

        public override string ReadString()
        {
            string str = "";
            char chr;
            while ((int)(chr = ReadChar()) != 0)
                str += chr;
            return str;
        }
    }
}
