/*
MBNCSUtil -- Managed Battle.net Authentication Library
Copyright (C) 2005-2008 by Robert Paveza

Redistribution and use in source and binary forms, with or without modification, 
are permitted provided that the following conditions are met: 

1.) Redistributions of source code must retain the above copyright notice, 
this list of conditions and the following disclaimer. 
2.) Redistributions in binary form must reproduce the above copyright notice, 
this list of conditions and the following disclaimer in the documentation 
and/or other materials provided with the distribution. 
3.) The name of the author may not be used to endorse or promote products derived 
from this software without specific prior written permission. 
	
See LICENSE.TXT that should have accompanied this software for full terms and 
conditions.

*/


using System;
using System.IO;

namespace MBNCSUtil
{
    /// <summary>
    /// Completes a <see cref="DataBuffer">DataBuffer</see> implementation with the additional
    /// data used by the BNCS protocol.
    /// </summary>
    /// <remarks>
    /// <para>When using this class with a Stream, the BncsReader only takes the next packet's data
    /// off of the stream.  An ideal example of this would be when using a <see cref="System.Net.Sockets.NetworkStream">NetworkSteam</see>
    /// to connect to Battle.net.  Incidentally, this constructor and method will block execution until new data has arrived.  Therefore,
    /// if your main receiving loop is going to use these methods, it should be on a background worker loop.</para>
    /// </remarks>
    public class BncsReader : DataReader
    {
        private int m_len = 0;
        private byte m_id;

        /// <summary>
        /// Gets the length of the data.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if this reader is not yet initialized.</exception>
        public override int Length
        {
            get
            {
                return m_len;
            }
        }

        /// <summary>
        /// Gets or sets the ID of the packet as it was specified when it was created.
        /// </summary>
        public byte PacketID { get { return m_id; } set { m_id = value; } }

        /// <summary>
        /// Creates a new data reader with the specified stream as input.
        /// </summary>
        /// <param name="str">The stream from which to read.</param>
        /// <exception cref="ArgumentNullException">Thrown if <b>str</b>
        /// is <b>null</b> (<b>Nothing</b> in Visual Basic).</exception>
        public BncsReader(Stream str)
            :
            this(str, new BinaryReader(str))
        {

        }

        #region hacked constructors
        // these constructors are hacked together.
        // because C# doesn't allow me to really modify a variable in object scope, I can't
        // use a helper method.  Instead, I'm forced to create objects one at a time and
        // link together the constructor.  Finally at the BncsReader(Stream, byte, byte, ushort)
        // constructor I get what I need done.
        private BncsReader(Stream str, BinaryReader br)
            : this(
            str, br.ReadBytes(2)[1], br.ReadUInt16()
            )
        {

        }

        private BncsReader(Stream str, byte id, ushort len)
            : base(str, (int)len - 4)
        {
            m_id = id;
            m_len = len;
        }
        #endregion

        /// <summary>
        /// Creates a new data reader with the specified byte data.
        /// </summary>
        /// <param name="data">The data to read.</param>
        /// <exception cref="ArgumentNullException">Thrown if <b>data</b> is 
        /// <b>null</b> (<b>Nothing</b> in Visual Basic).</exception>
        public BncsReader(byte[] data)
            : this(
            new MemoryStream(data, 4, data.Length - 4, false, false),
            data[1], BitConverter.ToUInt16(data, 2))
        {

        }
    }
}
