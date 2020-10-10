namespace Atlas.Battlenet
{

    public class Platform
    {
        public enum PlatformCode
        {
            MacOSClassic = 0x504D4143, // PMAC
            MacOSX = 0x584D4143, // XMAC
            Windows = 0x49583836, // IX86
        }

        public static string PlatformName(PlatformCode code, bool extended = true)
        {
            return code switch
            {
                PlatformCode.MacOSClassic => "Mac OS Classic",
                PlatformCode.MacOSX       => "Mac OS X",
                PlatformCode.Windows      => "Windows",
                _ => "Unknown" + (extended ? " (" + code.ToString() + ")" : ""),
            };
        }
    }
}
