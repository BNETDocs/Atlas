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
using System.Globalization;

namespace MBNCSUtil.Data
{
    /// <summary>
    /// Represents metadata about an icon from a Starcraft or Warcraft II: Battle.net Edition .BNI file.  This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>To obtain an instance of this class, use the <see>BniFileParser</see> class.</para>
    /// </remarks>
    public sealed class BniIcon : IDisposable
    {
        private Image m_img;
        private int m_flags;
        private string[] m_softwareList;

        internal BniIcon(Image img, int flags, uint[] softwareList)
        {
            m_img = img;
            m_flags = flags;

            m_softwareList = new string[softwareList.Length];
            for (int i = 0; i < softwareList.Length; i++)
            {
                byte[] code = BitConverter.GetBytes(softwareList[i]);
                byte temp = code[0];
                code[0] = code[3];
                code[3] = temp;
                temp = code[1];
                code[1] = code[2];
                code[2] = temp;
                m_softwareList[i] = Encoding.ASCII.GetString(code);
            }
        }

        /// <summary>
        /// Gets the icon.
        /// </summary>
        public Image Image
        {
            get { return m_img; }
        }

#if BNSHARP
        /// <summary>
        /// Gets the user flags that should be set in order for a user to display this icon.
        /// </summary>
        /// <remarks>
        /// <para>If this value is <see cref="UserFlags">None</see> then the user should receive his or her product-defined icon, 
        /// available in the <see>SoftwareProductCodes</see> property.
        /// </para>
        /// </remarks>
#else
        /// <summary>
        /// Gets the user flags that should be set in order for a user to display this icon.
        /// </summary>
        /// <remarks>
        /// <para>If this value is <see cref="UserFlags">None</see> then the user should receive his or her product-defined icon, 
        /// available in the <see>SoftwareProductCodes</see> property.
        /// </para>
        /// </remarks>
#endif
        public 
#if BNSHARP
            UserFlags
#else
            int 
#endif
            UserFlags
        {
            get { return 
#if BNSHARP
                (UserFlags)
#endif
                m_flags; }
        }

        /// <summary>
        /// Gets a list of the product codes that are eligible for this icon.  If this list is empty, the icon is determined based on the 
        /// user's flags.
        /// </summary>
        public string[] SoftwareProductCodes
        {
            get
            {
                string[] codes = new string[m_softwareList.Length];
                Array.Copy(m_softwareList, codes, codes.Length);
                return codes;
            }
        }

        /// <summary>
        /// Gets a string representation of this icon.
        /// </summary>
        /// <returns>A string containing flags and product information.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (m_softwareList.Length > 0)
            {
                sb.Append(m_softwareList[0]);
                for (int i = 1; i < m_softwareList.Length; i++)
                {
                    sb.AppendFormat(CultureInfo.CurrentCulture, ",{0}", m_softwareList);
                }
            }
#if BNSHARP
            return string.Format(CultureInfo.CurrentCulture, "Flags: {0}, Products: {1}", (UserFlags)m_flags, sb);
#else
            return string.Format(CultureInfo.CurrentCulture, "Flags: 0x{0:x8}, Products: {1}", m_flags, sb);
#endif
        }

        #region IDisposable Members

        /// <summary>
        /// Disposes the object, freeing any unmanaged and managed resources.
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
                if (m_img != null)
                {
                    m_img.Dispose();
                    m_img = null;
                }
            }
        }
        #endregion
    }
}
