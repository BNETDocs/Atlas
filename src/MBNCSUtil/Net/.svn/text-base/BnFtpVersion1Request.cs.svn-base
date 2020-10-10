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
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace MBNCSUtil.Net
{
    /// <summary>
    /// Represents a Battle.net FTP (BnFTP) file transfer request for Version 1 products.
    /// </summary>
    /// <remarks>
    /// <para>This class is only valid for Starcraft Retail, Starcraft: Brood War, Diablo II Retail, 
    /// Diablo II: Lord of Destruction, and Warcraft II: Battle.net Edition clients.  For Warcraft III: The Reign
    /// of Chaos and Warcraft III: The Frozen Throne, use the <see cref="BnFtpVersion2Request">BnFtpVersion2Request</see>
    /// class.</para>
    /// </remarks>
    public class BnFtpVersion1Request : BnFtpRequestBase
    {
        private string m_adExt;
        private int m_adId;
        private bool m_ad;

        // 33 + fileName.Length

        /// <summary>
        /// Creates a standard Version 1 Battle.net FTP request.
        /// </summary>
        /// <param name="productId">The four-character identifier for the product being emulated by this request.</param>
        /// <param name="fileName">The full or relative path to the file as it is to be stored on the local 
        /// machine.  The name portion of the file must be the filename being requested from the service.</param>
        /// <param name="fileTime">The last-write time of the file.  If the file is not available, this parameter
        /// can be <b>null</b> (<b>Nothing</b> in Visual Basic).</param>
        public BnFtpVersion1Request(string productId, string fileName, DateTime? fileTime)
            : base(fileName, productId, fileTime)
        {
            string m_prod = this.Product;

            if (m_prod != Resources.star &&
                m_prod != Resources.sexp &&
                m_prod != Resources.d2dv &&
                m_prod != Resources.d2xp &&
                m_prod != Resources.w2bn)
            {
                throw new ArgumentOutOfRangeException(Resources.param_productId, productId,
                    Resources.bnftp_ver1invalidProduct);
            }
        }

        /// <summary>
        /// Creates a Version 1 Battle.net FTP request specifically for banner ad downloads.
        /// </summary>
        /// <param name="productId">The four-character identifier for the product being emulated by this request.</param>
        /// <param name="fileName">The full or relative path to the file as it is to be stored on the local 
        /// machine.  The name portion of the file must be the filename being requested from the service.</param>
        /// <param name="fileTime">The last-write time of the file.  If the file is not available, this parameter
        /// can be <b>null</b> (<b>Nothing</b> in Visual Basic).</param>
        /// <param name="adBannerId">The banner ID provided by Battle.net's ad notice message.</param>
        /// <param name="adBannerExtension">The banner filename extension provided by Battle.net's ad notice message.</param>
        /// <remarks>
        /// <para>Although it is not specifically required to download banner ads, it is recommended for forward-compatibility
        /// with the Battle.net protocol that this constructor is used.</para>
        /// </remarks>
        public BnFtpVersion1Request(string productId, string fileName, DateTime fileTime, 
            int adBannerId, string adBannerExtension)
            : this(productId, fileName, fileTime)
        {
            m_adExt = adBannerExtension;
            m_adId = adBannerId;
            m_ad = true;
        }

        /// <summary>
        /// Executes the BnFTP request, downloading the file to where <see cref="BnFtpRequestBase.LocalFileName">LocalFileName</see>
        /// specifies, and closes the connection.
        /// </summary>
        /// <remarks>
        /// <para>By default, <c>LocalFileName</c> is the same name as the remote file, which will cause the file
        /// to be saved in the local application path.  The desired location of the file must be set before 
        /// <b>ExecuteRequest</b> is called.</para>
        /// </remarks>
        /// <exception cref="IOException">Thrown if the local file cannot be written.</exception>
        /// <exception cref="SocketException">Thrown if the remote host closes the connection prematurely.</exception>
        public override void ExecuteRequest()
        {
            DataBuffer buffer = new DataBuffer();
            buffer.InsertInt16((short)(33 + FileName.Length));
            buffer.InsertInt16(0x0100);
            buffer.InsertDwordString("IX86");
            buffer.InsertDwordString(Product);
            if (m_ad)
            {
                buffer.InsertInt32(m_adId);
                buffer.InsertDwordString(m_adExt);
            }
            else
            {
                buffer.InsertInt64(0);
            }
            // currently resuming is not supported
            buffer.InsertInt32(0);
            if (FileTime.HasValue)
            {
                buffer.InsertInt64(FileTime.Value.ToFileTimeUtc());
            }
            else
            {
                buffer.InsertInt64(0);
            }
            buffer.InsertCString(FileName);

            Socket sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sck.Connect(Server, 6112);
            sck.Send(new byte[] { 2 });
            sck.Send(buffer.GetData(), 0, buffer.Count, SocketFlags.None);

            byte[] hdrLengthBytes = new byte[2];
            sck.Receive(hdrLengthBytes, 2, SocketFlags.None);

            int hdrLen = BitConverter.ToInt16(hdrLengthBytes, 0);
            Trace.WriteLine(hdrLen, "Header Length");
            byte[] hdrBytes = new byte[hdrLen - 2];
            sck.Receive(hdrBytes, hdrLen - 2, SocketFlags.None);
            DataReader rdr = new DataReader(hdrBytes);
            rdr.Seek(2);
            int fileSize = rdr.ReadInt32();
            this.FileSize = fileSize;
            rdr.Seek(8);
            long fileTime = rdr.ReadInt64();
            string name = rdr.ReadCString();
            if (string.Compare(name, FileName, StringComparison.OrdinalIgnoreCase) != 0 || FileSize == 0)
            {
                throw new FileNotFoundException(Resources.bnftp_filenotfound);
            }
            Trace.WriteLine(fileSize, "File Size");

            byte[] data = ReceiveLoop(sck, fileSize);
            sck.Close();

            FileStream fs = new FileStream(LocalFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            fs.SetLength(fileSize);
            fs.Write(data, 0, fileSize);
            fs.Flush();
            fs.Close();
            DateTime time = DateTime.FromFileTimeUtc(fileTime);
            File.SetLastWriteTimeUtc(LocalFileName, time);
        }

        private byte[] ReceiveLoop(Socket sck, int totalLength)
        {
            byte[] incBuffer = new byte[totalLength];
            int totRecv = 0;

            using (NetworkStream ns = new NetworkStream(sck, false))
            {
                while (sck.Connected && totRecv < totalLength)
                {
                    int sizeToReceive = Math.Min(totalLength - totRecv, 10240);
                    int received = ns.Read(incBuffer, totRecv, sizeToReceive);
                    if (received == 0)
                        throw new SocketException();

                    totRecv += received;

                    try
                    {
                        OnFilePartDownloaded(new DownloadStatusEventArgs(totRecv, totalLength, FileName));
                    }
                    catch
#if DEBUG
                        (Exception ex)
#endif
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine("An unhandled exception was raised in the BnFTP receive loop but was not passed to the caller to prevent interruption in the download (as the exception was not raised because of the download, but because of an event handler listening to the download status).  The exception follows:");
                        System.Diagnostics.Debug.WriteLine(ex);
#endif
                    }
                }
            }

            return incBuffer;
        }
        
    }
}
