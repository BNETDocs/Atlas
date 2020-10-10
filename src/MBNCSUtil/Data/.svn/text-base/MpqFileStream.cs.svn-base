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
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace MBNCSUtil.Data
{
    /// <summary>
    /// Represents an MPQ file stream (that is, a stream within an MPQ file).
    /// </summary>
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    public class MpqFileStream : Stream, IDisposable
    {
        private IntPtr m_hFile;
        private string m_path;
        private MpqArchive m_owner;
        private bool m_disposed;
        private long m_pos;

        [DebuggerStepThrough]
        private void checkDisposed()
        {
            if (m_disposed)
                throw new ObjectDisposedException(m_path, "The MpqFileStream for this object has been disposed.");

        }

        [DebuggerStepThrough]
        internal MpqFileStream(string internalPath, MpqArchive parent)
        {
            m_owner = parent;

            IntPtr hFile = LateBoundStormDllApi.SFileOpenFileEx(parent.Handle, internalPath, SearchType.CurrentOnly);

            m_path = internalPath;
            m_hFile = hFile;
        }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        public string Name { get { return m_path; } }

        #region IDisposable Members

        /// <summary>
        /// Disposes the MpqFileStream.
        /// </summary>
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Cleans up unmanaged resources in use.
        /// </summary>
        /// <param name="disposing"><c>true</c> if the object is being disposed; <c>false</c> if it is being finalized.</param>
        protected override void Dispose(bool disposing)
        {
            if (m_disposed) return;

            if (m_hFile != IntPtr.Zero)
            {
                LateBoundStormDllApi.SFileCloseFile(m_hFile);
                m_hFile = IntPtr.Zero;

                m_owner.FileIsDisposed(this);
                m_owner = null;
                m_path = null;
            }

            m_disposed = true;
        }

        #endregion

        /// <summary>
        /// Gets whether the stream supports reading.
        /// </summary>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <summary>
        /// Gets whether the stream supports seeking.
        /// </summary>
        public override bool CanSeek
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the length of the stream.
        /// </summary>
        public override long Length
        {
            get
            {
                checkDisposed();

                return LateBoundStormDllApi.SFileGetFileSize(m_hFile);
            }
        }

        /// <summary>
        /// Gets or sets the offset from the beginning of the stream at which the stream is currently located.
        /// </summary>
        public override long Position
        {
            get
            {
                return m_pos;
            }
            set
            {
                checkDisposed();

                Seek(value, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Reads data from the underlying stream into the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer that will receive the data.</param>
        /// <param name="offset">The starting location in the buffer to get the data.</param>
        /// <param name="count">The amount of data to be read.</param>
        /// <remarks>
        /// <para>Rather than throwing an exception, if the buffer is too small to return the requested amount of 
        /// data, only as much data as the buffer can hold is returned.</para>
        /// </remarks>
        /// <returns>The number of bytes read.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            checkDisposed();

            if (buffer == null)
                throw new ArgumentNullException("buffer", "The read buffer cannot be null.");

            int maxLen = count - offset;
            if (maxLen > buffer.Length)
            {
                maxLen = buffer.Length;
            }

            int bytesToCopy = (int)(Length - Position);
            if (bytesToCopy > maxLen)
                bytesToCopy = maxLen;

            byte[] tmpBuffer = new byte[maxLen];

            int amount = LateBoundStormDllApi.SFileReadFile(m_hFile, tmpBuffer, bytesToCopy);

            Buffer.BlockCopy(tmpBuffer, 0, buffer, offset, amount);

            m_pos += amount;

            return amount;
        }

        /// <summary>
        /// Moves the current location within the stream to the specified location.
        /// </summary>
        /// <param name="offset">The offset relative to the seek origin.</param>
        /// <param name="origin">The seek origin.</param>
        /// <returns>The new position in the file.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPos = LateBoundStormDllApi.SFileSetFilePointer(m_hFile, offset, origin);
            m_pos = newPos;
            return m_pos;
        }

        #region Inapplicable methods
        /// <summary>
        /// Gets whether this stream supports writing.
        /// </summary>
        public override bool CanWrite
        {
            get { return false; }
        }

        /// <summary>
        /// Flushes the buffer to the underlying stream.
        /// </summary>
        /// <exception cref="NotImplementedException">Thrown whenever this method is called.</exception>
        public override void Flush()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        /// <summary>
        /// Sets the length of the underlying stream.
        /// </summary>
        /// <param name="value">The new length.</param>
        /// <exception cref="NotImplementedException">Thrown whenever this method is called.</exception>
        public override void SetLength(long value)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        /// <summary>
        /// Writes to the stream.
        /// </summary>
        /// <param name="buffer">The data to write.</param>
        /// <param name="offset">The offset into the buffer to write.</param>
        /// <param name="count">The number of bytes to write.</param>
        /// <exception cref="NotImplementedException">Thrown whenever this method is called.</exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }
        #endregion
    }
}
