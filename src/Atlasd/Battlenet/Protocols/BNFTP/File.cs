using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.BNFTP
{
    class File
    {
        public string BNFTPPath { get => Settings.GetString(new string[] { "bnftp", "root" }, null); }
        public bool Exists { get => new FileInfo(Path.Combine(BNFTPPath, Name)).Exists; }
        public string Name { get; private set; }
        public DateTime LastAccessTime { get => System.IO.File.GetLastAccessTime(Path.Combine(BNFTPPath, Name)); }
        public DateTime LastAccessTimeUtc { get => System.IO.File.GetLastAccessTimeUtc(Path.Combine(BNFTPPath, Name)); }
        public long Length { get => new FileInfo(Path.Combine(BNFTPPath, Name)).Length; }

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
