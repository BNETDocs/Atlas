using System;

namespace Atlasd.Daemon
{
    class Common
    {
        public static bool TcpNoDelay = true;

        /**
         * <seealso cref="https://stackoverflow.com/a/228060/2367146"/>
         */
        public static string ReverseString(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public static bool TryToUInt32FromString(string value, out uint number, uint defaultNumber = 0)
        {
            string v = value;

            try
            {
                if (v.StartsWith("0b") || v.StartsWith("0B")
                    || v.StartsWith("&b") || v.StartsWith("&B"))
                {
                    number = Convert.ToUInt32(v.Substring(2), 2);
                }
                else if (v.StartsWith("0x") || v.StartsWith("0X")
                    || v.StartsWith("&h") || v.StartsWith("&H"))
                {
                    number = Convert.ToUInt32(v.Substring(2), 16);
                }
                else if (v.StartsWith("0") && v.Length > 1)
                {
                    number = Convert.ToUInt32(v.Substring(1), 8);
                }
                else
                {
                    number = Convert.ToUInt32(v, 10);
                }
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException || ex is ArgumentOutOfRangeException
                    || ex is FormatException || ex is OverflowException)
                {
                    number = defaultNumber;
                    return false;
                }
                else
                {
                    throw ex;
                }
            }

            return true;
        }
    }
}
