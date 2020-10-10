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

namespace MBNCSUtil.Data
{
    // Class that handles all DXTC format specific conversions
    internal static class DXTCFormat
    {
        // takes DXT1 compressed 8 bit data in b, with width w and height w
        // returns uncompressed bitmap data as 32 bit RGBA
        public static int[] DecodeDXT1(int w, int h, byte[] b)
        {
            int[] r = new int[w * h * 3];
            int i, j, m, n;

            int work = 0;
            int color_0, color_1;
            int colorIndex;
            int[][] colors = new int[4][];

            // Loop through the 4*4 pixel wide compression blocks
            for (j = 0; j < h / 4; j++)
            {
                for (i = 0; i < w / 4; i++)
                {
                    // Decode the block palette
                    color_0 = b[work] + b[work + 1] * 256;
                    color_1 = b[work + 2] + b[work + 3] * 256;
                    colors[0] = decodeColor(color_0);
                    colors[1] = decodeColor(color_1);
                    colors[2] = new int[3];
                    colors[3] = new int[3];

                    // Peculiarity of DXT1, if c0 is bigger than c1, we're handling a 4 color block
                    // if c0 <= c1, then it's a 3 color block with hard transparency, and c3 designates this transparency
                    if (color_0 > color_1) // 4 color block
                    {
                        for (m = 0; m < 3; m++)
                            colors[2][m] = (2 * colors[0][m] + colors[1][m] + 1) / 3;
                        for (m = 0; m < 3; m++)
                            colors[3][m] = (colors[0][m] + 2 * colors[1][m] + 1) / 3;

                    }
                    else // 3 color block with transparency
                    {
                        for (m = 0; m < 3; m++)
                            colors[2][m] = (colors[0][m] + colors[1][m]) / 2;
                        colors[3] = decodeColor(0);
                    }

                    // Read the subpixels and store their RGBA values in the return array
                    for (n = 0; n < 4; n++)
                    {
                        for (m = 0; m < 4; m++)
                        {
                            colorIndex = dxtcPixel(b, work + 4, m, n);
                            r[((j * 4 + n) * w + i * 4 + m) * 3 + 0] = colors[colorIndex][0];
                            r[((j * 4 + n) * w + i * 4 + m) * 3 + 1] = colors[colorIndex][1];
                            r[((j * 4 + n) * w + i * 4 + m) * 3 + 2] = colors[colorIndex][2];
                        }
                    }

                    // One compression block in DXT1 has a length of 8 bytes
                    work += 8;
                }
            }

            return r;
        }

        // takes DXT2 compressed 8 bit data in b, with width w and height w
        // returns uncompressed bitmap data as 32 bit RGBA
        public static int[] DecodeDXT2(int w, int h, byte[] b)
        {
            int[] r = new int[w * h * 4];
            int i, j, m, n;

            int work = 0, alpha;
            int color_0, color_1;
            int colorIndex;
            int[][] colors = new int[4][];
            int[] alphas = new int[4];

            // Loop through the 4*4 pixel wide compression blocks
            for (j = 0; j < h / 4; j++)
            {
                for (i = 0; i < w / 4; i++)
                {
                    for (m = 0; m < 4; m++)
                        alphas[m] = b[work + 2 * m] + b[work + 2 * m + 1] * 256;

                    color_0 = b[work + 8] + b[work + 9] * 256;
                    color_1 = b[work + 10] + b[work + 11] * 256;
                    colors[0] = decodeColor(color_0);
                    colors[1] = decodeColor(color_1);

                    colors[2] = new int[3];
                    colors[3] = new int[3];
                    for (m = 0; m < 3; m++)
                        colors[2][m] = (2 * colors[0][m] + colors[1][m] + 1) / 3;
                    for (m = 0; m < 3; m++)
                        colors[3][m] = (colors[0][m] + 2 * colors[1][m] + 1) / 3;

                    for (n = 0; n < 4; n++)
                    {
                        for (m = 0; m < 4; m++)
                        {
                            colorIndex = dxtcPixel(b, work + 12, m, n);
                            r[((j * 4 + n) * w + i * 4 + m) * 4 + 0] = colors[colorIndex][0];
                            r[((j * 4 + n) * w + i * 4 + m) * 4 + 1] = colors[colorIndex][1];
                            r[((j * 4 + n) * w + i * 4 + m) * 4 + 2] = colors[colorIndex][2];
                            alpha = dxtcAlpha(alphas, m, n);
                            r[((j * 4 + n) * w + i * 4 + m) * 4 + 3] = (alpha == 15) ? 255 : alpha;
                        }
                    }

                    // DXT2 blocks are 16 bytes long
                    work += 16;
                }
            }

            return r;
        }

        // Encapsulates some of the bitshifting done for decoding DXT1 blocks
        private static int dxtcPixel(byte[] b, int s, int x, int y)
        {
            x *= 2;
            int w = b[s + y];
            int r = (w & (3 << x)) >> x;
            return r;
        }

        // Takes a 4*4 compression block of alpha values in a, then returns the alpha value
        // of the subpixel defined by x and y
        // Some bitshifting has to be done since one byte covers two pixels
        private static int dxtcAlpha(int[] a, int x, int y)
        {
            x *= 4;
            int w = a[y];
            int r = (w & (15 << x)) >> x;
            return r;
        }

        // Converts a 565 color value into an array of 888 RGB color data 
        private static int[] decodeColor(int colorCode)
        {
            int blue = colorCode & 31;
            int green = (colorCode & 2016) / 32;
            int red = (colorCode & 63488) / 2048;

            int[] r = new int[3];
            r[0] = red * 8;
            r[1] = green * 4;
            r[2] = blue * 8;

            return r;
        }

    }
}
