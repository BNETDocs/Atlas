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

namespace MBNCSUtil.Util
{
    internal static class Native
    {
        internal unsafe static void Memcpy(void* target, void* src, int byteLength)
        {
            if ((byteLength % 4) == 0)
            {
                int* tgt = (int*)target;
                int* sr = (int*)src;
                byteLength /= 4;
                for (int i = 0; i < byteLength; i++)
                {
                    *(tgt + i) = *(sr + i);
                }
            }
            else
            {
                byte* tgt = (byte*)target;
                byte* sr = (byte*)src;
                for (int i = 0; i < byteLength; i++)
                {
                    *(tgt + i) = *(sr + i);
                }
            }
        }

        internal unsafe static void Memset(void* target, byte value, int byteLength)
        {
            if ((byteLength % 4) == 0)
            {
                int* tgt = (int*)target;
                int val = value | (value << 8) | (value << 16) | (value << 24);
                byteLength /= 4;
                for (int i = 0; i < byteLength; i++)
                {
                    *(tgt + i) = val;
                }
            }
            else
            {
                byte* tgt = (byte*)target;
                for (int i = 0; i < byteLength; i++)
                {
                    *(tgt + i) = value;
                }
            }
        }

        //internal unsafe static bool Strcmp(byte* strA, byte* strB)
        //{
        //    byte a, b;
        //    bool different = false;
        //    do
        //    {
        //        a = *strA;
        //        b = *strB;
        //        strA++;
        //        strB++;
        //        if (a != b)
        //        {
        //            different = true;
        //            break;
        //        }
        //    } while (a != 0 && b != 0);
        //    return different;
        //}

        //internal static unsafe int Strlen(byte* str)
        //{
        //    int i = 0;
        //    checked // checked causes OverflowException to be raised if i overflows at int.MaxValue.
        //    {
        //        while (str[i] != 0)
        //            i++;
        //    }

        //    return i;
        //}

        internal static unsafe byte* Memmove(byte* dest, byte* src, int byteCount)
        {
            using (HeapPtr ptr = new HeapPtr(byteCount, AllocMethod.HGlobal))
            {
                ptr.ReadData(src, byteCount);
                Memcpy((void*)dest, ptr.ToPointer(), byteCount);
                return dest;
            }
        }
    }
}
