using Atlasd.Daemon;
using System;
using System.Globalization;
using System.Net;
using System.Numerics;

namespace Atlasd.Battlenet
{
    class ServerSignature
    {
        public const int Length = 128;

        public static byte[] Create(ClientState client)
        {
            var signature = new byte[Length];

            var modulusHex = Settings.GetString(new[] { "battlenet", "emulation", "server_signature", "modulus" }, "", true);
            var exponentHex = Settings.GetString(new[] { "battlenet", "emulation", "server_signature", "private_exponent" }, "", true);
            if (string.IsNullOrEmpty(modulusHex) || string.IsNullOrEmpty(exponentHex)) return signature;

            var addressText = Settings.GetString(new[] { "battlenet", "emulation", "server_signature", "address" }, "", true);
            IPAddress address;
            if (!string.IsNullOrEmpty(addressText)) address = IPAddress.Parse(addressText);
            else if (client.Socket?.LocalEndPoint is IPEndPoint localEndPoint) address = localEndPoint.Address.MapToIPv4();
            else return signature;

            var modulus = BigInteger.Parse("0" + modulusHex, NumberStyles.HexNumber);
            var exponent = BigInteger.Parse("0" + exponentHex, NumberStyles.HexNumber);

            var block = new byte[Length];
            Array.Fill(block, (byte)0xBB);
            address.GetAddressBytes().CopyTo(block, 0);
            block[Length - 1] = 0x0B;

            var message = new BigInteger(block, isUnsigned: true);
            if (message >= modulus)
            {
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, "Setting [battlenet] -> [emulation] -> [server_signature] -> [modulus] is too small to sign with; check value");
                return signature;
            }

            BigInteger.ModPow(message, exponent, modulus).ToByteArray(isUnsigned: true).CopyTo(signature, 0);
            return signature;
        }
    }
}
