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

namespace MBNCSUtil.Util
{
    internal sealed class HeapPtr : IDisposable
    {
        private int m_len;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr m_ptr;
        private AllocMethod m_method;

        public HeapPtr(int byteLength, AllocMethod method)
        {
            m_len = byteLength;

            if (!Enum.IsDefined(typeof(AllocMethod), method))
                throw new ArgumentOutOfRangeException("method");

            if (byteLength < 0)
                throw new ArgumentOutOfRangeException("byteLength");

            m_method = method;

            switch (method)
            {
                case AllocMethod.HGlobal:
                    m_ptr = Marshal.AllocHGlobal(byteLength);
                    break;
                case AllocMethod.CoTaskMem:
                    m_ptr = Marshal.AllocCoTaskMem(byteLength);
                    break;
            }
        }

        public unsafe void ReadData(byte* ptr, int byteLength)
        {
            byte[] bytes = new byte[byteLength];
            Marshal.Copy(new IntPtr((void*)ptr), bytes, 0, byteLength);
            MarshalData(bytes);
        }

        public void MarshalData(byte[] data)
        {
            if (m_ptr == IntPtr.Zero)
                throw new ObjectDisposedException("HeapPtr");

            if (data.Length > m_len)
            {
                m_len = data.Length;
                Realloc(data.Length);
            }

            Marshal.Copy(data, 0, m_ptr, data.Length);
        }

        //public void MarshalStringA(string s)
        //{
        //    MarshalData(Encoding.ASCII.GetBytes(s));
        //}

        //public void MarshalStringW(string s)
        //{
        //    MarshalData(Encoding.Unicode.GetBytes(s));
        //}

        public void Realloc(int newLength)
        {
            if (m_ptr == IntPtr.Zero)
                throw new ObjectDisposedException("HeapPtr");

            switch (m_method)
            {
                case AllocMethod.HGlobal:
                    m_ptr = Marshal.ReAllocHGlobal(m_ptr, new IntPtr(newLength));
                    break;
                case AllocMethod.CoTaskMem:
                    m_ptr = Marshal.ReAllocCoTaskMem(m_ptr, newLength);
                    break;
                default:
                    throw new InvalidOperationException("Invalid memory type.");
            }
        }

        public unsafe void* ToPointer()
        {
            if (m_ptr == IntPtr.Zero)
                throw new ObjectDisposedException("HeapPtr");

            return m_ptr.ToPointer();
        }

        #region IDisposable Members

        ~HeapPtr()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (m_ptr == IntPtr.Zero)
                return;

            switch (m_method)
            {
                case AllocMethod.HGlobal:
                    Marshal.FreeHGlobal(m_ptr);
                    break;
                case AllocMethod.CoTaskMem:
                    Marshal.FreeCoTaskMem(m_ptr);
                    break;
            }

            m_ptr = IntPtr.Zero;

            if (disposing)
            {

            }
        }

        #endregion

        #region implicit operators
        public static unsafe implicit operator byte*(HeapPtr h)
        {
            return (byte*)h.ToPointer();
        }
        #endregion

        #region explicit operators
        public static explicit operator IntPtr(HeapPtr h)
        {
            return h.m_ptr;
        }
        #endregion
    }

    internal enum AllocMethod
    {
        HGlobal,
        CoTaskMem
    }
}
