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

namespace MBNCSUtil.Net
{
    /// <summary>
    /// Represents a generic Battle.net FTP (BnFTP) request.
    /// </summary>
    /// <remarks>
    /// <para>The specific Battle.net FTP protocol is implemented by the 
    /// <see cref="BnFtpVersion1Request">BnFtpVersion1Request</see> and 
    /// <see cref="BnFtpVersion2Request">BnFtpVersion2Request</see> classes, which 
    /// have their uses based on which client is being emulated.  For Warcraft 3 and
    /// The Frozen Throne, <b>BnFtpVersion2Request</b> should be used; otherwise, 
    /// <b>BnFtpVersion1Request</b> should be used.</para>
    /// </remarks>
    public abstract class BnFtpRequestBase
    {
        private string m_fileName, m_localFile, m_product, m_server = "useast.battle.net";
        private int m_size;
        private DateTime? m_time;

        /// <summary>
        /// Creates a new generic Battle.net FTP request.
        /// </summary>
        /// <param name="fileName">The name of the file to be downloaded.</param>
        /// <param name="product">The four-character product string specifying the
        /// client being emulated.</param>
        /// <param name="fileTime">The timestamp of the file's last write in UTC.
        /// You may specify <b>null</b> (<b>Nothing</b> in Visual Basic) if the 
        /// time is unavailable.</param>
        /// <remarks>
        /// <para>Valid emulation clients include:
        /// <list type="bullet">
        ///     <item>STAR for Starcraft Retail</item>
        ///     <item>SEXP for Starcraft: Brood War</item>
        ///     <item>W2BN for Warcraft II: Battle.net Edition</item>
        ///     <item>D2DV for Diablo II Retail</item>
        ///     <item>D2XP for Diablo II: Lord of Destruction</item>
        ///     <item>WAR3 for Warcraft III: The Reign of Chaos</item>
        ///     <item>W3XP for Warcraft III: The Frozen Throne</item>
        /// </list>
        /// </para>
        /// </remarks>
        protected BnFtpRequestBase(string fileName, string product, DateTime? fileTime)
        {
            m_fileName = fileName;
            if (fileName.IndexOf('\\') != -1)
            {
                m_fileName = fileName.Substring(fileName.LastIndexOf('\\') + 1);
            }
            m_product = product.ToUpperInvariant();
            
            m_time = fileTime;

            this.LocalFileName = fileName;
        }

        /// <summary>
        /// Gets the Product string utilized by this request.
        /// </summary>
        public virtual string Product { get { return m_product; } }

        /// <summary>
        /// Invokes the <see cref="FilePartDownloaded">FilePartDownloaded</see> event.
        /// </summary>
        /// <remarks>
        /// <para><b>Note to Inheritors:</b> The suggested way to hook the <b>FilePartDownloaded</b> event is to override this method.  However, to 
        /// ensure that the event is called and all listeners receive it, be certain to call the base implementation as well.</para>
        /// </remarks>
        /// <param name="e">The download status for this file.</param>
        protected virtual void OnFilePartDownloaded(DownloadStatusEventArgs e)
        {
            if (FilePartDownloaded != null)
                FilePartDownloaded(this, e);
        }

        /// <summary>
        /// Indicates that part of a file has been downloaded during this request.
        /// </summary>
        public event DownloadStatusEventHandler FilePartDownloaded;

        #region IBnFtpRequest Members

        /// <summary>
        /// Gets or sets the local path of the file.
        /// </summary>
        /// <remarks>
        /// <para>This property must be set before the <see cref="ExecuteRequest">ExecuteRequest</see> method is 
        /// called.  It can be changed in subsequent calls to download the same file to multiple locations; however,
        /// changing this property will not affect files that have already been downloaded.</para>
        /// </remarks>
        public string LocalFileName
        {
            get
            {
                return m_localFile;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(Resources.param_value, Resources.fileNull);

                if (!value.Contains("\\"))
                {
                    value = Path.GetFullPath(string.Concat(".\\", value));
                }
                string tmpPath = Path.GetDirectoryName(value);
                if (!Directory.Exists(tmpPath))
                {
                    Directory.CreateDirectory(tmpPath);
                }

                m_localFile = value;
            }
        }

        /// <summary>
        /// Gets the name of the filed being requested.
        /// </summary>
        public string FileName
        {
            get { return m_fileName; }
        }

        /// <summary>
        /// Gets (and in derived classes, sets) the size of the file.
        /// </summary>
        /// <remarks>
        /// <para>This property is only valid after 
        /// <see cref="ExecuteRequest">ExecuteRequest</see> has been called.</para>
        /// </remarks>
        public int FileSize
        {
            get { return m_size; }
            protected set
            {
                m_size = value;
            }
        }

        /// <summary>
        /// Gets the local file's last-write time, if it was specified.  If it was not specified, this property
        /// returns <b>null</b> (<b>Nothing</b> in Visual Basic).
        /// </summary>
        public DateTime? FileTime { get { return m_time; } }

        /// <summary>
        /// Gets or sets the server from which this request should download.
        /// </summary>
        /// <remarks>
        /// <para>The default server is <c>useast.battle.net</c>.</para>
        /// </remarks>
        public string Server
        {
            get
            {
                return m_server;
            }
            set
            {
                if (value == null)
                    value = "useast.battle.net";

                m_server = value;
            }
        }

        /// <summary>
        /// Executes the BnFTP request, downloading the file to where <see cref="LocalFileName">LocalFileName</see>
        /// specifies, and closes the connection.
        /// </summary>
        /// <remarks>
        /// <para>By default, <c>LocalFileName</c> is the same name as the remote file, which will cause the file
        /// to be saved in the local application path.  The desired location of the file must be set before 
        /// <b>ExecuteRequest</b> is called.</para>
        /// </remarks>
        /// <exception cref="IOException">Thrown if the local file cannot be written.</exception>
        /// <exception cref="System.Net.Sockets.SocketException">Thrown if the remote host closes the connection prematurely.</exception>
        public abstract void ExecuteRequest();

        #endregion
    }
}
