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
    internal sealed class LockdownHeap
    {
        #region LDHeapRecord
        private class LDHeapRecord
        {
            public byte[] data;

            //public static Comparison<LDHeapRecord> Comparison
            //{
            //    get
            //    {
            //        return delegate(LDHeapRecord a, LDHeapRecord b)
            //        {
            //            int valA = BitConverter.ToInt32(a.data, 0);
            //            int valB = BitConverter.ToInt32(b.data, 0);

            //            if (valA < valB)
            //                return -1;
            //            else if (valA > valB)
            //                return 1;
                        
            //            return 0;
            //        };
            //    }
            //}
        }
        #endregion

        public LockdownHeap()
        {
            m_obs = new List<LDHeapRecord>();
        }

        private List<LDHeapRecord> m_obs;

        public void Add(int[] src)
        {
            byte[] data = new byte[src.Length * 4];
            Buffer.BlockCopy(src, 0, data, 0, src.Length * 4);
            Add(data);
        }

        public void Add(byte[] data)
        {
            if (data.Length < 0x10)
                throw new ArgumentOutOfRangeException("data", "Argument must be 16 bytes or longer.");

            LDHeapRecord rec = new LDHeapRecord();
            rec.data = new byte[16];
            Buffer.BlockCopy(data, 0, rec.data, 0, 16);
            m_obs.Add(rec);
        }

        //public void Sort()
        //{
        //    m_obs.Sort(LDHeapRecord.Comparison);
        //}

        //public IntPtr ToIntPtr()
        //{
        //    int byteLength = m_obs.Count * 16;
        //    IntPtr mem = Marshal.AllocHGlobal(byteLength);
        //    for (int i = 0; i < m_obs.Count; i++)
        //    {
        //        Marshal.Copy(m_obs[i].data, 0, new IntPtr(mem.ToInt64() + (i * 16)), 16);
        //    }

        //    return mem;
        //}

        public HeapPtr ToPointer()
        {
            int byteLength = m_obs.Count * 16;
            HeapPtr ptr = new HeapPtr(byteLength, AllocMethod.HGlobal);
            for (int i = 0; i < m_obs.Count; i++)
            {
                IntPtr memoryBase = (IntPtr)ptr;
                IntPtr memoryOffset = new IntPtr(memoryBase.ToInt64() + (i * 16));
                Marshal.Copy(m_obs[i].data, 0, memoryOffset, 16);
            }
            return ptr;
        }

        public int CurrentLength
        {
            get { return m_obs.Count; }
        }
    }
}
