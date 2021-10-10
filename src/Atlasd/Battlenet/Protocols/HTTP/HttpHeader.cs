using System;
using System.Collections.Generic;
using System.Text;

namespace Atlasd.Battlenet.Protocols.Http
{
    class HttpHeader
    {
        public const int MaxKeyLength = 4096;
        public const int MaxValueLength = 4096;

        protected string Key;
        protected string Value;

        public HttpHeader(string key, string value)
        {
            SetKey(key);
            SetValue(value);
        }

        public string GetKey()
        {
            return Key;
        }

        public string GetValue()
        {
            return Value;
        }

        public void SetKey(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaxKeyLength)
            {
                throw new ArgumentOutOfRangeException($"value length must be between 1-{MaxKeyLength}");
            }

            Key = value;
        }

        public void SetValue(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > MaxValueLength)
            {
                throw new ArgumentOutOfRangeException($"value length must be between 1-{MaxValueLength}");
            }

            Value = value;
        }

        public override string ToString()
        {
            return $"{Key}: {Value}\r\n";
        }
    }
}
