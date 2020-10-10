using System;
using System.Collections.Generic;

namespace Atlasd.Battlenet.Protocols.Game
{
    class Frame
    {
        public Queue<Message> Messages { get; protected set; }

        public Frame()
        {
            Messages = new Queue<Message>();
        }

        public Frame(Queue<Message> messages)
        {
            Messages = messages;
        }

        public byte[] ToByteArray()
        {
            var framebuf = new byte[0];
            var msgs = new Queue<Message>(Messages); // Clone Messages into local variable

            while (msgs.Count > 0)
            {
                var messagebuf = msgs.Dequeue().ToByteArray();
                var buf = new byte[framebuf.Length + messagebuf.Length];

                System.Buffer.BlockCopy(framebuf, 0, buf, 0, framebuf.Length);
                System.Buffer.BlockCopy(messagebuf, 0, buf, framebuf.Length, messagebuf.Length);

                framebuf = buf;
            }

            return framebuf;
        }
    }
}
