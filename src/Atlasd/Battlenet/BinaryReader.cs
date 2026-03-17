using Atlasd.Battlenet.Exceptions;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet
{
    class BinaryReader : System.IO.BinaryReader
    {
        private readonly object _lock = new object();

        public BinaryReader(Stream input) : base(input, Encoding.UTF8) { }
        public BinaryReader(Stream input, Encoding encoding) : base(input, encoding) { }
        public BinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen) { }

        public long GetNextNull()
        {
            lock (_lock)
            {
                long lastPosition = BaseStream.Position;

                while (BaseStream.Position < BaseStream.Length)
                {
                    if (ReadByte() == 0)
                    {
                        long r = BaseStream.Position;
                        BaseStream.Position = lastPosition;
                        return r;
                    }
                }

                BaseStream.Position = lastPosition;
                return -1;
            }
        }

        public byte[] ReadByteString()
        {
            lock (_lock)
            {
                var nullPos = GetNextNull();
                if (nullPos < 0)
                {
                    throw new GameProtocolViolationException(null,
                        $"Truncated string field at stream position {BaseStream.Position}: missing null terminator");
                }
                var size = nullPos - BaseStream.Position;
                return ReadBytes((int)size)[..^1];
            }
        }

        public override string ReadString()
        {
            lock (_lock)
            {
                string str = "";
                char chr;
                while (BaseStream.Position < BaseStream.Length && (int)(chr = ReadChar()) != 0)
                    str += chr;
                return str;
            }
        }
    }
}
