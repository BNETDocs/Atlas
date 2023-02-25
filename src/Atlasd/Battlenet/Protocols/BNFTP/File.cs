using Atlasd.Daemon;
using System;
using System.IO;

namespace Atlasd.Battlenet.Protocols.BNFTP
{
    class File : IDisposable
    {
        public string BNFTPPath { get => Settings.GetString(new string[] { "bnftp", "root" }, null); }
        public bool Exists { get => GetFileInfo().Exists; }
        public bool IsDirectory { get => GetFileInfo().Attributes.HasFlag(FileAttributes.Directory); }
        public string Name { get; private set; }
        public DateTime LastAccessTime { get => System.IO.File.GetLastAccessTime(System.IO.Path.Combine(BNFTPPath, Name)); }
        public DateTime LastAccessTimeUtc { get => System.IO.File.GetLastAccessTimeUtc(System.IO.Path.Combine(BNFTPPath, Name)); }
        public DateTime LastWriteTime { get => System.IO.File.GetLastWriteTime(System.IO.Path.Combine(BNFTPPath, Name)); }
        public DateTime LastWriteTimeUtc { get => System.IO.File.GetLastWriteTimeUtc(System.IO.Path.Combine(BNFTPPath, Name)); }
        public long Length { get => GetFileInfo().Length; }
        public string Path { get => System.IO.Path.GetFullPath(System.IO.Path.Combine(BNFTPPath, Name)); }
        public StreamReader StreamReader { get; private set; } = null;

        public File(string filename)
        {
            Name = filename;
        }

        /**
         * <remarks>Closes any internally open references. Call this before dropping all references to this object.</remarks>
         */
        public void Close()
        {
            CloseStream(); // close the StreamReader if it's open.
        }

        /**
         * <remarks>Closes the StreamReader property if it is open.</remarks>
         */
        public void CloseStream()
        {
            if (StreamReader != null)
            {
                StreamReader.Close();
            }
        }

        /**
         * <remarks>This is part of the .NET IDisposable class interface. User code should use the Close() method instead.</remarks>
         */
        public void Dispose() /* part of IDisposable */
        {
            // call our own Close() method.
            Close();
        }

        /**
         * <remarks>Retrieves a System.IO.FileInfo object given the current values of the BNFTPPath and Name properties.</remarks>
         * <param name="ignoreLimits">Set to False to check that the target file does not leave BNFTP root, is a file, and it exists. If a check fails, the entire method stops, prints to Logging, and returns null. Set to True to ignore these checks instead and attempt creating a FileInfo object anyway.</param>
         * <returns>The System.IO.FileInfo object if successful, null otherwise.</returns>
         */
        public FileInfo GetFileInfo(bool ignoreLimits = false)
        {
            var rootStr = System.IO.Path.GetFullPath(BNFTPPath);
            var pathStr = System.IO.Path.GetFullPath(System.IO.Path.Combine(rootStr, Name));

            if (!ignoreLimits && (pathStr.Length < rootStr.Length || pathStr.Substring(0, rootStr.Length) != rootStr))
            {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.BNFTP, $"Error retrieving file info; path would leave BNFTP root directory");
                return null;
            }

            FileInfo fileinfo;
            try
            {
                fileinfo = new FileInfo(System.IO.Path.Combine(BNFTPPath, Name));
            }
            catch (Exception ex)
            {
                if (!(ex is UnauthorizedAccessException || ex is PathTooLongException || ex is NotSupportedException)) throw;
                return null;
            }

            if (fileinfo == null)
            {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.BNFTP, $"Error retrieving file info for [{Name}]; null FileInfo object");
                return null;
            }

            if (!ignoreLimits && !fileinfo.Exists)
            {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.BNFTP, $"Error retrieving file info for [{Name}]; file not found");
                return null;
            }

            if (!ignoreLimits && fileinfo.Attributes.HasFlag(FileAttributes.Directory))
            {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.BNFTP, $"Error retrieving file info for [{Name}]; path pointed to a directory");
                return null;
            }

            return fileinfo;
        }

        /**
         * <remarks>Opens a StreamReader object then assigns it to the StreamReader property, and returns true if successful; returns false otherwise.</remarks>
         * <returns>True if the StreamReader was opened and assigned to File.StreamReader, False otherwise; if False, File.StreamReader will be null.</returns>
         */
        public bool OpenStream()
        {
            if (StreamReader != null)
            {
                StreamReader.Close();
            }
            StreamReader = null;

            var fileinfo = GetFileInfo();
            if (fileinfo == null) return false;

            Logging.WriteLine(Logging.LogLevel.Debug, Logging.LogType.BNFTP, $"Opening read stream for [{fileinfo.FullName}]...");
            StreamReader = new StreamReader(fileinfo.FullName);

            return true;
        }
    }
}
