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

namespace MBNCSUtil.Data
{
    /// <summary>
    /// When implemented in a derived class, allows a custom image codec to be parsed to retrieve a .NET <see>Image</see> object.
    /// </summary>
    /// <remarks>
    /// <para>Because of differences in file type and purpose, this class does not support working with images from .BNI files found in 
    /// Starcraft or Warcraft II: Battle.net Edition.  To use those files, use the <see>BniFileParser</see> class instead.</para>
    /// </remarks>
    public abstract class ImageParser : IDisposable
    {
        #region constants
        private const int BLP1 = 0x31504c42;
        private const int BLP2 = 0x32504c42;
        #endregion

        /// <summary>
        /// Creates a new <see>ImageParser</see>.
        /// </summary>
        protected ImageParser()
        {

        }

        /// <summary>
        /// Gets the number of mipmaps contained in this image.
        /// </summary>
        public abstract int NumberOfMipmaps
        {
            get;
        }

        /// <summary>
        /// Gets the size of the mipmap at the specified index.
        /// </summary>
        /// <param name="mipmapIndex">The mipmap index.  This value must be non-negative and less than the value reported by 
        /// the <see>NumberOfMipmaps</see> property.</param>
        /// <returns>A <see>Size</see> containing the dimensions of the mipmap at that index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="mipmapIndex"/> is out of bounds.</exception>
        public abstract Size GetSizeOfMipmap(int mipmapIndex);

        /// <summary>
        /// Gets a new <see>Image</see> of the mipmap at the specified index.
        /// </summary>
        /// <param name="mipmapIndex">The mipmap index.  This value must be non-negative and less than the value reported by 
        /// the <see>NumberOfMipmaps</see> property.</param>
        /// <returns>An <see>Image</see> representation of the mipmap at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="mipmapIndex"/> is out of bounds.</exception>
        public abstract Image GetMipmapImage(int mipmapIndex);

        #region IDisposable Members

        /// <summary>
        /// Disposes the parser, cleaning up managed and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the parser, cleaning up unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing">Specifies whether to clean up managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {

        }

        #endregion

        /// <summary>
        /// Creates a new <see>ImageParser</see> for the file at the specified path.
        /// </summary>
        /// <param name="path">The file to open.</param>
        /// <returns>An <see>ImageParser</see> ready to present images.</returns>
        /// <exception cref="FileNotFoundException">Thrown if <paramref name="path" /> is not found.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user does not have permission to open the file at 
        /// <paramref name="path" />.</exception>
        /// <exception cref="InvalidDataException">Thrown if the file format was invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path" /> is <see langword="null" />.</exception>
        public static ImageParser Create(string path)
        {
            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Create(fs);
            }
        }

        /// <summary>
        /// Creates a new <see>ImageParser</see> for the specified stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <returns>An <see>ImageParser</see> ready to present images.</returns>
        /// <exception cref="ArgumentException">Thrown if the specified stream cannot seek.</exception>
        /// <exception cref="InvalidDataException">Thrown if the file format was invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream" /> is <see langword="null" />.</exception>
        public static ImageParser Create(Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream))
            {
                int fourCC = br.ReadInt32();
                switch (fourCC)
                {
                    case BLP1:
                        return new Blp1Parser(stream);
                    case BLP2:
                        return new Blp2Parser(stream);
                    default:
                        throw new InvalidDataException("Invalid file format.");

                }
            }
        }
    }

}
