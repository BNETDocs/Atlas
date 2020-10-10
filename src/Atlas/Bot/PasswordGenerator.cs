using System;

namespace Atlas.Bot
{
    class PasswordGenerator
    {
        // generator settings
        public static bool letter_o_with_zero = true;
        public static bool lowercase = true;
        public static int size = 16;
        public static bool symbols = true;
        public static bool numbers = true;
        public static bool uppercase = true;

        public static string Generate()
        {
            string buffer = "";
            string mask = "";
            Random r = new Random();

            if (lowercase) mask += "abcdefghijklmnpqrstuvwxyz";
            if (uppercase) mask += "ABCDEFGHIJKLMNPQRSTUVWXYZ";
            if (numbers) mask += "0123456789";
            if (symbols) mask += "!@#$%^&*()[]{}\\|;:'\",<.>/?";

            if (letter_o_with_zero)
            {
                if (lowercase) mask += "o";
                if (uppercase) mask += "O";
            }

            while (buffer.Length < size)
                buffer += mask.Substring(r.Next(0, mask.Length), 1);

            return buffer;
        }
    }
}
