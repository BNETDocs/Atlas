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
using System.Runtime.InteropServices;

namespace MBNCSUtil.Data
{
    #region interop support
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern IntPtr LoadLibrary(string path);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        //[DllImport("kernel32.dll")]
        //public static extern void FreeLibrary(IntPtr hModule);

        public static bool Is64BitProcess
        {
            get
            {
                return IntPtr.Size == 8;
            }
        }
    }
    #endregion
}
