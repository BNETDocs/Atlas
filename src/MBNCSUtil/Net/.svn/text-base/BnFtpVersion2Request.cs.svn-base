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

namespace MBNCSUtil.Net
{
    /// <summary>
    /// Represents a Battle.net FTP (BnFTP) file transfer request for Version 2 products.
    /// </summary>
    /// <remarks>
    /// <para>This class is only valid for Warcraft III: The Reign of Chaos and Warcraft III: The Frozen Throne.
    /// For Starcraft Retail, Starcraft: Brood War, Diablo II Retail, Diablo II: Lord of Destruction, and Warcraft
    /// II: Battle.net Edition clients, use the <see cref="BnFtpVersion1Request">BnFtpVersion1Request</see>
    /// class.</para>
    /// </remarks>
    public class BnFtpVersion2Request : BnFtpRequestBase
    {
        private int m_adId;
        private string m_adExt;
        private CdKey m_key;
        private bool m_ad;

        /// <summary>
        /// Creates a standard Version 2 Battle.net FTP request.
        /// </summary>
        /// <param name="productId">The four-character identifier for the product being emulated by this request.</param>
        /// <param name="fileName">The full or relative path to the file as it is to be stored on the local 
        /// machine.  The name portion of the file must be the filename being requested from the service.</param>
        /// <param name="fileTime">The last-write time of the file.  If the file is not available, this parameter
        /// can be <b>null</b> (<b>Nothing</b> in Visual Basic).</param>
        /// <param name="cdKey">The CD key of the client being emulated.</param>
        public BnFtpVersion2Request(string productId, string fileName, DateTime fileTime, string cdKey)
            : base(fileName, productId, fileTime)
        {
            string prod = Product;
            if (prod != Resources.war3 &&
                prod != Resources.w3xp)
            {
                throw new ArgumentOutOfRangeException(Resources.param_productId, productId, Resources.bnftp_ver2invalidProduct);
            }

            m_key = new CdKey(cdKey);
        }

        /// <summary>
        /// Creates a Version 2 Battle.net FTP request specifically for banner ad downloads.
        /// </summary>
        /// <param name="product">The four-character identifier for the product being emulated by this request.</param>
        /// <param name="fileName">The full or relative path to the file as it is to be stored on the local 
        /// machine.  The name portion of the file must be the filename being requested from the service.</param>
        /// <param name="fileTime">The last-write time of the file.  If the file is not available, this parameter
        /// can be <b>null</b> (<b>Nothing</b> in Visual Basic).</param>
        /// <param name="cdKey">The CD key of the client being emulated.</param>
        /// <param name="adId">The banner ID provided by Battle.net's ad notice message.</param>
        /// <param name="adFileExtension">The banner filename extension provided by Battle.net's ad notice message.</param>
        /// <remarks>
        /// <para>Although it is not specifically required to download banner ads, it is recommended for forward-compatibility
        /// with the Battle.net protocol that this constructor is used.</para>
        /// </remarks>
        public BnFtpVersion2Request(string fileName, string product, DateTime fileTime, string cdKey, 
            int adId, string adFileExtension)
            : this(fileName, product, fileTime, cdKey)
        {
            m_ad = true;
            m_adId = adId;
            m_adExt = adFileExtension;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "time")]
        public override void ExecuteRequest()
        {
            DataBuffer buf1 = new DataBuffer();
            buf1.InsertInt16(20);
            buf1.InsertInt16(0x0200);
            buf1.InsertDwordString("IX86");
            buf1.InsertDwordString(Product);
            if (m_ad)
            {
                buf1.InsertInt32(m_adId);
                buf1.InsertDwordString(m_adExt);
            }
            else
            {
                buf1.InsertInt64(0);
            }

            Socket sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sck.Connect(Server, 6112);

            sck.Send(new byte[] { 2 });
            sck.Send(buf1.GetData(), 0, buf1.Count, SocketFlags.None);

            NetworkStream ns = new NetworkStream(sck, false);
            DataReader rdr = new DataReader(ns, 4);
            int serverToken = rdr.ReadInt32();

            DataBuffer buf2 = new DataBuffer();
            buf2.InsertInt32(0); // no resuming
            if (FileTime.HasValue)
            {
                buf2.InsertInt64(FileTime.Value.ToFileTimeUtc()); 
            }
            else
            {
                buf2.InsertInt64(0);
            }

            int clientToken = new Random().Next();
            buf2.InsertInt32(clientToken);

            buf2.InsertInt32(m_key.Key.Length);
            buf2.InsertInt32(m_key.Product);
            buf2.InsertInt32(m_key.Value1);
            buf2.InsertInt32(0);
            buf2.InsertByteArray(m_key.GetHash(clientToken, serverToken));
            buf2.InsertCString(FileName);

            sck.Send(buf2.GetData(), 0, buf2.Count, SocketFlags.None);

            rdr = new DataReader(ns, 4);
            int msg2Size = rdr.ReadInt32() - 4;
            rdr = new DataReader(ns, msg2Size);

            this.FileSize = rdr.ReadInt32();
            rdr.Seek(8);
            long fileTime = rdr.ReadInt64();
            DateTime time = DateTime.FromFileTimeUtc(fileTime);
            string name = rdr.ReadCString();
            if (string.Compare(name, FileName, StringComparison.OrdinalIgnoreCase) != 0 || FileSize == 0)
            {
                throw new FileNotFoundException(Resources.bnftp_filenotfound);
            }

            byte[] data = ReceiveLoop(sck, FileSize);
            sck.Close();

            FileStream fs = new FileStream(LocalFileName, FileMode.OpenOrCreate, FileAccess.Write);
            fs.Write(data, 0, FileSize);
            fs.Flush();
            fs.Close();
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