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
using System.Security.Cryptography;
using System.Diagnostics;
using MBNCSUtil.Util;
using System.Globalization;

namespace MBNCSUtil
{
    /// <summary>
    /// Supports the New Logon System's SRP (Secure Remote Password)
    /// authentication system as well as Warcraft III server 
    /// signature validation.  This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>This class does not monitor contexts to ensure that 
    /// its values are being modified in the appropriate sequence;
    /// the NLS authorization scheme is left up to the consumer.</para>
    /// </remarks>
    /// <threadsafety>This type is safe for all multithreaded operations.</threadsafety>
    public sealed class NLS
    {
        #region constants
        /// <summary>
        /// The modulus value used for login calculations.
        /// </summary>
        public const string Modulus = "F8FF1A8B619918032186B68CA092B5557E976C78C73212D91216F6658523C787";
        /// <summary>
        /// The generator value used for login calculations.
        /// </summary>
        public const int Generator = 47; // 0x2f
        /// <summary>
        /// The four-byte RSA server signature key used to decrypt 
        /// the server signatures.
        /// </summary>
        public const int SignatureKey = 0x10001;
        /// <summary>
        /// The modulus used to calculate the server IP signature.
        /// </summary>
        public const string ServerModulus = "cf8d697fbac28db6fd9d54cc4140edc296785157e7bdf52db032d940668e16ea76348a8e6932844120d38a085e3df42a98dd00c2e4fc26fdf425d34d2dc582d020a606a1d577e1c973b8f3cb9e430788fc395a150b480f293556ba2dfcc1e5dcb556b58f0ecd3b3aa1b41942e820fab032e30b9d786efac30fc50d0fabd6a3d5 ";
        #endregion
        #region helper static fields
        private static readonly SHA1 s_sha = new SHA1Managed();
        private static readonly RandomNumberGenerator s_rand =
            new RNGCryptoServiceProvider();
        private static readonly BigInteger s_modulus = new BigInteger(Modulus, 16);
        private static readonly BigInteger s_generator = new BigInteger((ulong)47);
        #endregion
        #region fields
        private string userName, password;
        private byte[] k, userNameAscii;
        private BigInteger verifier, x, a, A, m1;
        #endregion
        #region ctor/dtor/static creator
        /// <summary>
        /// Creates a new NLS login context.
        /// </summary>
        /// <param name="Username">The username to use for authentication.</param>
        /// <param name="Password">The password to use for authentication.</param>
        /// <remarks>
        /// This type does not validate the sequence from moving from one message to the next.  Ensure that you
        /// have the correct sequence of calls.
        /// </remarks>
        /// <returns>An NLS context ID.</returns>
        public NLS(string Username, string Password)
        {
            userName = Username;
            userNameAscii = Encoding.ASCII.GetBytes(userName);
            password = Password;

            byte[] rand_a = new byte[32];
            s_rand.GetNonZeroBytes(rand_a);
            a = new BigInteger(rand_a);
            a %= s_modulus;

            a = new BigInteger(ReverseArray(a.GetBytes()));
            //A = s_generator.ModPow(a, s_modulus);
            A = new BigInteger(ReverseArray(s_generator.ModPow(a, s_modulus).GetBytes()));
        }
        #endregion

        #region verify server proof
        /// <summary>
        /// Verifies that the server's proof value matches the value
        /// calculated by the client.
        /// </summary>
        /// <param name="serverProof">The 20-byte server proof.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if 
        /// the server proof value is not exactly 20 bytes.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the object has not 
        /// yet been initialized.</exception>
        /// <remarks>
        /// This method should be called after the <see cref="LoginProof(byte[], int, int, byte[], byte[])">LoginProof</see> method.
        /// </remarks>
        /// <returns><b>True</b> if the server proof is valid; 
        /// otherwise <b>false</b>.</returns>
        public bool VerifyServerProof(byte[] serverProof)
        {
            if (serverProof.Length != 20)
                throw new ArgumentOutOfRangeException(Resources.nlsServerProof20);

            MemoryStream ms_m2 = new MemoryStream(92);
            BinaryWriter bw = new BinaryWriter(ms_m2);
            bw.Write(EnsureArrayLength(A.GetBytes(), 32));
            bw.Write(m1.GetBytes());
            bw.Write(k);
            byte[] client_m2_data = ms_m2.GetBuffer();
            ms_m2.Close();

            byte[] client_hash_m2 = s_sha.ComputeHash(client_m2_data);
            BigInteger client_m2 = new BigInteger(client_hash_m2);
            BigInteger server_m2 = new BigInteger(serverProof);

#if DEBUG
            Trace.WriteLine(client_m2.ToHexString(), "Client");
            Trace.WriteLine(server_m2.ToHexString(), "Server");
#endif

            return client_m2.Equals(server_m2);
        }
        #endregion
        #region login proof
        /// <summary>
        /// Adds the account login proof (for SID_AUTH_ACCOUNTLOGONPROOF)
        /// to the specified stream at the current location.
        /// </summary>
        /// <param name="stream">The stream to modify.</param>
        /// <param name="serverSalt">The salt value, sent from the server
        /// in SID_AUTH_ACCOUNTLOGON.</param>
        /// <param name="serverRandomKey">The server key, sent from the server
        /// in SID_AUTH_ACCOUNTLOGON.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if 
        /// the salt or server key values are not exactly 32 bytes.</exception>
        /// <exception cref="IOException">Thrown if the buffer does 
        /// not have enough space to add the account creation information.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the object has not 
        /// yet been initialized.</exception>
        /// <remarks>
        /// <para>The writeable length of the stream must be at least 20 bytes.</para>
        /// <para>This method should be called after the <see cref="LoginAccount(byte[], int, int)">LoginAccount</see> method.</para>
        /// </remarks>
        /// <returns>The total number of bytes written to the buffer.</returns>
        public int LoginProof(Stream stream, byte[] serverSalt, byte[] serverRandomKey)
        {
            if (serverSalt.Length != 32)
                throw new ArgumentOutOfRangeException(Resources.param_salt, serverSalt, Resources.nlsSalt32);
            if (serverRandomKey.Length != 32)
                throw new ArgumentOutOfRangeException(Resources.param_serverKey, serverRandomKey, Resources.nlsServerKey32);

            if (stream.Position + 20 > stream.Length)
                throw new IOException(Resources.nlsLoginProofSpace);

            CalculateM1(serverSalt, serverRandomKey);

            stream.Write(EnsureArrayLength(this.m1.GetBytes(), 20), 0, 20);

            return 20;
        }

        /// <summary>
        /// Adds the account login proof (for SID_AUTH_ACCOUNTLOGONPROOF)
        /// to the specified buffer at the specified location.
        /// </summary>
        /// <param name="buffer">The buffer to modify.</param>
        /// <param name="startIndex">The starting index at which to 
        /// modify the buffer.</param>
        /// <param name="totalLength">The total number of bytes from 
        /// the starting index of the buffer that may be modified.</param>
        /// <param name="serverSalt">The salt value, sent from the server
        /// in SID_AUTH_ACCOUNTLOGON.</param>
        /// <param name="serverKey">The server key, sent from the server
        /// in SID_AUTH_ACCOUNTLOGON.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if 
        /// the salt or server key values are not exactly 32 bytes.</exception>
        /// <exception cref="IOException">Thrown if the buffer does 
        /// not have enough space to add the account creation information.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the object has not 
        /// yet been initialized.</exception>
        /// <remarks>
        /// <para>The writeable length of the buffer must be at least 20 bytes.</para>
        /// <para>This method should be called after the <see cref="LoginAccount(byte[], int, int)">LoginAccount</see> method.</para>
        /// </remarks>
        /// <returns>The total number of bytes written to the buffer.</returns>
        public int LoginProof(byte[] buffer, int startIndex, int totalLength, byte[] serverSalt, byte[] serverKey)
        {
            MemoryStream ms = new MemoryStream(buffer, startIndex, totalLength, true);
            return LoginProof(ms, serverSalt, serverKey);
        }

        /// <summary>
        /// Adds the account login proof (for SID_AUTH_ACCOUNTLOGONPROOF)
        /// to the specified packet.
        /// </summary>
        /// <param name="logonProofPacket">The BNCS packet to which to add the account logon data.</param>
        /// <param name="serverSalt">The salt value, sent from the server
        /// in SID_AUTH_ACCOUNTLOGON.</param>
        /// <param name="serverKey">The server key, sent from the server
        /// in SID_AUTH_ACCOUNTLOGON.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if 
        /// the salt or server key values are not exactly 32 bytes.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the object has not 
        /// yet been initialized.</exception>
        /// <remarks>
        /// <para>This method should be called after the <see cref="LoginAccount(DataBuffer)">LoginAccount</see> method.</para>
        /// </remarks>
        /// <returns>The total number of bytes written to the buffer.</returns>
        public int LoginProof(DataBuffer logonProofPacket, byte[] serverSalt, byte[] serverKey)
        {
            byte[] temp = new byte[20];
            int len = LoginProof(temp, 0, 20, serverSalt, serverKey);
            logonProofPacket.Insert(temp);
            return len;
        }

        #endregion
        #region login account
        /// <summary>
        /// Adds the account login information (for SID_AUTH_ACCOUNTLOGON)
        /// to the specified stream at the current location.
        /// </summary>
        /// <param name="stream">The stream to modify.</param>
        /// <exception cref="IOException">Thrown if the stream does 
        /// not have enough space to add the account creation information.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the object has not 
        /// yet been initialized.</exception>
        /// <remarks>
        /// <para>The writeable length of the stream must be at least 33 bytes plus the length of the username.</para>
        /// <para>This method may be called first after creating the instance, or after the 
        /// <see cref="CreateAccount(Stream)">CreateAccount</see> method.</para>
        /// </remarks>
        /// <returns>The total number of bytes written to the stream.</returns>
        public int LoginAccount(Stream stream)
        {
            if ((stream.Position + 33 + userNameAscii.Length) > stream.Length)
                throw new IOException(Resources.nlsAcctLoginSpace);

            stream.Write(EnsureArrayLength(A.GetBytes(), 32), 0, 32);
            stream.Write(userNameAscii, 0, userNameAscii.Length);
            stream.WriteByte(0);

            return 33 + userNameAscii.Length;
        }

        /// <summary>
        /// Adds the account login information (for SID_AUTH_ACCOUNTLOGON)
        /// to the specified packet.
        /// </summary>
        /// <param name="loginPacket">The packet to which to add the login information.</param>
        /// <exception cref="InvalidOperationException">Thrown if the object has not 
        /// yet been initialized.</exception>
        /// <remarks>
        /// <para>This method may be called first after creating the instance, or after the 
        /// <see cref="CreateAccount(DataBuffer)">CreateAccount</see> method.</para>		
        /// </remarks>
        /// <returns>The total number of bytes written to the buffer.</returns>
        // Suppressed CA1726: "LoginAccount" is the canonical name from Battle.net terminology.
        public int LoginAccount(DataBuffer loginPacket)
        {
            byte[] temp = new byte[33 + userNameAscii.Length];
            int len = LoginAccount(temp, 0, temp.Length);
            loginPacket.Insert(temp);
            return len;
        }

        /// <summary>
        /// Adds the account login information (for SID_AUTH_ACCOUNTLOGON)
        /// to the specified buffer at the specified location.
        /// </summary>
        /// <param name="buffer">The buffer to modify.</param>
        /// <param name="startIndex">The starting index at which to 
        /// modify the buffer.</param>
        /// <param name="totalLength">The total number of bytes from 
        /// the starting index of the buffer that may be modified.</param>
        /// <exception cref="IOException">Thrown if the buffer does 
        /// not have enough space to add the account creation information.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the object has not 
        /// yet been initialized.</exception>
        /// <remarks>
        /// <para>The writeable length of the stream must be at least 33 bytes plus the length of the username.</para>
        /// <para>This method may be called first after creating the instance, or after the 
        /// <see cref="CreateAccount(byte[], int, int)">CreateAccount</see> method.</para>		
        /// </remarks>
        /// <returns>The total number of bytes written to the buffer.</returns>
        // Suppressed CA1726: "LoginAccount" is the canonical name from Battle.net.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Login")]
        public int LoginAccount(byte[] buffer, int startIndex, int totalLength)
        {
            MemoryStream ms = new MemoryStream(buffer, startIndex, totalLength, true);
            return LoginAccount(ms);
        }
        #endregion
        #region account creation
        /// <summary>
        /// Adds the account creation information (for SID_AUTH_ACCOUNTCREATE)
        /// to the specified stream at the current location.
        /// </summary>
        /// <param name="stream">The stream to modify.</param>
        /// <exception cref="IOException">Thrown if the stream does 
        /// not have enough space to add the account creation information.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the object has not 
        /// yet been initialized.</exception>
        /// <remarks>
        /// <para>The writeable length of the stream must be at least 65 bytes plus the length of the user name.</para>
        /// <para>This method must be called first if you are creating a new account.</para>
        /// </remarks>
        /// <returns>The total number of bytes written to the stream.</returns>
        public int CreateAccount(Stream stream)
        {
            if ((stream.Position + 65 + userNameAscii.Length) > stream.Length)
                throw new IOException(Resources.nlsAcctCreateSpace);

            byte[] clientSalt = new byte[32];
            s_rand.GetNonZeroBytes(clientSalt);

            CalculateVerifier(clientSalt);

            stream.Write(EnsureArrayLength(clientSalt, 32), 0, 32);
            stream.Write(ReverseArray(EnsureArrayLength(verifier.GetBytes(), 32)), 0, 32);
            stream.Write(userNameAscii, 0, userNameAscii.Length);
            stream.WriteByte(0);

            return 65 + userNameAscii.Length;
        }

        /// <summary>
        /// Adds the account creation information (for SID_AUTH_ACCOUNTCREATE)
        /// to the specified packet.
        /// </summary>
        /// <param name="acctPacket">The packet to which to add the account creation information.</param>
        /// <exception cref="InvalidOperationException">Thrown if the object has not 
        /// yet been initialized.</exception>
        /// <remarks>
        /// <para>This method must be called first if you are creating a new account.</para>
        /// </remarks>
        /// <returns>The total number of bytes written to the buffer.</returns>
        public int CreateAccount(DataBuffer acctPacket)
        {
            byte[] temp = new byte[65 + userName.Length];
            int len = CreateAccount(temp, 0, temp.Length);
            acctPacket.InsertByteArray(temp);
            return len;
        }

        /// <summary>
        /// Adds the account creation information (for SID_AUTH_ACCOUNTCREATE)
        /// to the specified buffer at the specified location.
        /// </summary>
        /// <param name="buffer">The buffer to modify.</param>
        /// <param name="startIndex">The starting index at which to 
        /// modify the buffer.</param>
        /// <param name="totalLength">The total number of bytes from 
        /// the starting index of the buffer that may be modified.</param>
        /// <exception cref="IOException">Thrown if the buffer does 
        /// not have enough space to add the account creation information.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the object has not 
        /// yet been initialized.</exception>
        /// <remarks>
        /// <para>The writeable length of the stream must be at least 65 bytes plus the length of the user name.</para>
        /// <para>This method must be called first if you are creating a new account.</para>
        /// </remarks>
        /// <returns>The total number of bytes written to the buffer.</returns>
        public int CreateAccount(byte[] buffer, int startIndex, int totalLength)
        {
            MemoryStream ms = new MemoryStream(buffer, startIndex, totalLength, true);
            return CreateAccount(ms);
        }
        #endregion
        #region warcraft 3 server verifier (static)
        /// <summary>
        /// Validates a Warcraft III server signature.
        /// </summary>
        /// <param name="serverSignature">The server signature from 
        /// Battle.net's SID_AUTH_INFO message.</param>
        /// <param name="ipAddress">The IPv4 address of the server
        /// currently connected-to.</param>
        /// <returns><b>True</b> if the signature matches; 
        /// otherwise <b>false</b>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if 
        /// the server signature is not exactly 128 bytes.</exception>
        public static bool ValidateServerSignature(byte[] serverSignature,
            byte[] ipAddress)
        {
            // code based on iago's code.

            if (serverSignature.Length != 128)
                throw new ArgumentOutOfRangeException(Resources.nlsSrvSig128);

            BigInteger key = new BigInteger(new byte[] { 0, 1, 0, 1 } /* ReverseArray(new BigInteger((ulong)SignatureKey).GetBytes()) */);
            BigInteger mod = new BigInteger(ServerModulus, 16);
            BigInteger sig = new BigInteger(ReverseArray(serverSignature));

            byte[] result = sig.ModPow(key, mod).GetBytes();
            BigInteger res = new BigInteger(ReverseArray(result));

            MemoryStream ms_res = new MemoryStream(result.Length);
            ms_res.Write(ipAddress, 0, 4);
            for (int i = 4; i < result.Length; i++)
                ms_res.WriteByte(0xbb);

            ms_res.Seek(-1, SeekOrigin.Current);
            ms_res.WriteByte(0x0b);

            BigInteger cor_res = new BigInteger(ms_res.GetBuffer());
            ms_res.Close();

            return cor_res.Equals(res);
        }
        #endregion 
        #region private methods
        private void CalculateVerifier(byte[] serverSalt)
        {
            string unpwexpr = String.Concat(
                userName.ToUpper(CultureInfo.InvariantCulture), ":", password.ToUpper(CultureInfo.InvariantCulture)
                );

            byte[] unpw_bytes = Encoding.ASCII.GetBytes(unpwexpr);
            byte[] hash1 = s_sha.ComputeHash(unpw_bytes);

            byte[] unpw_salt_bytes = new byte[serverSalt.Length + hash1.Length]; // should be 52
            Array.Copy(serverSalt, unpw_salt_bytes, serverSalt.Length);
            Array.Copy(hash1, 0, unpw_salt_bytes, serverSalt.Length, hash1.Length);

            byte[] hash2 = s_sha.ComputeHash(unpw_salt_bytes);

            lock (this)
            {
                //this.salt = serverSalt;
                x = new BigInteger(ReverseArray(hash2));
                //x = new BigInteger(hash2);
                verifier = s_generator.ModPow(x, s_modulus);
            }
        }


        private void CalculateM1(byte[] saltFromServer, byte[] issuedServerKey)
        {
            BigInteger local_B = new BigInteger(ReverseArray(issuedServerKey));
            //BigInteger local_B = new BigInteger(serverKey);

            // first calculate u.
            byte[] u_sha = s_sha.ComputeHash(issuedServerKey);
            BigInteger u = new BigInteger(u_sha, 4);

            if (verifier == null)
                CalculateVerifier(saltFromServer);

            // then we need to calculate S.
            BigInteger local_S = ((s_modulus + local_B - verifier) % s_modulus);
            local_S = local_S.ModPow((a + (u * x)), s_modulus);
            byte[] bytes_s = EnsureArrayLength(ReverseArray(local_S.GetBytes()), 32);
            //byte[] bytes_s = local_S.GetBytes();

            // now K.  yeah, this is weird.
            byte[] even_s = new byte[16];
            byte[] odds_s = new byte[16];
            for (int i = 0, j = 0; i < bytes_s.Length; i += 2, j++)
            {
                even_s[j] = bytes_s[i];
                odds_s[j] = bytes_s[i + 1];
            }
            byte[] even_hash = s_sha.ComputeHash(even_s);
            byte[] odds_hash = s_sha.ComputeHash(odds_s);
            byte[] local_k = new byte[40];
            for (int i = 0; i < local_k.Length; i++)
            {
                if ((i & 1) == 0)
                {
                    local_k[i] = even_hash[i / 2];
                }
                else
                {
                    local_k[i] = odds_hash[i / 2];
                }
            }

            // finally, m1.
            BigInteger sha_g = new BigInteger(s_sha.ComputeHash(ReverseArray(s_generator.GetBytes())));
            BigInteger sha_n = new BigInteger(s_sha.ComputeHash(ReverseArray(s_modulus.GetBytes())));
            BigInteger g_xor_n = sha_g ^ sha_n;

            MemoryStream ms = new MemoryStream(40 + saltFromServer.Length + A.GetBytes().Length + issuedServerKey.Length + local_k.Length);
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(g_xor_n.GetBytes());
            bw.Write(s_sha.ComputeHash(Encoding.ASCII.GetBytes(userName.ToUpper(CultureInfo.InvariantCulture))));
            bw.Write(saltFromServer);
            bw.Write(EnsureArrayLength(A.GetBytes(), 32));
#if DEBUG
            if (A.GetBytes().Length < 32)
                DataFormatter.WriteToTrace(A.GetBytes(), "A length less than 32 bytes");
#endif
            bw.Write(issuedServerKey);
            bw.Write(local_k);

            byte[] m1_data = ms.GetBuffer();
            ms.Close();
            byte[] m1_hash = s_sha.ComputeHash(m1_data);

            lock (this)
            {
                this.k = local_k;
                //this.salt = saltFromServer;
                //this.serverKey = issuedServerKey;
                //this.S = local_S;
                m1 = new BigInteger(m1_hash);
            }
        }


        private static byte[] ReverseArray(byte[] array)
        {
            byte[] res = new byte[array.Length];

            for (int i = 0; i < array.Length; ++i)
                res[i] = array[array.Length - 1 - i];
            return res;
        }

        private static byte[] EnsureArrayLength(byte[] array, int minSize)
        {
            if (array.Length < minSize)
            {
                byte[] temp = new byte[minSize];
                Buffer.BlockCopy(array, 0, temp, minSize - array.Length, array.Length);
                array = temp;
            }
            return array;
        }
        #endregion
    }
}
