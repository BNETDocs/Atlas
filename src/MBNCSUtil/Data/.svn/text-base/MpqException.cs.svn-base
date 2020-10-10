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
using System.Runtime.Serialization;

namespace MBNCSUtil.Data
{
    /// <summary>
    /// Wraps an I/O exception with MPQ-specific errors.
    /// </summary>
    [Serializable]
    public class MpqException : IOException
    {
        /// <summary>
        /// Creates an MPQ exception with no exception information.
        /// </summary>
        public MpqException() : base() { }

        /// <summary>
        /// Creates an MPQ exception with the specified message.
        /// </summary>
        /// <param name="message">The error message related to the exception.</param>
        public MpqException(string message) : base(message) { }
        /// <summary>
        /// Creates an MPQ exception with the specified message and inner exception.
        /// </summary>
        /// <param name="message">The error message related to the exception.</param>
        /// <param name="inner">A related exception that caused this exception.</param>
        public MpqException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Creates an MPQ exception from serialized data.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization streaming context.</param>
        protected MpqException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
