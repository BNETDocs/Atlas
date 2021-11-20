using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Atlasd.Daemon;
using System.Text.Json;

namespace Atlasd.Battlenet.Protocols.Game
{
    public class ProfanityFilter
    {
        // J S O N  F I L E  C O N S T A N T S
        private const string json_title = "profanity_filter";
        private const string json_key = "key";
        private const string json_value = "value";

        // Destination array was not long enough. Check the destination index, length, and the array's lower bounds.
        // Error that occures with array.copy if the location being overwritten is over the length range of the <out>array
        private const string KEY_VALUE_LENGTH_ARGUMENTEXCEPTION = "Check your JSON file and ensure your key length and value length match.";

        private class ProfanityFilterKeySet
        {
            public string Key { get; set; }
            public byte[] Value { get; set; }
            public ProfanityFilterKeySet(string varKey, byte[] varValue)
            {
                Key = varKey;
                Value = varValue;
            }
        }
        private static List<ProfanityFilterKeySet> ChatFilterListing { get; set; }

        private static object mLockObj = new object();
        public static object LockObject
        {
            get
            {
                return mLockObj;
            }
        }
        private static bool ActiveFilterList { get; set; }

        public static void Initialize()
        {
            ActiveFilterList = false;
            if (ChatFilterListing == null || ChatFilterListing.Count != 0)
            {
                ChatFilterListing = new List<ProfanityFilterKeySet>();
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, "Initializing Profanity Filter");
            }
            else
            {
                ChatFilterListing.Clear();
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, "ReInitializing Profanity Filter");
            }

            Settings.State.RootElement.TryGetProperty(json_title, out var locProfanityFilterJson);
            string locKey, locValue;

            if (locProfanityFilterJson.ValueKind == JsonValueKind.Undefined)
            {
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, $"No {json_title} key found, defaulting to no profanity filter.");
                return;
            }
            // Make sure the title exists, and is of type array
            if (locProfanityFilterJson.ValueKind != JsonValueKind.Undefined && locProfanityFilterJson.ValueKind == JsonValueKind.Array)
            {
                foreach (var locProfanity in locProfanityFilterJson.EnumerateArray())
                {
                    locProfanity.TryGetProperty(json_key, out var locKeyJson);
                    locProfanity.TryGetProperty(json_value, out var locValueJson);

                    if (locKeyJson.ValueKind != JsonValueKind.String || locValueJson.ValueKind != JsonValueKind.String)
                        continue;

                    // since were searching in lowercase, set the values to lower case
                    locKey = locKeyJson.GetString().ToLower();
                    locValue = locValueJson.GetString().ToLower();

                    if ((!string.IsNullOrEmpty(locKey)) && (!string.IsNullOrEmpty(locValue)) && locKey.Length > 0 && locValue.Length > 0 && locKey.Length == locValue.Length)
                    {
                        var locProfane = new ProfanityFilterKeySet(locKey, Encoding.UTF8.GetBytes(locValue));
                        lock (LockObject)
                            ChatFilterListing.Add(locProfane);
                    }
                }
            }
            if (ChatFilterListing.Count > 0)
                ActiveFilterList = true;
            Logging.WriteLine(Logging.LogLevel.Info, Logging.LogType.Config, $"Initialized {ChatFilterListing.Count} Profanity Filter Keys.");
        }

        public static byte[] FilterMessage(byte[] varByteArray)
        {
            try
            {
                if (!ActiveFilterList)
                    return varByteArray;
                if (ChatFilterListing == null)
                    return varByteArray;
                byte[] finalArray = varByteArray;
                string lowerString = Encoding.UTF8.GetString(varByteArray).ToLower();

                lock (LockObject)
                {
                    int locIndex = -1;
                    foreach (var SetOfKeys in ChatFilterListing)
                    {
                        locIndex = lowerString.IndexOf(SetOfKeys.Key);
                        if (locIndex >= 0)
                            Array.Copy(SetOfKeys.Value, 0, finalArray, locIndex, SetOfKeys.Value.Length);
                    }
                }

                return finalArray;
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException(KEY_VALUE_LENGTH_ARGUMENTEXCEPTION);
            }
        }
        public static byte[] FilterMessage(string varString)
        {
            return FilterMessage(Encoding.UTF8.GetBytes(varString));
        }

        /// <summary>
        ///         ''' Just a manual disposal of our static list nothing big.
        ///         ''' </summary>
        public static void Dispose()
        {
            if (ChatFilterListing != null)
            {
                ChatFilterListing.Clear();
                ChatFilterListing = null;
            }
        }
    }
}
