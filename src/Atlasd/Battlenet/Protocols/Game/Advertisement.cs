using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Game
{
    class Advertisement
    {
        public string Filename { get; private set; }
        public DateTime Filetime { get; private set; }
        public string Url { get; private set; }
        public List<Product.ProductCode> Products { get; private set; }
        public List<uint> Locales { get; private set; }

        public Advertisement(string filename, string url, List<Product.ProductCode> products = null, List<uint> locales = null)
        {
            Filename = filename;
            Filetime = new FileInfo(filename).LastWriteTimeUtc;
            Url = url;
            Products = products;
            Locales = locales;
        }
    }
}
