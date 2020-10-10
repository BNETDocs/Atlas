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
using System.Diagnostics;

namespace MBNCSUtil.Data
{
    /// <summary>
    /// Provides a parser for .bni files from Starcraft and Warcraft II: Battle.net Edition.
    /// </summary>
    /// <remarks>
    /// <para>It is incorrect to attempt to use this class to read a Warcraft III .BNI file.  Those files are .MPQ files, and should be read
    /// using the <see>MpqArchive</see> class.</para>
    /// </remarks>
    public sealed class BniFileParser : IDisposable
    {
        #region utility types
        private struct IconHeader
        {
            public uint flagValue, width, height;
            public uint[] software;

            public IconHeader(BinaryReader br)
            {
                flagValue = br.ReadUInt32();
                width = br.ReadUInt32();
                height = br.ReadUInt32();
                //if (flagValue == 0)
                //{
                    List<uint> soft = new List<uint>();
                    uint val;
                    do
                    {
                        val = br.ReadUInt32();
                        if (val != 0)
                            soft.Add(val);

                    } while (val != 0);
                    software = soft.ToArray();
                //}
                //else
                //{
                //    software = new uint[0];
                //}
            }
        }
        private enum StartDescriptor : byte
        {
            BottomLeft = 0,
            BottomRight = 0x10,
            TopLeft = 0x20,
            TopRight = 0x30,
        }
        #endregion

        private Image m_fullImage;
        private List<BniIcon> m_icons = new List<BniIcon>();

        /// <summary>
        /// Creates a new BNI file parser to parse the specified file.
        /// </summary>
        /// <param name="filePath">The path to the file to parse.</param>
        /// <exception cref="FileNotFoundException">Thrown if the file specified in <paramref name="filePath"/> does not exist.</exception>
        /// <exception cref="InvalidDataException">Thrown if the file contains data types that are unsupported by this implementation.</exception>
        public BniFileParser(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file does not exist.", filePath);

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Parse(fs);
            }
        }

        /// <summary>
        /// Creates a new BNI file parser from the specified stream.
        /// </summary>
        /// <param name="bniFileStream">The stream to load.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="bniFileStream"/> is <see langword="null" />.</exception>
        /// <exception cref="InvalidDataException">Thrown if the file contains data types that are unsupported by this implementation.</exception>
        public BniFileParser(Stream bniFileStream)
        {
            if (object.ReferenceEquals(null, bniFileStream))
                throw new ArgumentNullException("bniFileStream");

            Parse(bniFileStream);
        }

        private unsafe void Parse(Stream str)
        {
            using (BinaryReader br = new BinaryReader(str))
            {
                br.ReadUInt32();  // headerLength
                ushort version = br.ReadUInt16();
                if (version != 1)
                    throw new InvalidDataException("Only version 1 of BNI files is supported.");
                br.ReadUInt16();
                uint numIcons = br.ReadUInt32();
                br.ReadUInt32(); // dataStart

                List<IconHeader> icons = new List<IconHeader>(unchecked((int)numIcons));
                for (int i = 0; i < numIcons; i++)
                {
                    icons.Add(new IconHeader(br));
                }

                // now onto the TGA header
                byte infoLength = br.ReadByte();
                br.ReadByte(); // color map type, unsupported
                if (br.ReadByte() != 0x0a) // run-length true-color; others unsupported
                    throw new InvalidDataException("Only run-length true-color TGA icons are supported.");
                br.ReadBytes(5); // color map data, ignored
                br.ReadUInt16(); // xOrigin
                br.ReadUInt16(); // yOrigin
                ushort width = br.ReadUInt16();
                ushort height = br.ReadUInt16();
                byte depth = br.ReadByte();
                if (depth != 24)
                    throw new InvalidDataException("Only 24-bit TGA is supported.");
                StartDescriptor descriptor = (StartDescriptor)br.ReadByte();
                byte[] info_bytes = br.ReadBytes(infoLength);
                Trace.WriteLine(Encoding.ASCII.GetString(info_bytes), "BNI header: information");

                int numberOfPixels = width * height;

                using (Bitmap bmp = new Bitmap(width, height))
                {
                    BitmapData data = bmp.LockBits(new Rectangle(Point.Empty, new Size(width, height)), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    int* currentPixel = (int*)data.Scan0.ToPointer();

                    for (int i = 0; i < numberOfPixels; )
                    {
                        byte packetType = br.ReadByte();
                        byte packetPixelCount = (byte)((packetType & 0x7f) + 1); // number of pixels in the current pixel packet

                        if ((packetType & 0x80) == 0x80) // check to see if MSB is set
                        {
                            byte nextBlue = br.ReadByte();
                            byte nextGreen = br.ReadByte();
                            byte nextRed = br.ReadByte();
                            Color c = Color.FromArgb(255, nextRed, nextGreen, nextBlue);
                            for (int pixel = 0; pixel < packetPixelCount; pixel++)
                            {
                                *currentPixel = c.ToArgb();
                                currentPixel++;
                            }
                        }
                        else
                        {
                            for (int pixel = 0; pixel < packetPixelCount; pixel++)
                            {
                                byte nextBlue = br.ReadByte();
                                byte nextGreen = br.ReadByte();
                                byte nextRed = br.ReadByte();
                                Color c = Color.FromArgb(255, nextRed, nextGreen, nextBlue);
                                *currentPixel = c.ToArgb();
                                currentPixel++;
                            }
                        }

                        i += packetPixelCount;
                    }

                    currentPixel = null;

                    bmp.UnlockBits(data);

                    m_fullImage = new Bitmap(width, height);

                    if (descriptor == StartDescriptor.TopRight)
                    {
                        //m_fullImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                    }
                    else if (descriptor == StartDescriptor.BottomLeft)
                    {
                        //m_fullImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
                        bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    }
                    else if (descriptor == StartDescriptor.BottomRight)
                    {
                        bmp.RotateFlip(RotateFlipType.RotateNoneFlipXY);
                    }

                    using (Graphics g = Graphics.FromImage(m_fullImage))
                    {
                        g.DrawImage(bmp, Point.Empty);
                    }

                    int currentY = 0;
                    for (int i = 0; i < numIcons; i++)
                    {
                        IconHeader header = icons[i];
                        Bitmap icon = new Bitmap(bmp, (int)width, (int)header.height);
                        using (Graphics g = Graphics.FromImage(icon))
                        {
                            Size nextIcon = new Size((int)width, (int)header.height);
                            g.DrawImage(bmp, new Rectangle(Point.Empty, nextIcon),
                                new Rectangle(new Point(0, currentY), nextIcon), GraphicsUnit.Pixel);
                        }

                        BniIcon curIcon = new BniIcon(icon, unchecked((int)header.flagValue), header.software);
                        m_icons.Add(curIcon);

                        currentY += unchecked((int)header.height);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the full image.
        /// </summary>
        public Image FullImage
        {
            get { return m_fullImage; }
        }

        /// <summary>
        /// Gets all icons and their associated metadata.
        /// </summary>
        public BniIcon[] AllIcons
        {
            get { return m_icons.ToArray(); }
        }

        #region IDisposable Members

        /// <summary>
        /// Disposes the object, freeing any managed and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_fullImage != null)
                {
                    m_fullImage.Dispose();
                    m_fullImage = null;
                }
            }
        }

        #endregion
    }
}
