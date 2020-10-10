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
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Globalization;

namespace MBNCSUtil.Data
{
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    internal static class LateBoundStormDllApi
    {
        #region MPQ exception throwing helper
        [DebuggerStepThrough]
        private static void ThrowMpqException(MpqErrorCodes status)
        {
            //Console.WriteLine("Last win32 error: {0}", i);
            switch (status)
            {
                case MpqErrorCodes.BadOpenMode:
                    throw new MpqException(Resources.mpq_badOpenMode);
                case MpqErrorCodes.FileNotFound:
                    throw new MpqException(Resources.mpq_fileNotFound);
                case MpqErrorCodes.MpqInvalid:
                    throw new MpqException(Resources.mpq_mpqArchiveCorrupt);
                default:
                    throw new MpqException(string.Format(CultureInfo.InvariantCulture, Resources.mpq_UnknownErrorType, status));
            }
        }
        #endregion
        public static void Initialize(IntPtr hModule)
        {
            IntPtr openArch = NativeMethods.GetProcAddress(hModule, "SFileOpenArchive");
            callback_SFileOpenArchive = (SFileOpenArchiveCallback)Marshal.GetDelegateForFunctionPointer(
                openArch, typeof(SFileOpenArchiveCallback));

            IntPtr closeArch = NativeMethods.GetProcAddress(hModule, "SFileCloseArchive");
            callback_SFileCloseArchive = (SFileCloseArchiveCallback)Marshal.GetDelegateForFunctionPointer(
                closeArch, typeof(SFileCloseArchiveCallback));

            IntPtr openFileEx = NativeMethods.GetProcAddress(hModule, "SFileOpenFileEx");
            callback_SFileOpenFileEx = (SFileOpenFileExCallback)Marshal.GetDelegateForFunctionPointer(
                openFileEx, typeof(SFileOpenFileExCallback));

            IntPtr hasFile = NativeMethods.GetProcAddress(hModule, "SFileHasFile");
            callback_SFileHasFile = (SFileHasFileCallback)Marshal.GetDelegateForFunctionPointer(
                hasFile, typeof(SFileHasFileCallback));

            IntPtr closeFile = NativeMethods.GetProcAddress(hModule, "SFileCloseFile");
            callback_SFileCloseFile = (SFileCloseFileCallback)Marshal.GetDelegateForFunctionPointer(
                closeFile, typeof(SFileCloseFileCallback));

            IntPtr getFileSize = NativeMethods.GetProcAddress(hModule, "SFileGetFileSize");
            callback_SFileGetFileSize = (SFileGetFileSizeCallback)Marshal.GetDelegateForFunctionPointer(
                getFileSize, typeof(SFileGetFileSizeCallback));

            IntPtr setFilePtr = NativeMethods.GetProcAddress(hModule, "SFileSetFilePointer");
            callback_SFileSetPointer = (SFileSetFilePointerCallback)Marshal.GetDelegateForFunctionPointer(
                setFilePtr, typeof(SFileSetFilePointerCallback));

            IntPtr readFile = NativeMethods.GetProcAddress(hModule, "SFileReadFile");
            callback_SFileReadFile = (SFileReadFileCallback)Marshal.GetDelegateForFunctionPointer(
                readFile, typeof(SFileReadFileCallback));
        }

        #region SFileOpenArchiveCallback
        private static SFileOpenArchiveCallback callback_SFileOpenArchive;
        public static IntPtr SFileOpenArchive(string fileName, uint dwPriority, uint dwFlags)
        {
            IntPtr hMpq = IntPtr.Zero;
            MpqErrorCodes status = callback_SFileOpenArchive(fileName, dwPriority, dwFlags, ref hMpq);
            if (status != MpqErrorCodes.Okay)
            {
                ThrowMpqException(status);
            }
            return hMpq;
        }
        #endregion
        #region SFileCloseArchive
        private static SFileCloseArchiveCallback callback_SFileCloseArchive;
        public static void SFileCloseArchive(IntPtr hMPQ)
        {
            MpqErrorCodes status = callback_SFileCloseArchive(hMPQ);
            if (status != MpqErrorCodes.Okay)
                ThrowMpqException(status);
        }
        #endregion
        #region SFileHasFile
        private static SFileHasFileCallback callback_SFileHasFile;
        public static bool SFileHasFile(IntPtr hMPQ, string fileName)
        {
            return callback_SFileHasFile(hMPQ, fileName);
        }
        #endregion
        #region SFileOpenFileEx
        private static SFileOpenFileExCallback callback_SFileOpenFileEx;
        public static IntPtr SFileOpenFileEx(IntPtr hMPQ, string fileName, SearchType searchScope)
        {
            IntPtr hFile = IntPtr.Zero;
            MpqErrorCodes status = callback_SFileOpenFileEx(hMPQ, fileName, searchScope, ref hFile);
            if (status != MpqErrorCodes.Okay)
                ThrowMpqException(status);

            return hFile;
        }
        #endregion
        #region SFileCloseFile
        private static SFileCloseFileCallback callback_SFileCloseFile;
        public static void SFileCloseFile(IntPtr hFile)
        {
            MpqErrorCodes status = callback_SFileCloseFile(hFile);
            if (status != MpqErrorCodes.Okay)
                ThrowMpqException(status);
        }
        #endregion
        #region SFileGetFileSize
        private static SFileGetFileSizeCallback callback_SFileGetFileSize;
        public static long SFileGetFileSize(IntPtr hFile)
        {
            int highFile = 0;
            int low = callback_SFileGetFileSize(hFile, ref highFile);
            long size = (highFile << 32) + low;

            return size;
        }
        #endregion
        #region SFileSetFilePointer
        private static SFileSetFilePointerCallback callback_SFileSetPointer;
        public static long SFileSetFilePointer(IntPtr hFile, long distanceToMove, SeekOrigin seekType)
        {
            int distanceHigh = (int)(distanceToMove >> 32);
            int distanceLow = unchecked((int)(distanceToMove & 0xffffffff));
            distanceLow = callback_SFileSetPointer(hFile, distanceLow, ref distanceHigh, seekType);

            long distance = (distanceHigh << 32) + distanceLow;
            return distance;
        }
        #endregion
        #region SFileReadFile
        private static SFileReadFileCallback callback_SFileReadFile;
        public static int SFileReadFile(IntPtr hFile, byte[] lpBuffer, int numberToRead)
        {
            int bytesRead = 0;
            MpqErrorCodes status = callback_SFileReadFile(hFile, lpBuffer, unchecked((uint)numberToRead),
                ref bytesRead, IntPtr.Zero);
            if (status != MpqErrorCodes.Okay)
            {
                Debugger.Break();
                ThrowMpqException(status);
            }

            return bytesRead;
        }
        #endregion
    }

    #region delegates
    internal delegate MpqErrorCodes SFileOpenArchiveCallback(string lpFileName, uint dwPriority, uint dwFlags,
                                                                ref IntPtr hMPQ);
    internal delegate MpqErrorCodes SFileCloseArchiveCallback(IntPtr hMPQ);
    internal delegate MpqErrorCodes SFileOpenFileCallback(string lpFileName, ref IntPtr hFile);
    internal delegate MpqErrorCodes SFileOpenFileExCallback(IntPtr hMPQ, string lpFileName, SearchType dwSearchScope, ref IntPtr hFile);
    internal delegate bool SFileHasFileCallback(IntPtr hMPQ, string szFileName);
    internal delegate MpqErrorCodes SFileCloseFileCallback(IntPtr hFile);
    internal delegate int SFileGetFileSizeCallback(IntPtr hFile, ref int lpFileSizeHigh);
    internal delegate int SFileSetFilePointerCallback(IntPtr hFile, int lDistanceToMove, ref int lplDistanceToMoveHigh, SeekOrigin dwMoveMethod);
    internal delegate MpqErrorCodes SFileReadFileCallback(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, ref int lpNumberOfBytesRead, IntPtr lpOverlapped);
    #endregion

    internal enum MpqErrorCodes
    {
        Okay = 1,
        MpqInvalid = unchecked((int)0x85200065),
        FileNotFound = unchecked((int)0x85200066),
        DiskFull = unchecked((int)0x85200068),
        HashTableFull = unchecked((int)0x85200069),
        AlreadyExists = unchecked((int)0x8520006a),
        BadOpenMode = unchecked((int)0x8520006c),
        CompactError = unchecked((int)0x85300001),
    }

    internal enum SearchType
    {
        CurrentOnly = 0,
        AllOpen = 1,
    }
}
