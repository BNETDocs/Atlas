using Atlasd.Battlenet.Protocols.Game;
using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.BNFTP
{
    class BNFTPState
    {
        public ClientState Client;

        // Version 1
        public UInt16 HeaderLength = 0;
        public UInt16 ProtocolVersion = 0;
        public Platform.PlatformCode PlatformId = Platform.PlatformCode.None;
        public Product.ProductCode ProductId = Product.ProductCode.None;
        public UInt32 AdId = 0;
        public UInt32 AdFileExtension = 0;
        public UInt32 FileStartPosition = 0;
        public UInt64 FileTime = 0;
        public string FileName = null;

        // Version 2
        public UInt32 ServerToken = 0;
        public UInt32 ClientToken = 0;
        public GameKey GameKey = null;

        public BNFTPState(ClientState client)
        {
            Client = client;
            ServerToken = (uint)new Random().Next(0, 0x7FFFFFFF);
        }

        public void Receive(byte[] buffer)
        {
            using var m = new MemoryStream(buffer);
            using var r = new BinaryReader(m);

            /**
             * ## VERSION 1    <-- ##
             * ## Client -> Server ##
             *
             * (TYPE)     (FIELD)                    (DESCRIPTION)
             * UINT16     Request Length
             * UINT16     Protocol Version           0x100 (256) or 0x200 (512)
             */

            HeaderLength = r.ReadUInt16();
            ProtocolVersion = r.ReadUInt16();

            switch (ProtocolVersion)
            {
                case 0x0100:
                    {
                        /**
                         * ## VERSION 1    <-- ##
                         * ## Client -> Server ##
                         *
                         * (TYPE)     (FIELD)                    (DESCRIPTION)
                         * UINT32     Platform ID                See Product Identification
                         * UINT32     Product ID                 See Product Identification
                         * UINT32     Ad Banner ID               0 unless downloading an ad banner
                         * UINT32     Ad Banner File Extension   0 unless downloading an ad banner
                         * UINT32     File start position        For resuming an incomplete download
                         * FILETIME   Filetime
                         * STRING     Filename
                         */

                        PlatformId = (Platform.PlatformCode)r.ReadUInt32();
                        ProductId = (Product.ProductCode)r.ReadUInt32();
                        AdId = r.ReadUInt32();
                        AdFileExtension = r.ReadUInt32();
                        FileStartPosition = r.ReadUInt32();
                        FileTime = r.ReadUInt64();
                        FileName = Encoding.UTF8.GetString(r.ReadByteString());

                        /**
                         * ## VERSION 1    --> ##
                         * ## Server -> Client ##
                         *
                         * (TYPE)     (FIELD)                    (DESCRIPTION)
                         * UINT16     Header Length              Does not include the file length
                         * UINT16     Type
                         * UINT32     File size
                         * UINT32     Ad Banner ID               0 unless downloading an ad banner
                         * UINT32     Ad Banner File Extension   0 unless downloading an ad banner
                         * FILETIME   Filetime
                         * STRING     Filename
                         * VOID       File data
                        */

                        var file = new BNFTP.File(FileName);
                        if (file == null)
                        {
                            Client.Disconnect();
                        }

                        BinaryReader stream = null;

                        bool uploaded = false;
                        try
                        {
                            if (!file.OpenStream())
                            {
                                Client.Disconnect();
                                break;
                            }

                            stream = new BinaryReader(file.StreamReader.BaseStream);
                            stream.BaseStream.Position = Math.Min(stream.BaseStream.Length, FileStartPosition);

                            var fileLength = (int)(stream.BaseStream.Length - stream.BaseStream.Position);
                            HeaderLength = (UInt16)(25 + Encoding.UTF8.GetByteCount(FileName));
                            var outBuf = new byte[HeaderLength];
                            using var wm = new MemoryStream(outBuf);
                            using var w = new BinaryWriter(wm);

                            w.Write((UInt16)HeaderLength);
                            w.Write((UInt16)0); // "Type" ???
                            w.Write((UInt32)fileLength);
                            w.Write((UInt32)AdId);
                            w.Write((UInt32)AdFileExtension);
                            w.Write((UInt64)new FileInfo(file.Path).LastWriteTimeUtc.ToFileTimeUtc());
                            w.Write((string)FileName);

                            Write(outBuf);
                            Write(stream.ReadBytes(fileLength));

                            uploaded = true;
                        }
                        catch (Exception ex)
                        {
                            if (!(ex is IOException || ex is FileNotFoundException || ex is UnauthorizedAccessException || ex is PathTooLongException)) throw;

                            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_BNFTP, Client.RemoteEndPoint, $"{ex.GetType().Name} error encountered for requested file [{FileName}]" + (string.IsNullOrEmpty(ex.Message) ? "" : $"; message: {ex.Message}"));
                        }
                        finally
                        {
                            if (uploaded)
                            {
                                Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Client_BNFTP, Client.RemoteEndPoint, $"Uploaded file [{FileName}] ({file.Length} bytes)");
                            }

                            if (stream != null)
                            {
                                stream.Close();
                            }

                            Client.Disconnect();
                        }

                        break;
                    }
                case 0x0200:
                    {
                        /**
                         * ## VERSION 2    <-- ##
                         * ## Client -> Server ##
                         *
                         * (TYPE)     (FIELD)                    (DESCRIPTION)
                         * UINT32     Platform ID                See Product Identification
                         * UINT32     Product ID                 See Product Identification
                         * UINT32     Ad Banner ID               0 unless downloading an ad banner
                         * UINT32     Ad Banner File Extension   0 unless downloading an ad banner
                         * UINT32     File start position        For resuming an incomplete download
                         * FILETIME   Filetime
                         * STRING     Filename
                         */

                        PlatformId = (Platform.PlatformCode)r.ReadUInt32();
                        ProductId = (Product.ProductCode)r.ReadUInt32();
                        AdId = r.ReadUInt32();
                        AdFileExtension = r.ReadUInt32();
                        FileStartPosition = r.ReadUInt32();
                        FileTime = r.ReadUInt64();
                        FileName = Encoding.UTF8.GetString(r.ReadByteString());

                        /**
                         * ## VERSION 2    --> ##
                         * ## Server -> Client ##
                         *
                         * (TYPE)     (FIELD)                    (DESCRIPTION)
                         * UINT32     Server Token
                         */

                        /**
                         * ## VERSION 2    <-- ##
                         * ## Client -> Server ##
                         *
                         * (TYPE)     (FIELD)                    (DESCRIPTION)
                         * UINT32     Starting position          Facilitates resuming
                         * FILETIME   Local filetime
                         * UINT32     Client Token
                         * UINT32     Key Length
                         * UINT32     Key's product value
                         * UINT32     Key's public value
                         * UINT32     Unknown (Always 0)
                         * UINT32 [5] CD key hash
                         * STRING     Filename
                         */

                        /**
                         * ## VERSION 2    --> ##
                         * ## Server -> Client ##
                         *
                         * (TYPE)     (FIELD)                    (DESCRIPTION)
                         * UINT16     Header Length              Does not include the file length
                         * UINT32     File size
                         * UINT32     Ad Banner ID               0 unless downloading an ad banner
                         * UINT32     Ad Banner File Extension   0 unless downloading an ad banner
                         * FILETIME   Filetime
                         * STRING     Filename
                         * VOID       File data
                        */

                        break;
                    }
                default:
                    {
                        Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Client_BNFTP, $"Received unknown BNFTP protocol version [0x{ProtocolVersion:X4}]");
                        Client.Disconnect("Unknown BNFTP protocol version");
                        break;
                    }
            }
        }

        public void Write(byte[] buffer)
        {
            Client.Send(buffer);
        }
    }
}
