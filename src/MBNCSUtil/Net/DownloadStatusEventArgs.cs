using System;
using System.Collections.Generic;
using System.Text;

namespace MBNCSUtil.Net
{
    /// <summary>
    /// Contains download status information about a currently-executing Battle.net FTP request.  This class cannot be inherited.
    /// </summary>
    /// <seealso cref="DownloadStatusEventHandler"/>
    /// <seealso cref="BnFtpRequestBase.FilePartDownloaded"/>
    public sealed class DownloadStatusEventArgs : EventArgs
    {
        #region fields
        private int m_count, m_total;
        private string m_fileName;
        #endregion

        internal DownloadStatusEventArgs(int current, int total, string file)
        {
            m_count = current;
            m_total = total;
            m_fileName = file;
        }

        #region properties
        /// <summary>
        /// Gets the current length of the file that has been downloaded.
        /// </summary>
        public int DownloadStatus
        {
            get { return m_count; }
        }

        /// <summary>
        /// Gets the total length of the file to download.
        /// </summary>
        public int FileLength
        {
            get { return m_total; }
        }

        /// <summary>
        /// Gets the name of the file being downloaded.
        /// </summary>
        public string FileName
        {
            get { return m_fileName; }
        }
        #endregion
    }

    /// <summary>
    /// Indicates the method type that handles Battle.net FTP download status events.
    /// </summary>
    /// <param name="sender">The object that initiated this event.</param>
    /// <param name="e">Status information about the download.</param>
    /// <remarks>
    /// <para>The <c>sender</c> parameter is guaranteed to always be an instance of <see cref="BnFtpRequestBase">BnFtpRequestBase</see> (or a derived class).</para>
    /// </remarks>
    public delegate void DownloadStatusEventHandler(object sender, DownloadStatusEventArgs e);
}
