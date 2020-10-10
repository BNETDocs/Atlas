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
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MBNCSUtil.Data
{
    internal class Blp2Parser : ImageParser
    {
        #region constants
        private const int MAX_MIPMAP_COUNT = 16;
        private const int BLP2_PALETTE_SIZE = 256;
        private const int BLP1_JPG_HEADER_OFFSET = 0xa0;
        private const int BLP1_JPG_HEADER_SIZE = 0x270;
        #endregion
        #region fields
        private Size m_mainSize;
        private Blp2CompressionType m_cmpType;
        private Color[] m_palette;
        private int m_alphaBits;
        private List<Blp2MipMap> m_mipmaps;
        private byte[] m_jpgHeader;
        #endregion

        public Blp2Parser(Stream stream)
        {
            if (!stream.CanSeek)
                throw new ArgumentException("The specified stream cannot seek.", "stream");

            m_mipmaps = new List<Blp2MipMap>();

            ParseInternal(stream);
        }

        #region internal implementation methods
        private void ParseInternal(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream))
            {
                br.ReadInt32(); // unknown
                m_cmpType = (Blp2CompressionType)br.ReadByte();
                m_alphaBits = br.ReadByte();
                int paletteBits = br.ReadByte();
                br.ReadByte(); // unknown
                int width = br.ReadInt32();
                int height = br.ReadInt32();
                Size fullImageSize = new Size(width, height);
                m_mainSize = fullImageSize;

                int[] mipmapOffsets = new int[MAX_MIPMAP_COUNT];
                for (int i = 0; i < MAX_MIPMAP_COUNT; i++)
                    mipmapOffsets[i] = br.ReadInt32();

                int[] sizes = new int[MAX_MIPMAP_COUNT];
                for (int i = 0; i < MAX_MIPMAP_COUNT; i++)
                    sizes[i] = br.ReadInt32();

            
                if (m_cmpType == Blp2CompressionType.Palette)
                {
                    if (paletteBits != 8)
                        throw new ArgumentException("Palettized data can only have 8 bits per pixel.", "stream");
                }
                else if (m_cmpType == Blp2CompressionType.DirectX)
                {
                    if (m_alphaBits != 0 && m_alphaBits != 8 && m_alphaBits != 1)
                        throw new ArgumentException("DXT2/DXT4-compressed images are not yet supported.");
                } 
                else if (m_cmpType == Blp2CompressionType.Jpeg) 
                {

                }
                else
                {
                    throw new ArgumentException("Unknown file format.");
                }

                if (m_cmpType == Blp2CompressionType.Palette)
                {
                    ParsePalette(stream, br);
                }
                else if (m_cmpType == Blp2CompressionType.Jpeg)
                {
                    ParseJpegHeader(stream, br);
                }

                for (int i = 0; i < MAX_MIPMAP_COUNT; i++)
                {
                    int currentOffset = mipmapOffsets[i];
                    int currentSize = sizes[i];

                    if (currentOffset == 0 || currentSize == 0)
                        break;

                    Size mipmapSize = Blp1Parser.GetSize(fullImageSize, i);
                    byte[] data = new byte[currentSize];
                    stream.Seek(currentOffset, SeekOrigin.Begin);
                    br.Read(data, 0, currentSize);
                    Blp2MipMap mipmap = new Blp2MipMap(mipmapSize, i, data);
                    m_mipmaps.Add(mipmap);
                }
            }
        }

        private void ParsePalette(Stream stream, BinaryReader br)
        {
            stream.Seek(148, SeekOrigin.Begin);
            m_palette = new Color[BLP2_PALETTE_SIZE];
            if (m_alphaBits == 0)
            {
                for (int i = 0; i < BLP2_PALETTE_SIZE; i++)
                {
                    m_palette[i] = Color.FromArgb(255, Color.FromArgb(br.ReadInt32()));
                }
            }
            else if (m_alphaBits == 8)
            {
                for (int i = 0; i < BLP2_PALETTE_SIZE; i++)
                {
                    int color = br.ReadInt32();
                    m_palette[i] = Color.FromArgb(255, Color.FromArgb(color));
                }
            }
            else
            {
                throw new InvalidDataException("Unexpected number of bits per alpha channel; only 0 or 8 are allowed for palettized images.");
            }
        }

        private void ParseJpegHeader(Stream stream, BinaryReader br)
        {
            long curPosition = stream.Position;
            stream.Seek(BLP1_JPG_HEADER_OFFSET, SeekOrigin.Begin);
            byte[] jpgHeader = new byte[BLP1_JPG_HEADER_SIZE];
            br.Read(jpgHeader, 0, BLP1_JPG_HEADER_SIZE);
            stream.Seek(curPosition, SeekOrigin.Begin);

            m_jpgHeader = jpgHeader;
        }

        private Image FromPalette(Blp2MipMap mipmap)
        {
            Bitmap bmp = new Bitmap(mipmap.Size.Width, mipmap.Size.Height, PixelFormat.Format32bppArgb);
            byte[] mipmapData = mipmap.Data;
            int[] result = new int[mipmap.Size.Width * mipmap.Size.Height];
            for (int i = 0; i < result.Length; i++)
            {
                Color paletteEntry = m_palette[mipmapData[i]];
                result[i] = paletteEntry.ToArgb();
            }
            BitmapData bmpData = bmp.LockBits(new Rectangle(Point.Empty, mipmap.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(result, 0, bmpData.Scan0, result.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }

        private Image FromJpeg(Blp2MipMap mipmap)
        {
            byte[] result = new byte[BLP1_JPG_HEADER_SIZE + mipmap.Data.Length];
            Buffer.BlockCopy(m_jpgHeader, 0, result, 0, m_jpgHeader.Length);
            Buffer.BlockCopy(mipmap.Data, 0, result, BLP1_JPG_HEADER_SIZE, mipmap.Data.Length);

            Image img;
            using (MemoryStream ms = new MemoryStream(result, false))
            {
                img = Image.FromStream(ms, false, false);
            }

            return img;
        }

        private Image FromDxt5(Blp2MipMap mipmap)
        {
            int[] data = DXTCFormat.DecodeDXT2(mipmap.Size.Width, mipmap.Size.Height, mipmap.Data);
            return FromBinary(mipmap.Size.Width, mipmap.Size.Height, data);
        }

        private Image FromDxt1(Blp2MipMap mipmap)
        {
            int[] data = DXTCFormat.DecodeDXT1(mipmap.Size.Width, mipmap.Size.Height, mipmap.Data);
            return FromBinary(mipmap.Size.Width, mipmap.Size.Height, data);
        }

        private unsafe Image FromBinary(int width, int height, int[] bmpData)
        {
            Bitmap bmp = new Bitmap(width, height);
            BitmapData data = bmp.LockBits(new Rectangle(Point.Empty, new Size(width, height)),
                        ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            uint* pBits = (uint*)data.Scan0;
            if (m_alphaBits > 0)
            {
                for (int i = 0; i < (width * height); i++)
                {
#pragma warning disable 0675
                    uint nVal = unchecked((uint)(((bmpData[i * 4] << 16) & 0xff0000) | ((bmpData[i * 4 + 1] << 8) & 0xff00) | ((bmpData[i * 4 + 2]) & 0xff) | ((bmpData[i * 4 + 3] & 0x000000ff) << 24) & 0xffffffff));
#pragma warning restore 0675
                    // FIX FOR IMAGES THAT ARE DXT WITH ALPHA BUT RETURNING 0 ALPHA:
                    // THESE LINES CAN BE UNCOMMENTED BUT OTHER TEXTURES COME BACK WRONG
                    // EXAMPLE TEXTURE:
                    // common.mpq\textures\weather\Snowflake01.blp
                    //if ((nVal & 0xff000000) == 0 && nVal != 0)
                    //    nVal |= 0xff000000;

                    *pBits++ = nVal;
                }
            }
            else
            {
                for (int i = 0; i < width * height; i++)
                {
#pragma warning disable 0675
                    uint nVal = unchecked((uint)(((bmpData[i * 3] << 16) & 0xff0000) | ((bmpData[i * 3 + 1] << 8) & 0xff00) | ((bmpData[i * 3 + 2]) & 0xff) | 0xff000000) & 0xffffffff);
#pragma warning restore 0675
                    *pBits++ = nVal;
                }
            }


            bmp.UnlockBits(data);

            return bmp;
        }


        #endregion

        #region Inherited virtual methods
        public override int NumberOfMipmaps
        {
            get { return m_mipmaps.Count; }
        }

        public override Size GetSizeOfMipmap(int mipmapIndex)
        {
            return Blp1Parser.GetSize(m_mainSize, mipmapIndex);
        }

        public override Image GetMipmapImage(int mipmapIndex)
        {
            if (mipmapIndex < 0 || mipmapIndex >= m_mipmaps.Count)
                throw new ArgumentOutOfRangeException("mipmapIndex", mipmapIndex, "Index must be non-negative and less than the number of mipmaps in this BLP file.");

            Blp2MipMap mipmap = m_mipmaps[mipmapIndex];

            Image result = null;
            if (m_cmpType == Blp2CompressionType.Palette)
            {
                result = FromPalette(mipmap);
            }
            else if (m_cmpType == Blp2CompressionType.DirectX)
            {
                if (m_alphaBits == 0 || m_alphaBits == 1)
                {
                    result = FromDxt1(mipmap);
                }
                else if (m_alphaBits == 8)
                {
                    result = FromDxt5(mipmap);
                }
            }
            else
            {
                result = FromJpeg(mipmap);
            }

            return result;
        }
        #endregion

        #region Blp2MipMap class
        private class Blp2MipMap
        {
            private Size _size;
            private int _index;
            private byte[] _data;

            public Blp2MipMap(Size size, int index, byte[] data)
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
