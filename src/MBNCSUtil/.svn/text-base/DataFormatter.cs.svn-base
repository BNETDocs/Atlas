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
using System.Runtime.InteropServices;
using System.Text;

namespace MBNCSUtil
{
    /// <summary>
    /// Provides functions for printing bytes to various output devices.  This class cannot
    /// be inherited.
    /// </summary>
    /// <example>
    /// <para>This example demonstrates how the formatter prints out binary data.</para>
    /// <code language="c#">
    /// DataFormatter.WriteToConsole(XSha1.CalculateHash(Encoding.ASCII.GetBytes("password")));
    /// </code>
    /// <para><b>Output:</b></para>
    /// <code>
    /// 0000   ec c8 0d 1d 76 e7 58 c0  b9 da 8c 25 ff 10 6a ff    ìE..vçXA.U.%ÿ.jÿ
    /// 0010   8e 24 29 16                                         .$).
    /// </code>
    /// </example>
    public static class DataFormatter
    {
        /// <summary>
        /// Formats a data into 16-byte rows followed by an ASCII representation.
        /// </summary>
        /// <param name="data">The data to format.</param>
        /// <returns>A string representing the data.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <b>data</b> is <b>null</b>
        /// (<b>Nothing</b> in Visual Basic).</exception>
        public static string Format(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(Resources.param_data, Resources.dataNull);

            StringBuilder sb = new StringBuilder();
            sb.Append("0000   ");
            if (data.Length == 0)
            {
                sb.Append("(empty)");
                return sb.ToString();
            }

            StringBuilder lineAscii = new StringBuilder(16, 16);

            for (int i = 0; i < data.Length; i++)
            {
                #region build the end-of-line ascii

                char curData = (char)data[i];
                if (char.IsLetterOrDigit(curData) || char.IsPunctuation(curData) ||
                    char.IsSymbol(curData) || curData == ' ')
                {
                    lineAscii.Append(curData);
                }
                else
                {
                    lineAscii.Append('.');
                }
                #endregion

                sb.AppendFormat("{0:x2} ", data[i]);
                if ((i + 1) % 8 == 0)
                {
                    sb.Append(" ");
                }
                if (((i + 1) % 16 == 0) || ((i + 1) == data.Length))
                {
                    if ((i + 1) == data.Length && ((i + 1) % 16) != 0)
                    {
                        int lenOfCurStr = ((i % 16) * 3);
                        if ((i % 16) > 8) lenOfCurStr++;

                        for (int j = 0; j < (47 - lenOfCurStr); j++)
                            sb.Append(' ');
                    }

                    sb.AppendFormat("  {0}", lineAscii.ToString());
                    lineAscii = new StringBuilder(16, 16);
                    sb.Append(Environment.NewLine);

                    if (data.Length > (i + 1))
                    {
                        sb.AppendFormat("{0:x4}   ", i + 1);
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Formats a data into 16-byte rows followed by an ASCII representation.
        /// </summary>
        /// <param name="data">The data to format.</param>
        /// <param name="startIndex">The starting position of the data to format.</param>
        /// <param name="length">The amount of data to format.</param>
        /// <returns>A string representing the data.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <b>data</b> is <b>null</b>
        /// (<b>Nothing</b> in Visual Basic).</exception>
        public static string Format(byte[] data, int startIndex, int length)
        {
            if (data == null)
                throw new ArgumentNullException(Resources.param_data, Resources.dataNull);

            StringBuilder sb = new StringBuilder();
            sb.Append("0000   ");
            if (data.Length == 0)
            {
                sb.Append("(empty)");
                return sb.ToString();
            }

            StringBuilder lineAscii = new StringBuilder(16, 16);

            for (int i = startIndex; i < data.Length && i < (startIndex + length); i++)
            {
                #region build the end-of-line ascii

                char curData = (char)data[i];
                if (char.IsLetterOrDigit(curData) || char.IsPunctuation(curData) ||
                    char.IsSymbol(curData) || curData == ' ')
                {
                    lineAscii.Append(curData);
                }
                else
                {
                    lineAscii.Append('.');
                }
                #endregion

                sb.AppendFormat("{0:x2} ", data[i]);
                if ((i + 1) % 8 == 0)
                {
                    sb.Append(" ");
                }
                if (((i + 1) % 16 == 0) || ((i + 1) == data.Length))
                {
                    if ((i + 1) == data.Length && ((i + 1) % 16) != 0)
                    {
                        int lenOfCurStr = ((i % 16) * 3);
                        if ((i % 16) > 8) lenOfCurStr++;

                        for (int j = 0; j < (47 - lenOfCurStr); j++)
                            sb.Append(' ');
                    }

                    sb.AppendFormat("  {0}", lineAscii.ToString());
                    lineAscii = new StringBuilder(16, 16);
                    sb.Append(Environment.NewLine);

                    if (data.Length > (i + 1))
                    {
                        sb.AppendFormat("{0:x4}   ", i + 1);
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Writes a series of bytes to the console, printing them in 16-byte rows
        /// followed by an ASCII representation.
        /// </summary>
        /// <param name="data">The data to print.</param>
        public static void WriteToConsole(byte[] data)
        {
            Console.WriteLine(Format(data));
        }

        /// <summary>
        /// Writes a series of bytes to trace listeners, printing them in 16-byte rows,
        /// followed by an ASCII representation.
        /// </summary>
        /// <param name="data">The data to print.</param>
        public static void WriteToTrace(byte[] data)
        {
            System.Diagnostics.Trace.WriteLine(Format(data));
        }

        /// <summary>
        /// Writes a series of bytes to trace listeners, printing them in 16-byte rows,
        /// followed by an ASCII representation.
        /// </summary>
        /// <param name="data">The data to print.</param>
        /// <param name="category">A category name to classify the data.</param>
        public static void WriteToTrace(byte[] data, string category)
        {
            System.Diagnostics.Trace.WriteLine(Format(data), category);
        }
    }
}
