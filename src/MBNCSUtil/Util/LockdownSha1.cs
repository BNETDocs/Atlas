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
  
  
Based on code copyright (c) 2007 by Robert O'Neal and/or x86.  See LICENSE.txt for 
details on these code sections.
 
	
See LICENSE.TXT that should have accompanied this software for full terms and 
conditions.

*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

namespace MBNCSUtil.Util
{
    internal static class LockdownSha1
    {
        private static readonly byte[] MysteryBuffer = new byte[] { 
            0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 
                                                                  };

        #region context
        internal class Context
        {
            /*
typedef struct
{
	int bitlen[2];
	int state[32];
} LD_SHA1_CTX;
             */
            public int[] bitlen = new int[2];
            public int[] state = new int[32];
        }
        #endregion
        #region helpers
        static uint ROTL32(uint value, int shift)
        {
            uint result = 0;
            result = (value << shift) | (value >> (32 - (shift & 0x1f)));
#if DEBUG
           // Trace.WriteLine(result.ToString("x8"), "ROTL value (unsigned)");
#endif
            return result;
        }

        static int ROTL32(int value, int shift)
        {
//            int result = 0;
//            result = (value << shift) | (value >> (32 - (shift & 0x1f)));
//#if DEBUG
//            Trace.WriteLine(result.ToString("x8"), "ROTL value");
//#endif
//            return result;
            return unchecked((int)ROTL32((uint)value, shift));
        }

        //static void Tweedle(ref int rotater, int bitwise, int bitwise2, int bitwise3, int[] buffer, int index, ref int result)
        //{
        //    int adder = buffer[index];
        //    result = result + (((ROTL32(bitwise3, 5)) + ((~(rotater) & bitwise2) | (rotater & bitwise))) + adder + 0x5a827999);
        //    buffer[index] = 0;
        //    rotater = ROTL32(rotater, 0x1e);
        //}

        //static void Tweedle(ref int rotater, int bitwise, int bitwise2, int bitwise3, ref int adder, ref int result)
        //{
        //    result = result + (((ROTL32(bitwise3, 5)) + ((~(rotater) & bitwise2) | (rotater & bitwise))) + adder + 0x5a827999);
        //    adder = 0;
        //    rotater = ROTL32(rotater, 0x1e);
        //}

        static unsafe void Tweedle(ref int rotater, int bitwise, int bitwise2, int bitwise3, int* adder, ref int result)
        {
            result = result + (((ROTL32(bitwise3, 5)) + ((~(rotater) & bitwise2) | (rotater & bitwise))) + *adder + 0x5a827999);
            *adder = 0;
            rotater = ROTL32(rotater, 0x1e);
        }

        //static void Twitter(ref int rotater, int bitwise, int rotater2, int bitwise2, int[] buffer, int index, ref int result)
        //{
        //    int rotater3 = buffer[index];
        //    result = result + ((((bitwise2 | bitwise) & rotater) | (bitwise2 & bitwise)) + ((ROTL32(rotater2, 5)) + rotater3) - 0x70e44324);
        //    buffer[index] = 0;
        //    rotater = ROTL32(rotater, 0x1e);
        //}

        //static void Twitter(ref int rotater, int bitwise, int rotater2, int bitwise2, ref int rotater3, ref int result)
        //{
        //    result = result + ((((bitwise2 | bitwise) & rotater) | (bitwise2 & bitwise)) + ((ROTL32(rotater2, 5)) + rotater3) - 0x70e44324);
        //    rotater3 = 0;
        //    rotater = ROTL32(rotater, 0x1e);
        //}

        static unsafe void Twitter(ref int rotater, int bitwise, int rotater2, int bitwise2, int* rotater3, ref int result)
        {
            result = result + ((((bitwise2 | bitwise) & rotater) | (bitwise2 & bitwise)) + ((ROTL32(rotater2, 5)) + *rotater3) - 0x70e44324);
            *rotater3 = 0;
            rotater = ROTL32(rotater, 0x1e);
        }
        #endregion
        #region transform
        //static void Sha1Transform(int[] data, int index, int[] state)
        //{
        //    int a, b, c, d, e, f, g, h, m, n;
        //    int i;

        //    int[] buf = new int[80];
        //    Buffer.BlockCopy(data, index, buf, 0, 0x40);

        //    for (i = 0; i < 0x40; i++)
        //    {
        //        buf[i + 16] = ROTL32(buf[i + 13] ^ buf[i + 8] ^ buf[i] ^ buf[i + 2], 1);
        //    }

        //    m = state[0];
        //    b = state[1];
        //    c = state[2];
        //    n = state[3];
        //    e = state[4];

        //    for (i = 0; i < 20; i += 5)
        //    {
        //        Tweedle(ref b, c, n, m, buf, (0 + i), ref e);
        //        Tweedle(ref m, b, c, e, buf, (1 + i), ref n);
        //        Tweedle(ref e, m, b, n, buf, (2 + i), ref c);
        //        Tweedle(ref n, e, m, c, buf, (3 + i), ref b);
        //        Tweedle(ref c, n, e, b, buf, (4 + i), ref m);
        //    }

        //    f = m;
        //    d = n;

        //    for (i = 0x14; i < 0x28; i += 5)
        //    {
        //        g = buf[i] + ROTL32(f, 5) + (d ^ c ^ b);
        //        d = d + ROTL32(g + e + 0x6ed9eba1, 5) + (c ^ ROTL32(b, 0x1e) ^ f) + buf[i + 1] + 0x6ed9ba1;
        //        c = c + ROTL32(d, 5) + ((g + e + 0x6ed9eba1) ^ ROTL32(b, 0x1e) ^ ROTL32(f, 0x1e)) + buf[i + 2] + 0x6ed9eba1;
        //        e = ROTL32(g + e + 0x6ed9eba1, 0x1e);
        //        b = ROTL32(b, 0x1e) + ROTL32(c, 5) + (e ^ d ^ ROTL32(f, 0x1e)) + buf[i + 3] + 0x6ed9eba1;
        //        d = ROTL32(d, 0x1e);
        //        f = ROTL32(f, 0x1e) + ROTL32(b, 5) + (e ^ d ^ c) + buf[i + 4] + 0x6ed9eba1;
        //        c = ROTL32(c, 0x1e);

        //        Array.Clear(buf, 0, 20);
        //    }

        //    m = f;
        //    n = d;

        //    for (i = 0x28; i < 0x3c; i += 5)
        //    {
        //        Twitter(ref b, n, m, c, buf, (i + 0), ref e);
        //        Twitter(ref m, c, e, b, buf, (i + 1), ref n);
        //        Twitter(ref e, b, n, m, buf, (i + 2), ref c);
        //        Twitter(ref n, m, c, e, buf, (i + 3), ref b);
        //        Twitter(ref c, e, b, n, buf, (i + 4), ref m);
        //    }

        //    f = m;
        //    a = m;
        //    d = n;

        //    for (i = 0x3c; i < 0x50; i += 5)
        //    {
        //        g = ROTL32(a, 5) + (d ^ c ^ b) + buf[i + 0] + e - 0x359d3e2a;
        //        b = ROTL32(b, 0x1e);
        //        e = g;
        //        d = (c ^ b ^ a) + buf[i + 1] + d + ROTL32(g, 5) - 0x359d3e2a;
        //        a = ROTL32(a, 0x1e);
        //        g = ROTL32(d, 5);
        //        g = (e ^ b & a) + buf[i + 2] + c + g - 0x359d3e2a;
        //        e = ROTL32(e, 0x1e);
        //        c = g;
        //        g = ROTL32(g, 5) + (e ^ d ^ a) + buf[i + 3] + b - 0x359d3e2a;
        //        d = ROTL32(d, 0x1e);
        //        h = (e ^ d ^ c) + buf[i + 4];
        //        b = g; 
        //        g = ROTL32(g, 5);
        //        c = ROTL32(c, 0x1e);
        //        a = (h + a) + g - 0x359d3e2a;

        //        Array.Clear(buf, i, 5);
        //    }

        //    state[0] = state[0] + a;
        //    state[1] = state[1] + b;
        //    state[2] = state[2] + c;
        //    state[3] = state[3] + d;
        //    state[4] = state[4] + e;
        //}

        static unsafe void Sha1Transform(int* data, int* state)
        {
            int a, b, c, d, e, f, g, h, m, n;
            int i;

            int* buf = stackalloc int[80];
            Native.Memcpy((void*)buf, (void*)data, 0x40);

            for (i = 0; i < 0x40; i++)
            {
                buf[i + 16] = ROTL32(buf[i + 13] ^ buf[i + 8] ^ buf[i] ^ buf[i + 2], 1);
                if (buf[i + 16] == -1)
                    buf[i + 16] = (int)ROTL32((uint)(buf[i + 13] ^ buf[i + 8] ^ buf[i] ^ buf[i + 2]), 1);
            }

            m = state[0];
            b = state[1];
            c = state[2];
            n = state[3];
            e = state[4];

            for (i = 0; i < 20; i += 5)
            {
                Tweedle(ref b, c, n, m, &buf[0 + i], ref e);
                Tweedle(ref m, b, c, e, &buf[1 + i], ref n);
                Tweedle(ref e, m, b, n, &buf[2 + i], ref c);
                Tweedle(ref n, e, m, c, &buf[3 + i], ref b);
                Tweedle(ref c, n, e, b, &buf[4 + i], ref m);
            }

            f = m;
            d = n;

            for (i = 0x14; i < 0x28; i += 5)
            {
                g = buf[i] + ROTL32(f, 5) + (d ^ c ^ b);
                d = d + ROTL32(g + e + 0x6ed9eba1, 5) + (c ^ ROTL32(b, 0x1e) ^ f) + buf[i + 1] + 0x6ed9eba1;
                c = c + ROTL32(d, 5) + ((g + e + 0x6ed9eba1) ^ ROTL32(b, 0x1e) ^ ROTL32(f, 0x1e)) + buf[i + 2] + 0x6ed9eba1;
                e = ROTL32(g + e + 0x6ed9eba1, 0x1e);
                b = ROTL32(b, 0x1e) + ROTL32(c, 5) + (e ^ d ^ ROTL32(f, 0x1e)) + buf[i + 3] + 0x6ed9eba1;
                d = ROTL32(d, 0x1e);
                f = ROTL32(f, 0x1e) + ROTL32(b, 5) + (e ^ d ^ c) + buf[i + 4] + 0x6ed9eba1;
                c = ROTL32(c, 0x1e);

                Native.Memset((void*)buf, 0, 20);
            }

            m = f;
            n = d;

            for (i = 0x28; i < 0x3c; i += 5)
            {
                Twitter(ref b, n, m, c, &buf[i + 0], ref e);
                Twitter(ref m, c, e, b, &buf[i + 1], ref n);
                Twitter(ref e, b, n, m, &buf[i + 2], ref c);
                Twitter(ref n, m, c, e, &buf[i + 3], ref b);
                Twitter(ref c, e, b, n, &buf[i + 4], ref m);
            }

            f = m;
            a = m;
            d = n;

            for (i = 0x3c; i < 0x50; i += 5)
            {
                g = ROTL32(a, 5) + (d ^ c ^ b) + buf[i + 0] + e - 0x359d3e2a;
                b = ROTL32(b, 0x1e);
                e = g;
                d = (c ^ b ^ a) + buf[i + 1] + d + ROTL32(g, 5) - 0x359d3e2a;
                a = ROTL32(a, 0x1e);
                g = ROTL32(d, 5);
                g = (e ^ b ^ a) + buf[i + 2] + c + g - 0x359d3e2a;
                e = ROTL32(e, 0x1e);
                c = g;
                g = ROTL32(g, 5) + (e ^ d ^ a) + buf[i + 3] + b - 0x359d3e2a;
                d = ROTL32(d, 0x1e);
                h = (e ^ d ^ c) + buf[i + 4];
                b = g;
                g = ROTL32(g, 5);
                c = ROTL32(c, 0x1e);
                a = (h + a) + g - 0x359d3e2a;

                Native.Memset((void*)buf, 0, 20);
            }

            state[0] = state[0] + a;
            state[1] = state[1] + b;
            state[2] = state[2] + c;
            state[3] = state[3] + d;
            state[4] = state[4] + e;
        }
        #endregion

        public static LockdownSha1.Context Init()
        {
            Context c = new Context();
            c.state[0] = 0x67452301;
            c.state[1] = unchecked((int)0xefcdab89);
            c.state[2] = unchecked((int)0x98badcfe);
            c.state[3] = 0x10325476;
            c.state[4] = unchecked((int)0xc3d2e1f0);
            return c;
        }

        public static unsafe void Update(Context ctx, byte[] data, int len)
        {
            fixed (byte* ptr = data)
            {
                Update(ctx, ptr, len);
            }
        }

        public static unsafe void Update(Context ctx, byte* data, int len)
        {
            fixed (int* bitlen = ctx.bitlen)
            fixed (int* istate = ctx.state)
            {
                byte* state = (byte*)istate;

                int a, b, c, i;

                c = len >> 29;
                b = len << 3;

                a = (bitlen[0] / 8) & 0x3f;

                // check for overflow
                if (((bitlen[0] + b) < bitlen[0]) || (bitlen[0] + b < b))
                    bitlen[1]++;

                bitlen[0] = bitlen[0] + b;
                bitlen[1] = bitlen[1] + c;

                len += a;
                data -= a;

                if (len >= 0x40)
                {
                    if (a != 0)
                    {
                        while (a < 0x40)
                        {
                            state[0x14 + a] = data[a];
                            a++;
                        }

                        Sha1Transform((int*)(state + 0x14), (int*)state);
                        len -= 0x40;
                        data += 0x40;
                        a = 0;
                    }

                    if (len >= 0x40)
                    {
                        b = len;
                        for (i = 0; i < b / 0x40; i++)
                        {
                            Sha1Transform((int*)data, (int*)state);
                            len -= 0x40;
                            data += 0x40;
                        }
                    }
                }

                for (; a < len; a++)
                {
                    state[a + 0x1c - 8] = data[a];
                }

                return;
            }
        }

        public static unsafe void Final(Context ctx, out byte[] hash)
        {
            int i;
            int* vars = stackalloc int[2];

            vars[0] = ctx.bitlen[0];
            vars[1] = ctx.bitlen[1];

            Update(ctx, MysteryBuffer, (-((ctx.bitlen[0] >> 3 | ctx.bitlen[1] << 29) + 9) & 0x3f) + 1);
            Update(ctx, (byte*)vars, 8);

            hash = new byte[20];

            fixed (byte* pbHash = hash)
            {
                int* pdHash = (int*)pbHash;
                for (i = 0; i < 5; i++)
                {
                    pdHash[i] = ctx.state[i];
                }
            }
        }

        public static unsafe void Final(Context ctx, HeapPtr ptr)
        {
            byte[] hashResult;
            Final(ctx, out hashResult);
            ptr.MarshalData(hashResult);
        }

        public static unsafe void Pad(Context ctx, int amount)
        {
            byte* emptybuffer = stackalloc byte[0x1000];
            while (amount > 0x1000)
            {
                Update(ctx, emptybuffer, 0x1000);
                amount -= 0x1000;
            }
            Update(ctx, emptybuffer, amount);
        }

        public static unsafe void HashFile(Context ctx, string filename)
        {
            byte[] fileData = File.ReadAllBytes(filename);
            Update(ctx, fileData, fileData.Length);
        }
    }
}
