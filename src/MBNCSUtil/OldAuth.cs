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
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace MBNCSUtil
{
    /// <summary>
    /// Calculates hash values for data using the old login-system
    /// checks.
    /// </summary>
    /// <remarks>
    /// This method of logon would be used in down-level clients 
    /// using the SID_CLIENTID, SID_CLIENTID2, or SID_AUTH_INFO 
    /// messages specifying the login style <b>0</b> (Old login
    /// system).
    /// </remarks>
    /// <threadsafety>This type is safe for multithreaded operations.</threadsafety>
    [ComVisible(false)]
    public sealed class OldAuth
    {
        private OldAuth()
        {
        }

        /// <summary>
        /// Calculates the single "broken" SHA-1 hash of the specified
        /// data.
        /// </summary>
        /// <param name="data">The data buffer to hash.</param>
        /// <returns>A 20-byte buffer containing the hash value.</returns>
        public static byte[] HashData(byte[] data)
        {
            return XSha1.CalculateHash(data);
        }

        /// <summary>
        /// Calculates the single "broken" SHA-1 hash of the specified
        /// password using ASCII encoding.
        /// </summary>
        /// <param name="data">The password to hash.</param>
        /// <returns>A 20-byte buffer containing the hash value.</returns>
        public static byte[] HashPassword(string data)
        {
            return HashData(Encoding.ASCII.GetBytes(data));
        }

        /// <summary>
        /// Calculates the double-pass "broken" SHA-1 hash of the 
        /// specified data.
        /// </summary>
        /// <param name="data">The data buffer to hash.</param>
        /// <param name="clientToken">The client token, 
        /// a randomly-generated value specified by the client.</param>
        /// <param name="serverToken">The server token, a 
        /// randomly-generated value specified by the server.</param>
        /// <returns>A 20-byte buffer containing the hash value.</returns>
        public static byte[] DoubleHashData(byte[] data,
            int clientToken, int serverToken)
        {
            return DoubleHashData(data,
                unchecked((uint)clientToken),
                unchecked((uint)serverToken));
        }
        /// <summary>
        /// Calculates the double-pass "broken" SHA-1 hash of the 
        /// specified password using ASCII encoding.
        /// </summary>
        /// <param name="data">The password to hash.</param>
        /// <param name="clientToken">The client token, 
        /// a randomly-generated value specified by the client.</param>
        /// <param name="serverToken">The server token, a 
        /// randomly-generated value specified by the server.</param>
        /// <returns>A 20-byte buffer containing the hash value.</returns>
        public static byte[] DoubleHashPassword(string data,
            int clientToken, int serverToken)
        {
            return DoubleHashData(Encoding.ASCII.GetBytes(data),
                unchecked((uint)clientToken),
                unchecked((uint)serverToken));
        }

        /// <summary>
        /// Calculates the double-pass "broken" SHA-1 hash of the 
        /// specified password using ASCII encoding.  This method is
        /// not CLS-compliant.
        /// </summary>
        /// <param name="data">The password to hash.</param>
        /// <param name="clientToken">The client token, 
        /// a randomly-generated value specified by the client.</param>
        /// <param name="serverToken">The server token, a 
        /// randomly-generated value specified by the server.</param>
        /// <returns>A 20-byte buffer containing the hash value.</returns>
        [CLSCompliant(false)]
        public static byte[] DoubleHashPassword(string data,
            uint clientToken, uint serverToken)
        {
            return DoubleHashData(Encoding.ASCII.GetBytes(data),
                clientToken, serverToken);
        }

        /// <summary>
        /// Calculates the double-pass "broken" SHA-1 hash of the 
        /// specified data.  This method is not CLS-compliant.
        /// </summary>
        /// <param name="data">The data buffer to hash.</param>
        /// <param name="clientToken">The client token, 
        /// a randomly-generated value specified by the client.</param>
        /// <param name="serverToken">The server token, a 
        /// randomly-generated value specified by the server.</param>
        /// <returns>A 20-byte buffer containing the hash value.</returns>
        [CLSCompliant(false)]
        public static byte[] DoubleHashData(byte[] data,
            uint clientToken, uint serverToken)
        {
            MemoryStream ms = new MemoryStream(28);
            BinaryWriter bw = new BinaryWriter(ms);
            byte[] firstHash = XSha1.CalculateHash(data);
            bw.Write(clientToken);
            bw.Write(serverToken);
            bw.Write(firstHash);
            byte[] toCalc = ms.GetBuffer();
            return XSha1.CalculateHash(toCalc);
        }
    }
}
