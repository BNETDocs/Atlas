using System;
using System.Collections.Concurrent;

namespace Atlasd.Battlenet.Protocols.MCP
{
    class Frame
    {
        public ConcurrentQueue<Message> Messages { get; protected set; }

        public Frame()
        {
            Messages = new ConcurrentQueue<Message>();
        }

        public Frame(ConcurrentQueue<Message> messages)
        {
            Messages = messages;
        }

        public byte[] ToByteArray(ProtocolType protocolType)
        {
            var framebuf = new byte[0];
            var msgs = new ConcurrentQueue<Message>(Messages); // Clone Messages into local variable

            while (msgs.Count > 0)
            {
                if (!msgs.TryDequeue(out var msg)) break;

                var messagebuf = msg.ToByteArray(protocolType);
                var buf = new byte[framebuf.Length + messagebuf.Length];

                Buffer.BlockCopy(framebuf, 0, buf, 0, framebuf.Length);
                Buffer.BlockCopy(messagebuf, 0, buf, framebuf.Length, messagebuf.Length);

                framebuf = buf;
            }

            return framebuf;
        }
    }
}
