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
using System.Text;

namespace MBNCSUtil
{
    /// <summary>
    /// Operates as a buffered data reader for network and file input.
    /// </summary>
    /// <remarks>
    /// <para>This class does not write data in any manner; for writing or sending data, 
    /// use the <see cref="DataBuffer">DataBuffer</see> or derived classes.</para>
    /// <para>This class always uses little-endian byte ordering.</para>
    /// </remarks>
    public class DataReader
    {
        private byte[] m_data;
        private int m_index;

        /// <summary>
        /// Gets a copy of the data used by the current instance.  When overridden in a 
        /// derived class, allows this class to access an alternative data source.
        /// </summary>
        [Obsolete("This property is deprecated and should no longer be used.  To access the underlying data you should use the UnderlyingBuffer property.", true)]
        // Suppressing CA1819: It does not make sense to make this property a method or a collection.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        protected virtual byte[] Data
        {
            get
            {
                byte[] dataCopy = new byte[m_data.Length];
                Buffer.BlockCopy(m_data, 0, dataCopy, 0, dataCopy.Length);
                return dataCopy;
            }
        }

        /// <summary>
        /// Gets a reference to the underlying buffer.
        /// </summary>
        protected byte[] UnderlyingBuffer
        {
            get { return m_data; }
        }

        #region ctors
        /// <summary>
        /// Creates a new data reader with the specified stream as input.
        /// </summary>
        /// <remarks>
        /// <para>This constructor will block until a full packet has been returned.</para>
        /// </remarks>
        /// <param name="str">The stream from which to read.</param>
        /// <param name="length">The length of the data to read from the stream.</param>
        public DataReader(Stream str, int length)
        {
            if (str == null)
                throw new ArgumentNullException(Resources.param_str, Resources.streamNull);

            int moreDataLen = length;
            int curIncIndex = 0;
            m_data = new byte[moreDataLen];
            while (moreDataLen > 0)
            {
                int tmpLen = str.Read(m_data, curIncIndex, moreDataLen);
                curIncIndex += tmpLen;
                moreDataLen -= tmpLen;
            }
        }

        /// <summary>
        /// Creates a new data reader with the specified byte data.
        /// </summary>
        /// <param name="data">The data to read.</param>
        public DataReader(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(Resources.param_data, Resources.dataNull);

            m_data = data;
        }
        #endregion

        #region DataReader Members
        /// <summary>
        /// Reads a boolean value from the data stream.
        /// </summary>
        /// <remarks>
        /// <para>This method interprets a 32-bit value from the stream as false if it is zero and true if it is nonzero.</para>
        /// </remarks>
        /// <returns>The next boolean value from the data stream.</returns>
        public bool ReadBoolean()
        {
            return BitConverter.ToInt32(m_data, m_index) != 0;
        }

        /// <summary>
        /// Reads a byte value from the data stream.
        /// </summary>
        /// <returns>The next byte from the data stream.</returns>
        public byte ReadByte()
        {
            return m_data[m_index++];
        }

        /// <summary>
        /// Reads a byte array from the data stream.
        /// </summary>
        /// <param name="expectedItems">The number of bytes to read from the stream.</param>
        /// <returns>The next <i>expectedItems</i> bytes from the stream.</returns>
        public byte[] ReadByteArray(int expectedItems)
        {
            byte[] data = new byte[expectedItems];
            Buffer.BlockCopy(m_data, m_index, data, 0, expectedItems);
            m_index += expectedItems;
            return data;
        }

        /// <summary>
        /// Reads a null-terminated byte array from the data stream.
        /// </summary>
        /// <remarks>
        /// <para>The return value includes the null terminator.</para>
        /// </remarks>
        /// <returns>The next byte array in the stream, terminated by a value of 0.</returns>
        public byte[] ReadNullTerminatedByteArray()
        {
            int i = m_index;

            while ((i < m_data.Length) && (m_data[i] != 0))
                i++;

            byte[] bytes = new byte[i - m_index];
            Buffer.BlockCopy(m_data, m_index, bytes, 0, bytes.Length);

            m_index = ++i;

            return bytes;
        }

        /// <summary>
        /// Reads a signed 16-bit value from the data stream.
        /// </summary>
        /// <returns>The next 16-bit value from the data stream.</returns>
        public short ReadInt16()
        {
            short s = BitConverter.ToInt16(m_data, m_index);
            m_index += 2;
            return s;
        }

        /// <summary>
        /// Reads an array of signed 16-bit values from the data stream.
        /// </summary>
        /// <param name="expectedItems">The number of 16-bit values to read from the stream.</param>
        /// <returns>The next <i>expectedItems</i> 16-bit values from the stream.</returns>
        public short[] ReadInt16Array(int expectedItems)
        {
            short[] data = new short[expectedItems];
            Buffer.BlockCopy(m_data, m_index, data, 0, expectedItems * 2);
            m_index += (expectedItems * 2);
            return data;
        }

        /// <summary>
        /// Reads an unsigned 16-bit value from the data stream.
        /// </summary>
        /// <remarks>
        /// <para>This method is not CLS-compliant.</para>
        /// </remarks>
        /// <returns>The next 16-bit value from the data stream.</returns>
        [CLSCompliant(false)]
        public ushort ReadUInt16()
        {
            ushort s = BitConverter.ToUInt16(m_data, m_index);
            m_index += 2;
            return s;
        }

        /// <summary>
        /// Reads an array of unsigned 16-bit values from the data stream.
        /// </summary>
        /// <remarks>
        /// <para>This method is not CLS-compliant.</para>
        /// </remarks>
        /// <param name="expectedItems">The number of 16-bit values to read from the stream.</param>
        /// <returns>The next <i>expectedItems</i> 16-bit values from the stream.</returns>
        [CLSCompliant(false)]
        public ushort[] ReadUInt16Array(int expectedItems)
        {
            ushort[] data = new ushort[expectedItems];
            Buffer.BlockCopy(m_data, m_index, data, 0, expectedItems * 2);
            m_index += (expectedItems * 2);
            return data;
        }

        /// <summary>
        /// Reads a signed 32-bit value from the data stream.
        /// </summary>
        /// <returns>The next 32-bit value from the data stream.</returns>
        public int ReadInt32()
        {
            int i = BitConverter.ToInt32(m_data, m_index);
            m_index += 4;
            return i;
        }

        /// <summary>
        /// Reads an array of signed 32-bit values from the data stream.
        /// </summary>
        /// <param name="expectedItems">The number of 32-bit values to read from the stream.</param>
        /// <returns>The next <i>expectedItems</i> 32-bit values from the stream.</returns>
        public int[] ReadInt32Array(int expectedItems)
        {
            int[] data = new int[expectedItems];
            Buffer.BlockCopy(m_data, m_index, data, 0, expectedItems * 4);
            m_index += (expectedItems * 4);
            return data;
        }

        /// <summary>
        /// Reads an unsigned 32-bit value from the data stream.
        /// </summary>
        /// <remarks>
        /// <para>This method is not CLS-compliant.</para>
        /// </remarks>
        /// <returns>The next 32-bit value from the data stream.</returns>
        [CLSCompliant(false)]
        public uint ReadUInt32()
        {
            uint i = BitConverter.ToUInt32(m_data, m_index);
            m_index += 4;
            return i;
        }

        /// <summary>
        /// Reads an array of signed 32-bit values from the data stream.
        /// </summary>
        /// <remarks>
        /// <para>This method is not CLS-compliant.</para>
        /// </remarks>
        /// <param name="expectedItems">The number of 32-bit values to read from the stream.</param>
        /// <returns>The next <i>expectedItems</i> 32-bit values from the stream.</returns>
        [CLSCompliant(false)]
        public uint[] ReadUInt32Array(int expectedItems)
        {
            uint[] data = new uint[expectedItems];
            Buffer.BlockCopy(m_data, m_index, data, 0, expectedItems * 4);
            m_index += (expectedItems * 4);
            return data;
        }

        /// <summary>
        /// Reads a signed 64-bit value from the data stream.
        /// </summary>
        /// <returns>The next 64-bit value from the data stream.</returns>
        public long ReadInt64()
        {
            long l = BitConverter.ToInt64(m_data, m_index);
            m_index += 8;
            return l;
        }

        /// <summary>
        /// Reads an array of signed 64-bit values from the data stream.
        /// </summary>
        /// <param name="expectedItems">The number of 64-bit values to read from the stream.</param>
        /// <returns>The next <i>expectedItems</i> 64-bit values from the stream.</returns>
        public long[] ReadInt64Array(int expectedItems)
        {
            long[] data = new long[expectedItems];
            Buffer.BlockCopy(m_data, m_index, data, 0, expectedItems * 8);
            m_index += (expectedItems * 8);
            return data;
        }

        /// <summary>
        /// Reads an unsigned 64-bit value from the data stream.
        /// </summary>
        /// <remarks>
        /// <para>This method is not CLS-compliant.</para>
        /// </remarks>
        /// <returns>The next 64-bit value from the data stream.</returns>
        [CLSCompliant(false)]
        public ulong ReadUInt64()
        {
            ulong l = BitConverter.ToUInt64(m_data, m_index);
            m_index += 8;
            return l;
        }

        /// <summary>
        /// Reads an array of unsigned 64-bit values from the data stream.
        /// </summary>
        /// <remarks>
        /// <para>This method is not CLS-compliant.</para>
        /// </remarks>
        /// <param name="expectedItems">The number of 64-bit values to read from the stream.</param>
        /// <returns>The next <i>expectedItems</i> 64-bit values from the stream.</returns>
        [CLSCompliant(false)]
        public ulong[] ReadUInt64Array(int expectedItems)
        {
            ulong[] data = new ulong[expectedItems];
            Buffer.BlockCopy(m_data, m_index, data, 0, expectedItems * 8);
            m_index += (expectedItems * 8);
            return data;
        }
        /// <summary>
        /// Reads the next byte in the stream but does not consume it.
        /// </summary>
        /// <returns>A byte value (0-255) if the call succeeded, or else -1 if reading past the end of the stream.</returns>
        public int Peek()
        {
            if (m_index >= m_data.Length)
                return -1;
            return m_data[m_index];
        }

        /// <summary>
        /// Peeks at the next possible four-byte string with the specified byte padding without advancing the index.
        /// </summary>
        /// <param name="padding">The byte used to pad the string to total four bytes.</param>
        /// <returns>The next 4-byte string, reversed, from the stream.</returns>
        public string PeekDwordString(byte padding)
        {
            int length = m_data.Length - m_index;
            if (length > 4)
                length = 4;

            byte[] b = new byte[length];
            int idx0 = -1;
            for (int i = m_index, j = length - 1; i < (m_index + length) && j >= 0; i++, j--)
            {
                b[j] = m_data[i];
                if (b[j] == padding)
                    idx0 = j;
            }
            if (idx0 == -1)
                idx0 = length;

            string result = Encoding.ASCII.GetString(b, 0, idx0);
            return result;
        }

        /// <summary>
        /// Reads the next possible four-byte string with the specified byte padding.
        /// </summary>
        /// <param name="padding">The byte used to pad the string to total four bytes.</param>
        /// <returns>The next 4-byte string, reversed, from the stream.</returns>
        public string ReadDwordString(byte padding)
        {
            string str = PeekDwordString(padding);
            m_index += 4;
            return str;
        }

        /// <summary>
        /// Reads the next C-style ASCII null-terminated string from the stream.
        /// </summary>
        /// <returns>The next C-style string.</returns>
        public string ReadCString()
        {
            return ReadCString(Encoding.ASCII);
        }

        /// <summary>
        /// Reads the next C-style null-terminated string from the stream.
        /// </summary>
        /// <param name="enc">The encoding used for the string.</param>
        /// <returns>The next C-style string encoded with the specified encoding.</returns>
        public string ReadCString(Encoding enc)
        {
            return ReadTerminatedString('\0', enc);
        }

        /// <summary>
        /// Reads the next pascal-style ASCII string from the stream.
        /// </summary>
        /// <returns>The next pascal-style string.</returns>
        public string ReadPascalString()
        {
            return ReadPascalString(Encoding.ASCII);
        }

        /// <summary>
        /// Reads the next pascal-style string from the stream.
        /// </summary>
        /// <param name="enc">The encoding used for the string.</param>
        /// <returns>The next pascal-style string encoded with the specified encoding.</returns>
        public string ReadPascalString(Encoding enc)
        {
            int len = ReadByte();
            string s = enc.GetString(m_data, m_index, len);
            m_index += enc.GetByteCount(s);
            return s;
        }

        /// <summary>
        /// Reads the next wide-pascal-style string from the stream.
        /// </summary>
        /// <returns>The next wide-pascal-style string.</returns>
        public string ReadWidePascalString()
        {
            return ReadWidePascalString(Encoding.ASCII);
        }

        /// <summary>
        /// Reads the next wide-pascal-style string from the stream.
        /// </summary>
        /// <param name="enc">The encoding used for the string.</param>
        /// <returns>The next wide-pascal-style string encoded with the specified encoding.</returns>
        public string ReadWidePascalString(Encoding enc)
        {
            int len = ReadInt16();
            string s = enc.GetString(m_data, m_index, len);
            m_index += enc.GetByteCount(s);
            return s;
        }

        /// <summary>
        /// Gets the length of the data.
        /// </summary>
        public virtual int Length
        {
            get
            {
                return m_data.Length;
            }
        }

        #endregion

        /// <summary>
        /// Returns the next variable-length string with the specified terminator character.
        /// </summary>
        /// <param name="Terminator">The terminator that should indicate the end of the string.</param>
        /// <param name="enc">The encoding to use to read the string.</param>
        /// <returns>A variable-length string with no NULL (0) characters nor the terminator character.</returns>
        public string ReadTerminatedString(char Terminator, Encoding enc)
        {
            int i = m_index;
            if (enc == Encoding.Unicode || enc == Encoding.BigEndianUnicode)
            {
                while ((i < m_data.Length) && ((i + 1 < m_data.Length) && (BitConverter.ToChar(m_data, i) != Terminator)))
                    i++;
            }
            else
            {
                while ((i < m_data.Length) && (m_data[i] != (Terminator & 0xff)))
                    i++;
            }

            string s = enc.GetString(m_data, m_index, i - m_index);
            m_index = ++i;

            return s;
        }

        /// <summary>
        /// Checks to see whether the offset from the current position lies within the stream and, if so, advances to
        /// that position relative to the current location.
        /// </summary>
        /// <param name="offset">The number of bytes beyond the current position to advance to.</param>
        /// <returns><b>True</b> if the position lies within the stream and the cursor was advanced; otherwise <b>false</b>.</returns>
        public bool Seek(int offset)
        {
            bool fOk = false;
            if (this.m_index + offset < m_data.Length)
            {
                m_index += offset;
                fOk = true;
            }
            return fOk;
        }

        /// <summary>
        /// Gets the current position within the stream.
        /// </summary>
        public int Position
        {
            get { return m_index; }
        }

        /// <summary>
        /// Gets a hex representation of this buffer.
        /// </summary>
        /// <returns>A string representing this buffer's contents in hexadecimal notation.</returns>
        public override string ToString()
        {
            return DataFormatter.Format(m_data, 0, Length);
        }
    }
}
