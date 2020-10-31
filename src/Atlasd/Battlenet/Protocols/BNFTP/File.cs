using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.BNFTP
{
    class File
    {
        public bool Exists { get => new FileInfo(Name).Exists; }
        public string Name { get; private set; }
        public DateTime LastModified { get => System.IO.File.GetLastAccessTime(Name); }
        public long Length { get => new FileInfo(Name).Length; }

        public File(string filename)
        {
            Name = filename;
        }

        public StreamReader GetStream()
        {
            return new StreamReader(Name);
        }
    }
}
