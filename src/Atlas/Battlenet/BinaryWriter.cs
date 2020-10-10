using System.IO;
using System.Text;

namespace Atlas.Battlenet
{
    class BinaryWriter : System.IO.BinaryWriter
    {
        private Encoding _encoding = Encoding.ASCII;

        public BinaryWriter(Stream output) : base(output) { }
        public BinaryWriter(Stream output, Encoding encoding) : base(output, encoding) { }
        public BinaryWriter(Stream output, Encoding encoding, bool leaveOpen) : base(output, encoding, leaveOpen) { }

        public override void Write(string value)
        {
            if (value != null)
            {
                byte[] buffer = _encoding.GetBytes(value);
                Write(buffer);
            }
            Write((byte)0);
        }
    }
}
