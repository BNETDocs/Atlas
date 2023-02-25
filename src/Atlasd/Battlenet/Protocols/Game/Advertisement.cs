using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Atlasd.Battlenet.Protocols.Game
{
    class Advertisement
    {
        public long DisplayCount;
        public string Filename { get; private set; }
        public DateTime Filetime { get; private set; }
        public string Url { get; private set; }
        public List<Product.ProductCode> Products { get; private set; }
        public List<uint> Locales { get; private set; }

        public Advertisement(string filename, string url, List<Product.ProductCode> products = null, List<uint> locales = null)
        {
            var file = new BNFTP.File(filename);
            if (file != null)
            {
                Filename = file.Name;
                Filetime = file.LastAccessTimeUtc;
            }
            else
            {
                Filename = filename;
                Filetime = DateTime.MinValue;
            }

            Url = url;
            Products = products;
            Locales = locales;

            DisplayCount = 0; // Each matching SID_DISPLAYAD increments this by one.
        }

        public void IncrementDisplayCount()
        {
            Interlocked.Increment(ref DisplayCount);
        }
    }
}
