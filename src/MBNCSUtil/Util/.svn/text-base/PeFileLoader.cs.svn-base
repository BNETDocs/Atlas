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
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace MBNCSUtil.Util
{
    internal class PeFileLoader : IDisposable
    {
        private const short DOS_SIGNATURE = 0x5A4D;
        private const int NT_SIGNATURE = 0x00004550;
        private const int IMAGE_SIZEOF_BASE_RELOCATION = 8;
        private const int IMAGE_REL_BASED_HIGHLOW = 3;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr m_ptr;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr m_baseAddrPtr;

        public unsafe byte* BaseAddress
        {
            get { return (byte*)m_baseAddrPtr.ToPointer(); }
        }

        public unsafe PeFileLoader(string path)
        {
            byte[] file = File.ReadAllBytes(path);
            m_ptr = Marshal.AllocHGlobal(file.Length);
            Marshal.Copy(file, 0, m_ptr, file.Length);
            file = null;

            PeFileReader.DosImageHeader* dosheader;
            PeFileReader.NtHeaders* ntheader;

            byte* pFile = (byte*)m_ptr.ToPointer();
            dosheader = (PeFileReader.DosImageHeader*)pFile;

            if (dosheader->e_magic != DOS_SIGNATURE)
            {
                Marshal.FreeHGlobal(m_ptr);
                m_ptr = IntPtr.Zero;
                throw new FileLoadException("Invalid DOS signature.");
            }

            ntheader = (PeFileReader.NtHeaders*)(pFile + dosheader->e_lfanew);
            if (ntheader->Signature != NT_SIGNATURE)
            {
                Marshal.FreeHGlobal(m_ptr);
                m_ptr = IntPtr.Zero;
                throw new FileLoadException("Invalid NT signature.");
            }

            m_baseAddrPtr = Marshal.AllocHGlobal(ntheader->OptionalHeader.SizeOfImage);
            byte* baseaddr = (byte*)m_baseAddrPtr.ToPointer();
            Native.Memcpy((void*)baseaddr, (void*)dosheader, dosheader->e_lfanew + ntheader->OptionalHeader.SizeOfHeaders);
            int imageBase, relocOffset;
            imageBase = ntheader->OptionalHeader.ImageBase;
            CopySections(pFile, ntheader, baseaddr);
            relocOffset = (int)(baseaddr - imageBase);

            if (relocOffset != 0)
            {
                PerformBaseReloc(baseaddr, ntheader, relocOffset);
            }
        }

        //private unsafe PeFileReader.ImageSectionHeader* GetSection(byte* data, byte* name)
        //{
        //    byte* baseaddr;
        //    int i;
        //    PeFileReader.DosImageHeader* dosheader;
        //    PeFileReader.NtHeaders* ntheader;
        //    PeFileReader.ImageSectionHeader* section;

        //    baseaddr = data;
        //    dosheader = (PeFileReader.DosImageHeader*)baseaddr;
        //    ntheader = (PeFileReader.NtHeaders*)(baseaddr + dosheader->e_lfanew);
        //    // roughly, IMAGE_FIRST_SECTION macro.  0x18 is the offset of the optional header, plus size of optional header.
        //    section = (PeFileReader.ImageSectionHeader*)(((byte*)ntheader) + 0x18 + ntheader->SizeOfOptionalHeader);

        //    for (i = 0; i < ntheader->NumberOfSections; i++, section++)
        //    {
        //        if (!Native.Strcmp(&section->Name, name))
        //        {
        //            return section;
        //        }
        //    }

        //    return null;
        //}

        private static unsafe void PerformBaseReloc(byte* baseaddr, PeFileReader.NtHeaders* ntheader, int relocOffset)
        {
            int i;
            PeFileReader.NtHeaders.ImageDataDirectory* directory;
            directory = (PeFileReader.NtHeaders.ImageDataDirectory*)&ntheader->OptionalHeader.IDD5;

            if (directory->Size > 0)
            {
                PeFileReader.ImageBaseRelocation* relocation = (PeFileReader.ImageBaseRelocation*)(baseaddr + directory->VirtualAddress);
                for (; relocation->VirtualAddress > 0; )
                {
                    byte* dest = (byte*)(baseaddr + relocation->VirtualAddress);
                    ushort* relocInfo = (ushort*)((byte*)relocation + IMAGE_SIZEOF_BASE_RELOCATION);
                    for (i = 0; i < ((relocation->SizeOfBlock - IMAGE_SIZEOF_BASE_RELOCATION) / 2); i++, relocInfo++)
                    {
                        uint* patch_addr;
                        int type, offset;
                        type = *relocInfo >> 12;
                        offset = *relocInfo & 0xfff;
                        if (type == IMAGE_REL_BASED_HIGHLOW)
                        {
                            patch_addr = (uint*)(dest + offset);
                            *patch_addr += unchecked((uint)relocOffset);
                        }
                    }

                    relocation = (PeFileReader.ImageBaseRelocation*)(((uint)relocation) + relocation->SizeOfBlock);
                }
            }
        }

        private static unsafe void CopySections(byte* data, PeFileReader.NtHeaders* header, byte* baseaddr)
        {
            int i, size;
            byte* dest;
            // roughly, IMAGE_FIRST_SECTION macro.  0x18 is the offset of the optional header, plus size of optional header.
            PeFileReader.ImageSectionHeader* section = (PeFileReader.ImageSectionHeader*)(((byte*)header) + 0x18 + header->SizeOfOptionalHeader);
            for (i = 0; i < header->NumberOfSections; i++, section++)
            {
                if (section->SizeOfRawData == 0)
                {
                    size = header->OptionalHeader.SectionAlignment;
                    if (size > 0)
                    {
                        dest = (byte*)(baseaddr + section->VirtualAddress);
                        section->PhysicalAddress = (int)dest;
                        Native.Memset(dest, 0, size);
                    }
                }
                else
                {
                    dest = (byte*)(baseaddr + section->VirtualAddress);
                    Native.Memcpy(dest, data + section->PointerToRawData, section->SizeOfRawData);
                    section->PhysicalAddress = (int)dest;
                }
            }
        }

        public static int GetVersion(string filename)
        {
            FileVersionInfo fve = FileVersionInfo.GetVersionInfo(filename);

            int version = (int)(((fve.ProductMajorPart & 0xff) << 24) | ((fve.ProductMinorPart & 0xff) << 16) |
                ((fve.ProductBuildPart & 0xff) << 8) | (fve.ProductPrivatePart & 0xff));
            return version;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(m_ptr);
                m_ptr = IntPtr.Zero;
            }

            if (m_baseAddrPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(m_baseAddrPtr);
                m_baseAddrPtr = IntPtr.Zero;
            }

            if (disposing)
            {

            }
        }

        ~PeFileLoader()
        {
            Dispose(false);
        }

        #endregion
    }
}
