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

namespace MBNCSUtil.Util
{
    internal static class LockdownCrev
    {
        private const int IMAGE_DIRECTORY_ENTRY_IMPORT = 1;
        private const int IMAGE_DIRECTORY_ENTRY_BASERELOC = 5;
        private const int IMAGE_SIZEOF_BASE_RELOCATION = 8;
        private const int IMAGE_REL_BASED_LOW = 2;
        private const int IMAGE_REL_BASED_HIGHLOW = 3;
        private const int IMAGE_REL_BASED_DIR64 = 10;

        #region seeds
        readonly static int[] seeds = new int[] { 
            unchecked((int)0xa1f3055a),
            0x5657124c,
            0x1780ab47,
            unchecked((int)0x80b3a410),
            unchecked((int)0xaf2179ea),
            0x0837b808,
            0x6f2516c6,
            unchecked((int)0xe3178148),
            0x0fcf90b6,
            unchecked((int)0xf2f09516),
            0x378d8d8c,
            0x07f8e083,
            unchecked((int)0xb0ee9741),
            0x7923c9af,
            unchecked((int)0xca11a05e),
            unchecked((int)0xd723c016),
            unchecked((int)0xfd545590),
            unchecked((int)0xfb600c2e),
            0x684c8785,
            0x58bede0b
        };
        #endregion

        /// <summary>
        /// Calculates the lockdown checkrevision.
        /// </summary>
        /// <param name="file1">The first game file</param>
        /// <param name="file2">The second game file</param>
        /// <param name="file3">The third game file</param>
        /// <param name="valueString">The value calculation string from the server</param>
        /// <param name="version">The version</param>
        /// <param name="checksum">The checksum</param>
        /// <param name="digest">The result</param>
        /// <param name="lockdownFile">The name of the lockdown file</param>
        /// <param name="imageDump">The path to the screen dump</param>
        /// <returns></returns>
        public static unsafe bool CheckRevision(string file1, string file2, string file3, byte[] valueString, ref int version, ref int checksum, out byte[] digest,
            string lockdownFile, string imageDump)
        {
            int returnIsValid = 1;
            int moduleOffset = 0;
            int seed, i;

            digest = null;
            
            int digit = GetDigit(lockdownFile);
            seed = seeds[digit];
            version = PeFileLoader.GetVersion(file1);

            // note that the HeapPtr can be implicitly cast to byte* and so therefore
            // can be used in method calls that would normally accept a byte* parameter.
            using (HeapPtr pSha1Buf = new HeapPtr(20, AllocMethod.HGlobal))
            using (HeapPtr pSha1Buf2 = new HeapPtr(20, AllocMethod.HGlobal))
            using (HeapPtr pValStr = new HeapPtr(valueString.Length, AllocMethod.HGlobal))
            using (HeapPtr pValStrEnc = new HeapPtr(valueString.Length, AllocMethod.HGlobal))
            using (HeapPtr pValStrBuffer1 = new HeapPtr(0x40, AllocMethod.HGlobal))
            using (HeapPtr pValStrBuffer2 = new HeapPtr(0x40, AllocMethod.HGlobal))
            using (HeapPtr pTempMem = new HeapPtr(16, AllocMethod.HGlobal)) 
            using (HeapPtr pDigest = new HeapPtr(17, AllocMethod.HGlobal))
            {
                pValStr.MarshalData(valueString);

                if (!ShuffleValueString(pValStr, valueString.Length, pValStrEnc))
                {
                    return false;
                }

                Native.Memset(pValStrBuffer1.ToPointer(), 0x36, 0x40);
                Native.Memset(pValStrBuffer2.ToPointer(), 0x5c, 0x40);

                byte* valuestrBuffer1 = pValStrBuffer1;
                byte* valuestrBuffer2 = pValStrBuffer2;
                byte* valueStrEncoded = pValStrEnc;

                for (i = 0; i < 0x10; i++)
                {
                    valuestrBuffer1[i] ^= valueStrEncoded[i];
                    valuestrBuffer2[i] ^= valueStrEncoded[i];
                }

                LockdownSha1.Context context = LockdownSha1.Init();
                LockdownSha1.Update(context, valuestrBuffer1, 0x40);

                if (!HashFile(context, lockdownFile, seed))
                    return false;

                if (!HashFile(context, file1, seed))
                    return false;

                if (!HashFile(context, file2, seed))
                    return false;

                if (!HashFile(context, file3, seed))
                    return false;

                LockdownSha1.HashFile(context, imageDump);

                LockdownSha1.Update(context, (byte*)&returnIsValid, 4);
                LockdownSha1.Update(context, (byte*)&moduleOffset, 4);

                LockdownSha1.Final(context, pSha1Buf);

                context = LockdownSha1.Init();
                LockdownSha1.Update(context, pValStrBuffer2, 0x40);
                LockdownSha1.Update(context, pSha1Buf, 0x14);
                LockdownSha1.Final(context, pSha1Buf2);

                checksum = *(int*)((byte*)pSha1Buf2);
                Native.Memmove(pTempMem, ((byte*)pSha1Buf2) + 4, 0x10);

                int valstrLen = 0xff;
                if (!CalculateDigest(pDigest, ref valstrLen, pTempMem))
                {
                    return false;
                }

                ((byte*)pDigest)[valstrLen] = 0x00;

                digest = new byte[valstrLen];
                Marshal.Copy(new IntPtr(pDigest.ToPointer()), digest, 0, valstrLen);
            }

            return true;
        }


        internal unsafe static int GetDigit(string filename)
        {
            int digit_1, digit_2;
            byte[] filenameBytes = Encoding.ASCII.GetBytes(filename);
            fixed (byte* pdigit_ptr = filenameBytes)
            {
                byte* digit_ptr = (byte*)(pdigit_ptr + filename.Length - 4);

                digit_1 = (int)(*(digit_ptr - 1) - '0');
                digit_2 = (int)(*(digit_ptr - 2) - '0');

                if (digit_2 == 1)
                    digit_1 += 10;
                if (digit_1 < 0 || digit_1 > 19)
                    return 0;
            }
            return digit_1;
        }

        internal unsafe static bool ShuffleValueString(byte* str, int len, byte* buffer)
        {
            int pos, i;
            byte adder, shifter;

            pos = 0;
            while (len != 0)
            {
                shifter = 0;
                for (i = 0; i < pos; i++)
                {
                    byte b = buffer[i];
                    buffer[i] = (byte)(shifter - buffer[i]);
                    shifter = unchecked((byte)((((b << 8) - b) + shifter) >> 8));
                }

                if (shifter != 0)
                {
                    if (pos >= 0x10) return false;

                    buffer[pos++] = shifter;
                }

                adder = (byte)(str[len - 1] - 1);
                for (i = 0; i < pos; i++)
                {
                    buffer[i] += adder;
                    adder = (buffer[i] < adder) ? (byte)1 : (byte)0;
                }

                if (adder != 0)
                {
                    if (pos >= 0x10)
                        return false;

                    buffer[pos++] = adder;
                }

                len--;
            }

            Native.Memset(buffer + pos, 0, 0x10 - pos);

            return true;
        }

        internal unsafe static bool CalculateDigest(byte* str1, ref int length, byte* str2)
        {
            int i, j;
            ushort word1, word2;
            byte* ptr_str1 = str1;
            bool ret = true;

            for (i = 0x10; i > 0; )
            {
                // skips over null bytes
                while (i > 0 && str2[i - 1] == 0)
                {
                    i--;
                }

                if (i != 0)
                {
                    word1 = 0;
                    for (j = i - 1; j >= 0; j--)
                    {
                        word2 = (ushort)(((ushort)(word1 << 8)) + (ushort)(str2[j] & 0xff));
                        WordShift(ref word2, ref word1);
                        str2[j] = (byte)word2;
                    }

                    if ((0x10 - i) >= length)
                        ret = false;
                    else
                        ptr_str1[0] = (byte)(word1 + 1);

                    ptr_str1++;
                }
            }
            length = (int)(ptr_str1 - str1);
            return ret;
        }

        internal static void WordShift(ref ushort str1, ref ushort str2)
        {
            // interim operations 
            ushort expr1 = (ushort)(str1 >> 8);
            ushort expr2 = (ushort)(str1 & 0xff);
            str2 = (ushort)((ushort)((expr1 + expr2) >> 8) + (ushort)((expr1 + expr2) & 0xff));

            expr1 = (ushort)(str2 & 0xff00);
            expr2 = (ushort)((str2 + 1) & 0xff);
            ushort expr3 = ((str2 & 0xff) != 0xff) ? (ushort)1 : (ushort)0;
            str2 = (ushort)(expr1 | (ushort)(expr2 - expr3));

            expr1 = (ushort)(str1 - str2);
            expr2 = ((((str1 - str2) >> 8) & 0xff) + 1) != 0 ? (ushort)0 : (ushort)0x100;
            expr3 = (ushort)(expr1 & 0xffff00ff);
            str1 = (ushort)(expr3 | expr2);

            expr1 = (ushort)(str1 & 0xffffff00);
            expr2 = (ushort)(-str1 & 0x000000ff);

            str1 = (ushort)(expr1 | expr2);
        }

        internal unsafe static bool HashFile(LockdownSha1.Context context, string filename, int seed)
        {
            int i, headersSize, sectionAlignment;
            byte* imageBase;
            PeFileLoader module;
            byte* firstSection;
            byte* baseaddr;

            //PeFileReader.NtHeaders.ImageDataDirectory* importDir;
            PeFileReader.NtHeaders.ImageDataDirectory* relocDir;
            PeFileReader.DosImageHeader* dosheader;
            PeFileReader.NtHeaders* ntheader;

            LockdownHeap heap = new LockdownHeap();

            module = new PeFileLoader(filename);

            baseaddr = module.BaseAddress;
            dosheader = (PeFileReader.DosImageHeader*)baseaddr;
            ntheader = (PeFileReader.NtHeaders*)(baseaddr + dosheader->e_lfanew);
            sectionAlignment = ntheader->OptionalHeader.SectionAlignment;
            imageBase = (byte*)ntheader->OptionalHeader.ImageBase;

            //importDir = (PeFileReader.NtHeaders.ImageDataDirectory*)&ntheader->OptionalHeader.IDD1;
            relocDir = (PeFileReader.NtHeaders.ImageDataDirectory*)&ntheader->OptionalHeader.IDD5;

            // roughly, IMAGE_FIRST_SECTION macro.  0x18 is the offset of the optional header, plus size of optional header.
            firstSection = (byte*)(((byte*)ntheader) + 0x18 + ntheader->SizeOfOptionalHeader);

            headersSize = ntheader->OptionalHeader.SizeOfHeaders;

            LockdownSha1.Update(context, baseaddr, headersSize);

            if (relocDir->VirtualAddress != 0 && relocDir->Size != 0)
            {
                if (!ProcessRelocDir(heap, baseaddr, relocDir))
                {
                    module.Dispose();
                    heap = null;
                    return false;
                }
            }

            for (i = 0; i < (ntheader->NumberOfSections); i++)
            {
                if (!ProcessSection(context, heap, baseaddr, imageBase, (PeFileReader.ImageSectionHeader*)(firstSection + (i * 0x28)), sectionAlignment, seed))
                {
                    module.Dispose();
                    heap = null;
                    return false;
                }
            }

            heap = null;
            module.Dispose();

            return true;
        }

        private static unsafe bool ProcessSection(LockdownSha1.Context context, LockdownHeap heap, byte* baseaddr, byte* preferredBaseAddr, 
            PeFileReader.ImageSectionHeader* section, int sectionAlignment, int seed)
        {
            int eax, virtualAddr, virtualSize, value;
            int index, bytes;
            int i;
            using (HeapPtr hpLockdownMem = heap.ToPointer())
            {
                int* lockdown_memory = (int*)hpLockdownMem.ToPointer();
                byte* allocatedMemoryBase;
                int lowerOffset = (int)baseaddr - (int)preferredBaseAddr;

                virtualAddr = section->VirtualAddress;
                virtualSize = section->VirtualSize;

                bytes = ((virtualSize + sectionAlignment - 1) & ~(sectionAlignment - 1)) - virtualSize;

                if (section->Characteristics < 0)
                {
                    LockdownSha1.Pad(context, bytes + virtualSize);
                }
                else
                {
                    index = 0;
                    if (heap.CurrentLength > 0)
                    {
                        for (i = 0; index < heap.CurrentLength && lockdown_memory[i] < virtualAddr; i += 4)
                        {
                            index++;
                        }
                    }

                    if (virtualSize > 0)
                    {
                        byte* startingMemory = baseaddr + virtualAddr;
                        byte* ptrMemory = startingMemory;
                        int memoryOffset = index * 4;
                        do
                        {
                            int sectionLength = (int)(startingMemory - ptrMemory + virtualSize);
                            eax = 0;
                            if (index < heap.CurrentLength)
                            {
                                eax = (int)(lockdown_memory[memoryOffset] + startingMemory - virtualAddr);
                            }
                            if (eax != 0)
                            {
                                eax -= (int)ptrMemory;
                                if (eax < sectionLength)
                                    sectionLength = eax;
                            }

                            if (sectionLength != 0)
                            {
                                LockdownSha1.Update(context, ptrMemory, sectionLength);
                                ptrMemory += sectionLength;
                            }
                            else
                            {
                                int* heapBuffer = stackalloc int[0x10];
                                Native.Memcpy((void*)heapBuffer, lockdown_memory + memoryOffset, 0x10);
                                value = (*(int*)ptrMemory - lowerOffset) ^ seed;
                                LockdownSha1.Update(context, (byte*)&value, 4);
                                ptrMemory += heapBuffer[1];
                                index++;
                                memoryOffset += 4;
                            }
                        } while ((ptrMemory - startingMemory) < virtualSize);
                    }

                    if (bytes > 0)
                    {
                        int i2 = 0;
                        IntPtr memoryAllocation = Marshal.AllocHGlobal(bytes);
                        allocatedMemoryBase = (byte*)memoryAllocation.ToPointer();
                        Native.Memset(allocatedMemoryBase, 0, bytes);
                        do
                        {
                            eax = 0;
                            if (index < heap.CurrentLength)
                            {
                                value = *(int*)(((byte*)lockdown_memory) + (index * 16));
                                eax = (int)(value - virtualSize - virtualAddr + allocatedMemoryBase);
                            }
                            bytes += i2;

                            if (eax != 0)
                            {
                                eax -= ((int*)allocatedMemoryBase)[i2 / 4];
                                if (eax < bytes)
                                    bytes = eax;
                            }

                            if (bytes != 0)
                            {
                                LockdownSha1.Update(context, &allocatedMemoryBase[i2], bytes);
                                i2 += bytes;
                            }
                        } while (i2 < bytes);

                        Marshal.FreeHGlobal(memoryAllocation);
                    }
                }

            }
            return true;
        }

        private unsafe static bool ProcessRelocDir(LockdownHeap heap, byte* baseaddr, PeFileReader.NtHeaders.ImageDataDirectory* relocDir)
        {
            int i, edx;
            int[] data = new int[4];

            PeFileReader.ImageBaseRelocation* relocation = (PeFileReader.ImageBaseRelocation*)(baseaddr + relocDir->VirtualAddress);
            for (; relocation->VirtualAddress > 0; )
            {
                short* relocInfo = (short*)((byte*)relocation + IMAGE_SIZEOF_BASE_RELOCATION);
                for (i = 0; i < ((relocation->SizeOfBlock - IMAGE_SIZEOF_BASE_RELOCATION) / 2); i++, relocInfo++)
                {
                    int type, offset;
                    type = *relocInfo >> 12;
                    offset = *relocInfo & 0xfff;
                    if (type != 0)
                    {
                        switch (type)
                        {
                            case IMAGE_REL_BASED_LOW:
                                edx = 2;
                                break;
                            case IMAGE_REL_BASED_HIGHLOW:
                                edx = 4;
                                break;
                            case IMAGE_REL_BASED_DIR64:
                                edx = 8;
                                break;
                            default:
                                return false;
                        }

                        data[0] = relocation->VirtualAddress + offset;
                        data[1] = edx;
                        data[2] = 2;
                        data[3] = type;

                        heap.Add(data);
                    }
                }

                relocation = (PeFileReader.ImageBaseRelocation*)(((byte*)relocation) + relocation->SizeOfBlock);
            }

            return true;
        }
    }
}
