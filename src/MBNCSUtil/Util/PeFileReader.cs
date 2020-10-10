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
using System.Collections.Specialized;

namespace MBNCSUtil.Util
{
    internal static class PeFileReader
    {
        //public PeFileReader(MemoryStream ms, BinaryReader br)
        //{
        //    DosHeader = new DosImageHeader(br);
        //    ms.Seek(DosHeader.e_lfanew, SeekOrigin.Begin);
        //    NtHeader = new NtHeaders(br);
        //}
        //public DosImageHeader DosHeader;
        //public NtHeaders NtHeader;

        #region _IMAGE_DOS_HEADER
        [StructLayout(LayoutKind.Sequential, Pack=1)]
        internal struct DosImageHeader
        {
            //public DosImageHeader(BinaryReader br)
            //{
            //    e_magic = br.ReadInt16();
            //    e_cblp = br.ReadInt16();
            //    e_cp = br.ReadInt16();
            //    e_crlc = br.ReadInt16();
            //    e_cparhdr = br.ReadInt16();
            //    e_minalloc = br.ReadInt16();
            //    e_maxalloc = br.ReadInt16();
            //    e_ss = br.ReadInt16();
            //    e_sp = br.ReadInt16();
            //    e_csum = br.ReadInt16();
            //    e_ip = br.ReadInt16();
            //    e_cs = br.ReadInt16();
            //    e_lfarlc = br.ReadInt16();
            //    e_ovno = br.ReadInt16();
            //    e_res0 = br.ReadInt16();
            //    e_res1 = br.ReadInt16();
            //    e_res2 = br.ReadInt16();
            //    e_res3 = br.ReadInt16();
            //    e_oemid = br.ReadInt16();
            //    e_oeminfo = br.ReadInt16();
            //    e_res2_0 = br.ReadInt16();
            //    e_res2_1 = br.ReadInt16();
            //    e_res2_2 = br.ReadInt16();
            //    e_res2_3 = br.ReadInt16();
            //    e_res2_4 = br.ReadInt16();
            //    e_res2_5 = br.ReadInt16();
            //    e_res2_6 = br.ReadInt16();
            //    e_res2_7 = br.ReadInt16();
            //    e_res2_8 = br.ReadInt16();
            //    e_res2_9 = br.ReadInt16();
            //    e_lfanew = br.ReadInt32();
            //}
            public short e_magic, e_cblp, e_cp, e_crlc, e_cparhdr, e_minalloc, e_maxalloc, e_ss, e_sp, e_csum, e_ip, e_cs, e_lfarlc, e_ovno;
            public short e_res0, e_res1, e_res2, e_res3;
            public short e_oemid, e_oeminfo;
            public short e_res2_0, e_res2_1, e_res2_2, e_res2_3, e_res2_4, e_res2_5, e_res2_6, e_res2_7, e_res2_8, e_res2_9;
            public int e_lfanew;
        }
        #endregion
        #region _IMAGE_NT_HEADERS
        [StructLayout(LayoutKind.Explicit)]
        internal struct NtHeaders
        {
            //public NtHeaders(BinaryReader br)
            //{
            //    Signature = br.ReadInt32();
            //    Machine = br.ReadInt16();
            //    NumberOfSections = br.ReadInt16();
            //    TimeDateStamp = br.ReadInt32();
            //    PointerToSymbolTable = br.ReadInt32();
            //    NumberOfSymbols = br.ReadInt32();
            //    SizeOfOptionalHeader = br.ReadInt16();
            //    Characteristics = br.ReadInt16();
            //    OptionalHeader = new OptionalHeader32(br);
            //    //Header32 = new OptionalHeader32();
            //    //Header64 = new OptionalHeader64();

            //    //if (IsImage32)                  // 32bit machine
            //    //    Header32 = new OptionalHeader32(br);
            //    //else                            // 64bit machine
            //    //    Header64 = new OptionalHeader64(br);
            //}

            #region _IMAGE_OPTIONAL_HEADER
            #region 32-bit
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            internal struct OptionalHeader32
            {
                //public OptionalHeader32(BinaryReader br)
                //{
                //    Magic = br.ReadInt16();
                //    MajorLinkerVersion = br.ReadByte();
                //    MinorLinkerVersion = br.ReadByte();
                //    SizeOfCode = br.ReadInt32();
                //    SizeOfInitializedData = br.ReadInt32();
                //    SizeOfUninitializedData = br.ReadInt32();
                //    AddressOfEntryPoint = br.ReadInt32();
                //    BaseOfCode = br.ReadInt32();
                //    BaseOfData = br.ReadInt32();
                //    ImageBase = br.ReadInt32();
                //    SectionAlignment = br.ReadInt32();
                //    FileAlignment = br.ReadInt32();
                //    MajorOSVersion = br.ReadInt16();
                //    MinorOSVersion = br.ReadInt16();
                //    MajorImageVersion = br.ReadInt16();
                //    MinorImageVersion = br.ReadInt16();
                //    MajorSubsystemVersion = br.ReadInt16();
                //    MinorSubsystemVersion = br.ReadInt16();
                //    Win32VersionValue = br.ReadInt32();
                //    SizeOfImage = br.ReadInt32();
                //    SizeOfHeaders = br.ReadInt32();
                //    CheckSum = br.ReadInt32();
                //    Subsystem = br.ReadInt16();
                //    DllCharacteristics = br.ReadInt16();
                //    SizeOfStackReserve = br.ReadInt32();
                //    SizeOfStackCommit = br.ReadInt32();
                //    SizeOfHeapReserve = br.ReadInt32();
                //    SizeOfHeapCommit = br.ReadInt32();
                //    LoaderFlags = br.ReadInt32();
                //    NumberOfRvaAndSizes = br.ReadInt32();
                //    IDD0 = new ImageDataDirectory(br);
                //    IDD1 = new ImageDataDirectory(br);
                //    IDD2 = new ImageDataDirectory(br);
                //    IDD3 = new ImageDataDirectory(br);
                //    IDD4 = new ImageDataDirectory(br);
                //    IDD5 = new ImageDataDirectory(br);
                //    IDD6 = new ImageDataDirectory(br);
                //    IDD7 = new ImageDataDirectory(br);
                //    IDD8 = new ImageDataDirectory(br);
                //    IDD9 = new ImageDataDirectory(br);
                //    IDDA = new ImageDataDirectory(br);
                //    IDDB = new ImageDataDirectory(br);
                //    IDDC = new ImageDataDirectory(br);
                //    IDDD = new ImageDataDirectory(br);
                //    IDDE = new ImageDataDirectory(br);
                //    IDDF = new ImageDataDirectory(br);
                //}
                #region std fields
                public short Magic;
                public byte MajorLinkerVersion, MinorLinkerVersion;
                public int SizeOfCode, SizeOfInitializedData, SizeOfUninitializedData, AddressOfEntryPoint, BaseOfCode, BaseOfData;
                #endregion
                #region nt fields
                public int ImageBase, SectionAlignment, FileAlignment;
                public short MajorOSVersion, MinorOSVersion, MajorImageVersion, MinorImageVersion, MajorSubsystemVersion, MinorSubsystemVersion;
                public int Win32VersionValue, SizeOfImage, SizeOfHeaders, CheckSum;
                public short Subsystem, DllCharacteristics;
                public int SizeOfStackReserve, SizeOfStackCommit, SizeOfHeapReserve, SizeOfHeapCommit, LoaderFlags, NumberOfRvaAndSizes;
                #endregion
                #region unrolled IMAGE_DATA_DIRECTORY entries
                public ImageDataDirectory IDD0, IDD1, IDD2, IDD3, IDD4, IDD5, IDD6, IDD7, IDD8, IDD9, IDDA, IDDB, IDDC, IDDD, IDDE, IDDF;
                #endregion
            }
            #endregion
            #region 64-bit
            //[StructLayout(LayoutKind.Sequential, Pack = 1)]
            //internal struct OptionalHeader64
            //{
            //    public OptionalHeader64(BinaryReader br)
            //    {
            //        Magic = br.ReadInt16();
            //        MajorLinkerVersion = br.ReadByte();
            //        MinorLinkerVersion = br.ReadByte();
            //        SizeOfCode = br.ReadInt32();
            //        SizeOfInitializedData = br.ReadInt32();
            //        SizeOfUninitializedData = br.ReadInt32();
            //        AddressOfEntryPoint = br.ReadInt32();
            //        BaseOfCode = br.ReadInt32();
            //        ImageBase = br.ReadInt64();
            //        SectionAlignment = br.ReadInt32();
            //        FileAlignment = br.ReadInt32();
            //        MajorOSVersion = br.ReadInt16();
            //        MinorOSVersion = br.ReadInt16();
            //        MajorImageVersion = br.ReadInt16();
            //        MinorImageVersion = br.ReadInt16();
            //        MajorSubsystemVersion = br.ReadInt16();
            //        MinorSubsystemVersion = br.ReadInt16();
            //        Win32VersionValue = br.ReadInt32();
            //        SizeOfImage = br.ReadInt32();
            //        SizeOfHeaders = br.ReadInt32();
            //        CheckSum = br.ReadInt32();
            //        Subsystem = br.ReadInt16();
            //        DllCharacteristics = br.ReadInt16();
            //        SizeOfStackReserve = br.ReadInt64();
            //        SizeOfStackCommit = br.ReadInt64();
            //        SizeOfHeapReserve = br.ReadInt64();
            //        SizeOfHeapCommit = br.ReadInt64();
            //        LoaderFlags = br.ReadInt32();
            //        NumberOfRvaAndSizes = br.ReadInt32();
            //        IDD0 = new ImageDataDirectory(br);
            //        IDD1 = new ImageDataDirectory(br);
            //        IDD2 = new ImageDataDirectory(br);
            //        IDD3 = new ImageDataDirectory(br);
            //        IDD4 = new ImageDataDirectory(br);
            //        IDD5 = new ImageDataDirectory(br);
            //        IDD6 = new ImageDataDirectory(br);
            //        IDD7 = new ImageDataDirectory(br);
            //        IDD8 = new ImageDataDirectory(br);
            //        IDD9 = new ImageDataDirectory(br);
            //        IDDA = new ImageDataDirectory(br);
            //        IDDB = new ImageDataDirectory(br);
            //        IDDC = new ImageDataDirectory(br);
            //        IDDD = new ImageDataDirectory(br);
            //        IDDE = new ImageDataDirectory(br);
            //        IDDF = new ImageDataDirectory(br);
            //    }
            //    #region std fields
            //    public short Magic;
            //    public byte MajorLinkerVersion, MinorLinkerVersion;
            //    public int SizeOfCode, SizeOfInitializedData, SizeOfUninitializedData, AddressOfEntryPoint, BaseOfCode;
            //    public long ImageBase;
            //    #endregion
            //    #region nt fields
            //    public int SectionAlignment, FileAlignment;
            //    public short MajorOSVersion, MinorOSVersion, MajorImageVersion, MinorImageVersion, MajorSubsystemVersion, MinorSubsystemVersion;
            //    public int Win32VersionValue, SizeOfImage, SizeOfHeaders, CheckSum;
            //    public short Subsystem, DllCharacteristics;
            //    public long SizeOfStackReserve, SizeOfStackCommit, SizeOfHeapReserve, SizeOfHeapCommit;
            //    public int LoaderFlags, NumberOfRvaAndSizes;
            //    #endregion
            //    #region unrolled IMAGE_DATA_DIRECTORY entries
            //    public ImageDataDirectory IDD0, IDD1, IDD2, IDD3, IDD4, IDD5, IDD6, IDD7, IDD8, IDD9, IDDA, IDDB, IDDC, IDDD, IDDE, IDDF;
            //    #endregion
            //}
            #endregion
            #region _IMAGE_DATA_DIRECTORY
            internal struct ImageDataDirectory
            {
                //public ImageDataDirectory(BinaryReader br)
                //{
                //    VirtualAddress = br.ReadInt32();
                //    Size = br.ReadInt32();
                //}
                public int VirtualAddress;
                public int Size;
            }
            #endregion
            #endregion
            [FieldOffset(0)]
            public int Signature;
            #region _IMAGE_FILE_HEADER fields
            [FieldOffset(4)]
            public short Machine;
            [FieldOffset(6)]
            public short NumberOfSections;
            [FieldOffset(8)]
            public int TimeDateStamp;
            [FieldOffset(12)]
            public int PointerToSymbolTable;
            [FieldOffset(0x10)]
            public int NumberOfSymbols;
            [FieldOffset(0x14)]
            public short SizeOfOptionalHeader;
            [FieldOffset(0x16)]
            public short Characteristics;
            #endregion
            [FieldOffset(0x18)]
            public OptionalHeader32 OptionalHeader;
            //[FieldOffset(0x18)]
            //public OptionalHeader32 Header32;
            //[FieldOffset(0x18)]
            //public OptionalHeader64 Header64;

            //public bool IsImage32 { get { return SizeOfOptionalHeader == 224; } }
        }
        #endregion

        #region _IMAGE_SECTION_HEADER
        [StructLayout(LayoutKind.Explicit)]
        public struct ImageSectionHeader
        {
            [FieldOffset(0)]
            public byte Name; // 8-byte array
            [FieldOffset(8)]
            public int PhysicalAddress;
            [FieldOffset(8)]
            public int VirtualSize;
            [FieldOffset(12)]
            public int VirtualAddress;
            [FieldOffset(0x10)]
            public int SizeOfRawData;
            [FieldOffset(0x14)]
            public int PointerToRawData;
            [FieldOffset(0x18)]
            public int PointerToRelocations;
            [FieldOffset(0x1c)]
            public int PointerToLinenumbers;
            [FieldOffset(0x20)]
            public short NumberOfRelocations;
            [FieldOffset(0x22)]
            public short NumberOfLinenumbers;
            [FieldOffset(0x24)]
            public int Characteristics;
        }
        #endregion

        #region _IMAGE_BASE_RELOCATION
        [StructLayout(LayoutKind.Sequential)]
        public struct ImageBaseRelocation
        {
            public int VirtualAddress;
            public int SizeOfBlock;
        }
        #endregion

        #region _IMAGE_RESOURCE_DIRECTORY_ENTRY
        [StructLayout(LayoutKind.Explicit)]
        public struct ImageResourceDirectoryEntry
        {
            /*
    union {
        struct {
            DWORD NameOffset:31;
            DWORD NameIsString:1;
        };
        DWORD   Name;
        WORD    Id;
    };
             */
            [MarshalAs(UnmanagedType.I4)]
            [FieldOffset(0)]
            public BitVector32 NameOffsetVector;
            [FieldOffset(0)]
            public int Name;
            [FieldOffset(0)]
            public short Id;
            /*
    union {
        DWORD   OffsetToData;
        struct {
            DWORD   OffsetToDirectory:31;
            DWORD   DataIsDirectory:1;
        };
    };
             */
            [FieldOffset(4)]
            public int OffsetToData;
            [FieldOffset(4)]
            public BitVector32 DirectoryOffsetVector;


            //public bool NameIsString
            //{
            //    get
            //    {
            //        return NameOffsetVector[31];
            //    }
            //}

            //public int NameOffset
            //{
            //    get
            //    {
            //        return (int)((NameOffsetVector.Data & 0x1e) >> 1);
            //    }
            //}

            //public bool DataIsDirectory
            //{
            //    get
            //    {
            //        return DirectoryOffsetVector[31];
            //    }
            //}

            //public int OffsetToDirectory
            //{
            //    get
            //    {
            //        return (int)((NameOffsetVector.Data & 0x1e) >> 1);
            //    }
            //}
        }
        #endregion

        #region _IMAGE_RESOURCE_DIRECTORY 
        public struct ImageResourceDirectory
        {
#pragma warning disable 649
            public int Characteristics;
            public int TimeDateStamp;
            public short MajorVersion;
            public short MinorVersion;
            public short NumberOfNamedEntries;
            public short NumberOfIdEntries;
#pragma warning restore 649
        }
        #endregion
    }
}
