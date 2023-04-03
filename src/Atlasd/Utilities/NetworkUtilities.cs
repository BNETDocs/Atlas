using System.IO;
using System.Net;

namespace Atlasd.Utilities
{
    class NetworkUtilities
    {
        public static string GetPublicAddress()
        {
            WebRequest request = WebRequest.Create("https://api.ipify.org");
            request.Method = "GET";

            using (WebResponse response = request.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }
    }
}
