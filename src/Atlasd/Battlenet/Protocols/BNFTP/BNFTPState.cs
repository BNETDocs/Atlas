using Atlasd.Battlenet.Protocols.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.BNFTP
{
    struct BNFTPState
    {
        public ClientState Client;

        // Version 1
        public UInt16 RequestLength;
        public UInt16 ProtocolVersion;
        public Platform.PlatformCode PlatformId;
        public Product.ProductCode ProductId;
        public UInt32 AdId;
        public UInt32 AdFileExtension;
        public UInt32 FileStartPosition;
        public UInt64 FileTime;
        public byte[] FileName;

        // Version 2
        public UInt32 ServerToken;
        public UInt32 ClientToken;
        public GameKey GameKey;
    }
}
