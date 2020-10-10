using Atlasd.Battlenet;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Atlasd.Daemon
{
    class Configuration
    {
        private const byte FileVersion = 1;

        public static JsonElement State;
        public static string Path = "Atlasd.json";

        public Configuration()
        {
            Load();
        }

        public void Load()
        {
            Reset();
            Console.WriteLine("[Config] Loading configuration.");

            try
            {
                byte[] jsonBytes = File.ReadAllBytes(Path);
                Utf8JsonReader jsonReader = new Utf8JsonReader(jsonBytes);
                State = JsonSerializer.Deserialize<JsonElement>(ref jsonReader);

                /*byte FileVersion = buffer.ReadByte();
                if (FileVersion != 1)
                {
                    Console.Error.WriteLine("[Config] Version is incompatible.");
                }*/
            }
            catch (FileNotFoundException)
            {
                Console.Error.WriteLine("[Config] File not found.");
            }
            finally
            {
            }
        }

        public void Reset()
        {
            Console.WriteLine("[Config] Resetting to default configuration.");

            State = new JsonElement();
        }

        public void Save()
        {
            Console.WriteLine("[Config] Saving configuration.");

            try
            {
            }
            finally
            {
            }
        }

    }
}