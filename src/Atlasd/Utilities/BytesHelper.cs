using System;
using System.Linq;
using System.Net;
using System.Text;

namespace Atlasd.Helpers
{
    public static class BytesHelper
    {
        public static byte[] GetBytes(this UInt32[] array)
        {
            byte[] byteArray = new byte[array.Length * sizeof(UInt32)];

            for (int i = 0; i < array.Length; i++)
            {
                byte[] tempArray = BitConverter.GetBytes(array[i]);
                Array.Copy(tempArray, 0, byteArray, i * sizeof(UInt32), tempArray.Length);
            }

            return byteArray;
        }

        public static byte[] GetBytes(this IPAddress value)
        {
            byte[] ipBytes = value.GetAddressBytes();
            int ipInt = BitConverter.ToInt32(ipBytes, 0);
            int networkOrderInt = IPAddress.NetworkToHostOrder(ipInt);
            return BitConverter.GetBytes(networkOrderInt).Reverse().ToArray();
        }

        public static string AsString(this byte[] array)
        {
            return Encoding.UTF8.GetString(array);
        }

        public static byte[] ToBytes(this string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }
    }
}
