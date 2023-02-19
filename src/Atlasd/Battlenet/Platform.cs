using System;

namespace Atlasd.Battlenet
{

    public class Platform
    {
        public enum PlatformCode : UInt32
        {
            None = 0, // None/Zero/Null
            MacOSPPC = 0x504D4143, // PMAC
            MacOSX86 = 0x584D4143, // XMAC
            Windows = 0x49583836, // IX86
        }

        public static string PlatformName(PlatformCode code, bool extended = true)
        {
            return code switch
            {
                PlatformCode.None     => "None",
                PlatformCode.MacOSPPC => $"macOS Classic{(extended ? " (PowerPC)" : "")}",
                PlatformCode.MacOSX86 => $"macOS{(extended ? " (x86)" : "")}",
                PlatformCode.Windows  => $"Windows{(extended ? " (x86)" : "")}",
                _ => $"Unknown{(extended ? $" (0x{(UInt32)code:X8})" : "")}",
            };
        }
    }
}
