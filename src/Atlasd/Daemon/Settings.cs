using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Atlasd.Daemon
{
    class Settings
    {
        private const uint DocumentVersionSupported = 0;

        public static JsonDocument State { get; private set; }
        public static string Path { get; private set; } = null;

        public static void Initialize()
        {
            if (Path == null || Path.Length == 0)
            {
                SetPathToDefault();
            }

            Load();
        }

        public static void Load()
        {
            Reset();
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, $"Loading configuration from [{Path}]");

            try
            {
                var jsonOpts = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                    MaxDepth = 10
                };

                var fileState = JsonDocument.Parse(new StreamReader(Path).ReadToEnd(), jsonOpts);

                if (!fileState.RootElement.TryGetProperty("document_version", out var documentVersionJson))
                {
                    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Document version missing, refusing to continue");
                    return;
                }

                if (!documentVersionJson.TryGetUInt32(out var documentVersionInt))
                {
                    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Document version is not of a uint32 type, refusing to continue");
                    return;
                }

                if (documentVersionInt != DocumentVersionSupported)
                {
                    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Document version {documentVersionInt} is different than supported version {DocumentVersionSupported}, refusing to continue");
                    return;
                }

                State = fileState;
                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, "Loaded configuration");
            }
            catch (FileNotFoundException)
            {
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"File not found [{Path}]");
            }
            catch (InvalidOperationException e)
            {
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"{e.GetType().Name} occurred while parsing [{Path}], refusing to continue");
            }
            finally
            {
            }
        }

        public static void Reset()
        {
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, "Resetting to default configuration");

            State = JsonDocument.Parse("{}");
        }

        public static void Save()
        {
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, $"Saving configuration to [{Path}]");

            byte[] jsonUtf8Bytes;

            try
            {
                var jsonOpts = new JsonSerializerOptions
                {
                    MaxDepth = 10,
                    WriteIndented = true,
                };
                jsonUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(State, jsonOpts);

                using (var stream = new StreamWriter(Path))
                {
                    stream.Write(jsonUtf8Bytes);
                }

                Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, "Saved configuration");
            }
            catch (IOException)
            {
                Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, "Failed to save configuration due to IOException");
            }
        }

        public static void SetPath(string path)
        {
            Path = path;
        }

        public static void SetPathToDefault()
        {
            Path = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "atlasd.json"));
        }
    }
}
