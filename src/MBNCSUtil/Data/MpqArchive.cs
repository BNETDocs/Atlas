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
using System.Security.Permissions;

namespace MBNCSUtil.Data
{
    /// <summary>
    /// Represents an MPQ archive.
    /// </summary>
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    public class MpqArchive : IDisposable
    {
        private IntPtr m_hMPQ;
        private bool m_disposed;
        private List<MpqFileStream> m_files;

        [DebuggerStepThrough]
        private void checkDisposed()
        {
            if (m_disposed)
                throw new ObjectDisposedException("MpqArchive");
        }

        internal MpqArchive(string path)
        {
            m_files = new List<MpqFileStream>();

            if (!File.Exists(path))
                throw new FileNotFoundException(Resources.fileNotFound, path);

            m_hMPQ = LateBoundStormDllApi.SFileOpenArchive(path, 1, 0);
        }

        /// <summary>
        /// Opens the specified file contained within the MPQ.
        /// </summary>
        /// <param name="mpqFilePath">The path to the file relative to the MPQ root.</param>
        /// <returns>An <see cref="MpqFileStream">MpqFileStream</see> to the file within the MPQ.</returns>
        /// <exception cref="MpqException">Thrown if the file is not found or there is a problem reading from the MPQ.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <c>mpqFilePath</c> is <b>null</b> (<b>Nothing</b> in Visual Basic).</exception>
        public MpqFileStream OpenFile(string mpqFilePath)
        {
            if (mpqFilePath == null) throw new ArgumentNullException(Resources.param_mpqFilePath, Resources.mpqFilePathArgNull);

            return new MpqFileStream(mpqFilePath, this);
        }


        internal IntPtr Handle
        {
            [DebuggerStepThrough]
            get { checkDisposed(); return m_hMPQ; }
        }

        [DebuggerStepThrough]
        internal void FileIsDisposed(MpqFileStream mfs)
        {
            checkDisposed();

            m_files.Remove(mfs);
        }

        /// <summary>
        /// Determines whether the archive contains the specified file.
        /// </summary>
        /// <param name="fileName">The path to the file relative to the MPQ root.</param>
        /// <returns><b>True</b> if the file is contained within the MPQ; otherwise <b>false</b>.</returns>
        public bool ContainsFile(string fileName)
        {
            return LateBoundStormDllApi.SFileHasFile(m_hMPQ, fileName);
        }

        /// <summary>
        /// Saves the specified file to the provided path.
        /// </summary>
        /// <param name="mpqFileName">The fully-qualified name of the file in the MPQ.</param>
        /// <param name="pathBase">The path to which to save the file.</param>
        /// <remarks>
        /// <para>The file is saved as an immediate child of the path specified in <paramref name="pathBase"/>.</para>
        /// </remarks>
        public void SaveToPath(string mpqFileName, string pathBase)
        {
            SaveToPath(mpqFileName, pathBase, false);
        }

        /// <summary>
        /// Saves the specified file to the specified path, optionally expanding the paths used in the MPQ.
        /// </summary>
        /// <param name="mpqFileName">The fully-qualified name of the file in the MPQ.</param>
        /// <param name="pathBase">The root path to which to save the file.</param>
        /// <param name="useFullMpqPath">Whether to create child directories based on the path to the file in the MPQ.</param>
        public void SaveToPath(string mpqFileName, string pathBase, bool useFullMpqPath)
        {
            string path;
            if (useFullMpqPath)
                path = Path.Combine(pathBase, mpqFileName);
            else
                path = Path.Combine(pathBase, mpqFileName.Substring(mpqFileName.LastIndexOf('\\') + 1));

            string directoryName = Path.GetDirectoryName(path);
            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);

            using (MpqFileStream fs = OpenFile(mpqFileName))
            {
                byte[] fileData = new byte[fs.Length];
                fs.Read(fileData, 0, fileData.Length);
                File.WriteAllBytes(path, fileData);
            }
        }

        #region IDisposable Members
        /// <summary>
        /// Called when the .NET Framework is removing this object from memory.
        /// </summary>
        ~MpqArchive()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes this archive.
        /// </summary>
        /// <remarks>
        /// <para>If you call Dispose on an archive you do not need to call <see cref="MpqServices.CloseArchive">MpqServices.CloseArchive</see> to close it.</para>
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Cleans up unmanaged resources used by this archive.
        /// </summary>
        /// <param name="disposing"><c>true</c> if the object is being disposed; <c>false</c> if it is being finalized.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (m_disposed) return;

            if (disposing)
            {
                foreach (MpqFileStream mfs in m_files)
                {
                    mfs.Dispose();
                }

                m_files.Clear();
                m_files = null;
            }

            if (m_hMPQ != IntPtr.Zero)
            {
                LateBoundStormDllApi.SFileCloseArchive(m_hMPQ);
            }

            m_disposed = true;

            MpqServices.NotifyArchiveDisposed(this);
        }

        #endregion

        #region IMpqArchive Members
        /// <summary>
        /// Gets the full text of the MPQ list file.
        /// </summary>
        /// <remarks>
        /// <para>In later versions of Blizzard's games, the developers included a file called "(listfile)" in 
        /// most MPQ archives identifying the names of the files contained in the MPQ (since they are hashed and
        /// therefore unavailable otherwise).</para>
        /// </remarks>
        /// <returns>A string containing the full text of the list file.</returns>
        /// <exception cref="MpqException">Thrown if the list file is not located.</exception>
        public string GetListFile()
        {
            string list = string.Empty;
            using (MpqFileStream mfs = OpenFile("(listfile)"))
            {
                StreamReader sr = new StreamReader(mfs, Encoding.ASCII);
                list = sr.ReadToEnd();
                sr.Close();
            }
            return list;
        }

        #endregion
    }
}
