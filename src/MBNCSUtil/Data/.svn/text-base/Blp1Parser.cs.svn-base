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
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MBNCSUtil.Data
{
    internal class Blp1Parser : ImageParser
    {
        #region constants
        private const int MAX_BLP1_MIPMAP_COUNT = 16;
        private const int BLP1_PALETTE_SIZE = 256;
        private const int BLP1_JPG_HEADER_OFFSET = 0xa0;
        private const int BLP1_JPG_HEADER_SIZE = 0x270;
        #endregion

        #region fields
        private List<Blp1MipMap> _mipmaps;
        private Blp1ImageType _compressionType;
        private Color[] _palette;
        private byte[] _jpgHeader;
        private Size _mainSize;
        #endregion

        #region constructors
        public Blp1Parser(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new ArgumentException("Cannot seek on the specified stream.", "stream");
            }

            _mipmaps = new List<Blp1MipMap>();

            ParseInternal(stream);
        }
        #endregion

        #region internal implementation methods
        private void ParseInternal(Stream s)
        {
            using (BinaryReader br = new BinaryReader(s))
            {
                Blp1ImageType compressionType = (Blp1ImageType)br.ReadInt32();
                int mipmapCount = br.ReadInt32();

                if (!Enum.IsDefined(typeof(Blp1ImageType), compressionType))
                    throw new InvalidDataException("The file specified an unknown type of BLP1 compression.");
                if (mipmapCount < 0)
                    throw new InvalidDataException("The file specified a negative number of mipmaps, which is invalid.");

                _compressionType = compressionType;

                int sizeX = br.ReadInt32();
                int sizeY = br.ReadInt32();

                Size fullImageSize = new Size(sizeX, sizeY);
                _mainSize = fullImageSize;

                br.ReadInt64(); // skip 'u2';

                int[] offsets = new int[MAX_BLP1_MIPMAP_COUNT];
                int[] sizes = new int[MAX_BLP1_MIPMAP_COUNT];

                for (int i = 0; i < MAX_BLP1_MIPMAP_COUNT; i++)
                {
                    offsets[i] = br.ReadInt32();
                }

                for (int i = 0; i < MAX_BLP1_MIPMAP_COUNT; i++)
                {
                    sizes[i] = br.ReadInt32();
                }

                if (compressionType == Blp1ImageType.Palette)
                {
                    ParsePalette(br);
                }
                else
                {
                    ParseJpgHeader(s, br);
                }

                for (int i = 0; i < MAX_BLP1_MIPMAP_COUNT; i++)
                {
                    int currentOffset = offsets[i];
                    int currentSize = sizes[i];

                    if (currentOffset == 0 || currentSize == 0)
                        break;

                    Size mipmapSize = GetSize(fullImageSize, i);
                    byte[] data = new byte[currentSize];
                    s.Seek(currentOffset, SeekOrigin.Begin);
                    br.Read(data, 0, currentSize);
                    Blp1MipMap mipmap = new Blp1MipMap(mipmapSize, i, data);
                    _mipmaps.Add(mipmap);
                }
            }
        }

        private void ParseJpgHeader(Stream s, BinaryReader br)
        {
            long curPosition = s.Position;
            s.Seek(BLP1_JPG_HEADER_OFFSET, SeekOrigin.Begin);
            byte[] jpgHeader = new byte[BLP1_JPG_HEADER_SIZE];
            br.Read(jpgHeader, 0, BLP1_JPG_HEADER_SIZE);
            s.Seek(curPosition, SeekOrigin.Begin);

            _jpgHeader = jpgHeader;
        }

        internal static Size GetSize(Size originalSize, int mipmapIndex)
        {
            Size result = originalSize;
            for (int i = 0; i < mipmapIndex; i++)
            {
                result = new Size((int)Math.Ceiling((double)result.Width / 2.0),
                    (int)Math.Ceiling((double)result.Height / 2.0));
            }

            return result;
        }

        private void ParsePalette(BinaryReader br)
        {
            _palette = new Color[BLP1_PALETTE_SIZE];

            for (int i = 0; i < BLP1_PALETTE_SIZE; i++)
            {
                Color c = Color.FromArgb(br.ReadInt32());
                c = Color.FromArgb(255, c);
                _palette[i] = c;
            }
        }

        private Image FromPalette(Blp1MipMap mipmap)
        {
            Bitmap bmp = new Bitmap(mipmap.Size.Width, mipmap.Size.Height, PixelFormat.Format32bppArgb);
            byte[] mipmapData = mipmap.Data;
            int[] result = new int[mipmap.Size.Width * mipmap.Size.Height];
            for (int i = 0; i < result.Length; i++)
            {
                Color paletteEntry = _palette[mipmapData[i]];
                result[i] = paletteEntry.ToArgb();
            }
            BitmapData bmpData = bmp.LockBits(new Rectangle(Point.Empty, mipmap.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(result, 0, bmpData.Scan0, result.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }

        private Image FromJpeg(Blp1MipMap mipmap)
        {
            byte[] result = new byte[BLP1_JPG_HEADER_SIZE + mipmap.Data.Length];
            Buffer.BlockCopy(_jpgHeader, 0, result, 0, _jpgHeader.Length);
            Buffer.BlockCopy(mipmap.Data, 0, result, BLP1_JPG_HEADER_SIZE, mipmap.Data.Length);

            Image img;
            using (MemoryStream ms = new MemoryStream(result, false))
            {
                img = Image.FromStream(ms, false, false);
            }

            return img;
        }
        #endregion

        #region inherited virtual methods
        public override int NumberOfMipmaps
        {
            get { return _mipmaps.Count; }
        }

        public override System.Drawing.Size GetSizeOfMipmap(int mipmapIndex)
        {
            return GetSize(_mainSize, mipmapIndex);
        }

        public override System.Drawing.Image GetMipmapImage(int mipmapIndex)
        {
            if (mipmapIndex < 0 || mipmapIndex >= _mipmaps.Count)
                throw new ArgumentOutOfRangeException("mipmapIndex", mipmapIndex, "Index must be non-negative and less than the number of mipmaps in this BLP file.");

            Blp1MipMap mipmap = _mipmaps[mipmapIndex];

            Image result = null;
            if (_compressionType == Blp1ImageType.Palette)
                result = FromPalette(mipmap);
            else
                result = FromJpeg(mipmap);

            return result;
        }
        #endregion

        #region IDispoable members
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _jpgHeader = null;

                if (_mipmaps != null)
                {
                    _mipmaps.Clear();
                    _mipmaps = null;
                }

                _palette = null;
            }
        }
        #endregion

        #region Blp1MipMap class
        private class Blp1MipMap
        {
            private Size _size;
            private int _index;
            private byte[] _data;

            public Blp1MipMap(Size size, int index, byte[] data)
            {
                _size = size;
                _index = index;
                _data = data;
            }

            public Size Size
            {
                get { return _size; }
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public int Index
            {
                get { return _index; }
            }

            public byte[] Data
            {
                get
                {
                    byte[] copy = new byte[_data.Length];
                    Buffer.BlockCopy(_data, 0, copy, 0, copy.Length);
                    return copy;
                }
            }
        }
        #endregion
    }
}
