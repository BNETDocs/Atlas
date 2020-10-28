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
        public static string Path =
#if DEBUG
                                    "../../../../../etc/atlasd.json";
#else
                                    "../etc/atlasd.json";
#endif

        public static void Load()
        {
            Reset();
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, "Loading configuration");

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
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, "Saving configuration");
            Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, "(Not implemented)");
        }
    }
}