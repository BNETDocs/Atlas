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
using System.Runtime.InteropServices;


namespace MBNCSUtil
{
    /// <summary>
    /// Completes a <see cref="DataBuffer">DataBuffer</see> implementation with the additional
    /// data used by the BNCS protocol.
    /// </summary>
    /// <example>
    /// <para>The following example illustrates creating a buffer that contains only the
    /// <b>SID_NULL</b> packet:</para>
    /// <para><i><b>Note</b>: this example assumes you have a 
    /// <see cref="System.Net.Sockets.Socket">Socket</see> object called "sck" in the 
    /// current context</i>.</para>
    /// <code language="C#">
    /// [C#]
    /// BncsPacket pckNull = new BncsPacket(0);
    /// sck.Send(pckNull.GetData(), 0, pckNull.Count, SocketFlags.None);
    /// </code>
    /// <code language="Visual Basic">
    /// [Visual Basic]
    /// Dim pckNull As New BncsPacket(0)
    /// sck.Send(pckNull.GetData(), 0, pckNull.Count, SocketFlags.None)
    /// </code>
    /// <code language="C++">
    /// [C++]
    /// BncsPacket ^pckNull = gcnew BncsPacket(0);
    /// sck->Send(pckNull->GetData(), 0, pckNull->Count, SocketFlags.None);
    /// </code>
    /// </example>
    /// <example>
    /// <para>The following example illustrates calculating the revision check (SID_AUTH_ACCONTLOGON) of Warcraft III:</para>
    /// <para><i><b>Note</b>: this example assumes you have a 
    /// <see cref="System.Net.Sockets.Socket">Socket</see> object called "sck" in the 
    /// current context</i>.</para>
    /// <code language="C#">
    /// [C#]
    /// BncsPacket pckLogin = new BncsPacket(0x53);
    /// NLS nls = new NLS(userName, password);
    /// nls.LoginAccount(pckLogin);
    /// sck.Send(pckLogin.GetData(), 0, pckLogin.Count, SocketFlags.None);
    /// </code>
    /// <code language="Visual Basic">
    /// [Visual Basic]
    /// Dim pckLogin As New BncsPacket(&amp;H51)
    /// Dim nls As New NLS(userName, password)
    /// nls.LoginAccount(pckLogin)
    /// sck.Send(pckLogin.GetData(), 0, pckLogin.Count, SocketFlags.None)
    /// </code>
    /// <code language="C++">
    /// [C++]
    /// // NOTE that userName and password must be System::String^, not char*!
    /// BncsPacket ^pckLogin = gcnew BncsPacket(0x51);
    /// NLS ^nls = gcnew NLS(userName, password);
    /// nls->LoginAccount(pckLogin)
    /// sck->Send(pckLogin->GetData(), 0, pckLogin->Count, SocketFlags.None);
    /// </code>
    /// </example>
    public sealed class BncsPacket : DataBuffer
    {
        private byte m_id;

        /// <summary>
        /// Creates a new BNCS packet with the specified packet ID.
        /// </summary>
        /// <param name="id">The BNCS packet ID.</param>
        public BncsPacket(byte id)
            : base()
        {
            m_id = id;
        }

        /// <summary>
        /// Gets or sets the ID of the packet as it was specified when it was created.
        /// </summary>
        public byte PacketID
        {
            get { return m_id; }
            set { m_id = value; }
        }

        /// <inheritdoc />
        public override int Count
        {
            get
            {
                return base.Count + 4;
            }
        }

        /// <summary>
        /// Gets the data in this packet, including the four-byte header.
        /// </summary>
        /// <returns>A byte array containing the packet data.</returns>
        public override byte[] GetData()
        {
            byte[] data = new byte[Count];
            byte[] baseData = base.GetData();
            data[0] = 0xff;
            data[1] = m_id;
            byte[] len = BitConverter.GetBytes((ushort)(Count & 0xffff));
            data[2] = len[0];
            data[3] = len[1];

            Buffer.BlockCopy(baseData, 0, data, 4, baseData.Length);

            return data;
        }
    }
}
