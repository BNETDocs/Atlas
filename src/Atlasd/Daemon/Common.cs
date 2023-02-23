using System;
using System.Net;

namespace Atlasd.Daemon
{
    class Common
    {
        public static Battlenet.Protocols.Http.HttpListener HttpListener = null;
        public static bool TcpNoDelay = true;

        public static void Initialize()
        {
            InitializeListener();
        }

        private static void InitializeListener()
        {
            Settings.State.RootElement.TryGetProperty("http", out var httpJson);
            httpJson.TryGetProperty("listener", out var listenerJson);
            listenerJson.TryGetProperty("interface", out var interfaceJson);
            listenerJson.TryGetProperty("port", out var portJson);

            var listenerAddressStr = interfaceJson.GetString();
            if (!IPAddress.TryParse(listenerAddressStr, out IPAddress listenerAddress))
            {
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Server, $"Unable to parse IP address from [http.listener.interface] with value [{listenerAddressStr}]; using any");
                listenerAddress = IPAddress.Any;
            }

            if (!portJson.TryGetInt32(out int listenerPort))
            {
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Server, $"Unable to parse port from [http.listener.port] with value [{portJson}]; using 8080");
                listenerPort = 8080;
            }

            if (!IPEndPoint.TryParse($"{listenerAddress}:{listenerPort}", out IPEndPoint listenerEndPoint))
            {
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Server, $"Unable to parse endpoint with value [{listenerAddress}:{listenerPort}]");
                return;
            }

            HttpListener = new Battlenet.Protocols.Http.HttpListener(listenerEndPoint);
        }

        public static bool TryToInt32FromString(string value, out int number, int defaultNumber = 0)
        {
            string v = value;

            try
            {
                if (v.StartsWith("0b") || v.StartsWith("0B")
                    || v.StartsWith("&b") || v.StartsWith("&B"))
                {
                    number = Convert.ToInt32(v[2..], 2);
                }
                if (v.StartsWith("-0b") || v.StartsWith("-0B")
                    || v.StartsWith("-&b") || v.StartsWith("-&B"))
                {
                    number = 0 - Convert.ToInt32(v[3..], 2);
                }
                else if (v.StartsWith("0x") || v.StartsWith("0X")
                    || v.StartsWith("&h") || v.StartsWith("&H"))
                {
                    number = Convert.ToInt32(v[2..], 16);
                }
                else if (v.StartsWith("-0x") || v.StartsWith("-0X")
                    || v.StartsWith("-&h") || v.StartsWith("-&H"))
                {
                    number = Convert.ToInt32(v[3..], 16);
                }
                else if (v.StartsWith("0") && v.Length > 1)
                {
                    number = Convert.ToInt32(v[1..], 8);
                }
                else if (v.StartsWith("-0") && v.Length > 2)
                {
                    number = Convert.ToInt32(v[2..], 8);
                }
                else
                {
                    number = Convert.ToInt32(v, 10);
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
                    throw;
                }
            }

            return true;
        }

        public static bool TryToUInt32FromString(string value, out uint number, uint defaultNumber = 0)
        {
            string v = value;

            try
            {
                if (v.StartsWith("0b") || v.StartsWith("0B")
                    || v.StartsWith("&b") || v.StartsWith("&B"))
                {
                    number = Convert.ToUInt32(v[2..], 2);
                }
                else if (v.StartsWith("0x") || v.StartsWith("0X")
                    || v.StartsWith("&h") || v.StartsWith("&H"))
                {
                    number = Convert.ToUInt32(v[2..], 16);
                }
                else if (v.StartsWith("0") && v.Length > 1)
                {
                    number = Convert.ToUInt32(v[1..], 8);
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
                    throw;
                }
            }

            return true;
        }
    }
}
