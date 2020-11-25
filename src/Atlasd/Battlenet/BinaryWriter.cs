using System.IO;
using System.Text;

namespace Atlasd.Battlenet
{
    class BinaryWriter : System.IO.BinaryWriter
    {
        public BinaryWriter(Stream output) : base(output, Encoding.UTF8) { }
        public BinaryWriter(Stream output, Encoding encoding) : base(output, encoding) { }
        public BinaryWriter(Stream output, Encoding encoding, bool leaveOpen) : base(output, encoding, leaveOpen) { }

        public override void Write(string value)
        {
            if (value != null) Write(Encoding.UTF8.GetBytes(value));
            Write((byte)0); // null-terminator
        }

        public void WriteByteString(byte[] value)
        {
            if (value != null) Write(value);
            Write((byte)0); // null-terminator
        }
    }
}
