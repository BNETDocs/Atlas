﻿using Atlasd.Daemon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static bool CanRead()
        {
            FileInfo fileinfo;
            try
            {
                fileinfo = new FileInfo(Path);

                if (fileinfo == null)
                {
                    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Configuration file [{Path}] returned null FileInfo object; check filesystem");
                    return false;
                }

                if (!fileinfo.Exists)
                {
                    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Configuration file [{Path}] does not exist; check file");
                    return false;
                }

                if (fileinfo.Attributes.HasFlag(FileAttributes.Directory))
                {
                    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Configuration file [{Path}] points to a directory; check path string");
                    return false;
                }

                if (fileinfo.Length == 0)
                {
                    Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Configuration file [{Path}] is empty; check file");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                if (!(ex is ArgumentNullException || ex is UnauthorizedAccessException || ex is PathTooLongException)) throw;
                return false;
            }
        }

        public static JsonElement.ArrayEnumerator GetArray(string[] keyPath, bool suppressLog = false)
        {
            try
            {
                var json = State.RootElement;
                for (var i = 0; i < keyPath.Length; i++)
                {
                    if (!json.TryGetProperty(keyPath[i], out json)) break;
                }
                return json.EnumerateArray();
            }
            catch (Exception ex)
            {
                if (!(ex is ArgumentNullException || ex is InvalidOperationException)) throw;
                if (!suppressLog) Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{string.Join("] -> [", keyPath)}] is not an array; check value");
                return new JsonElement.ArrayEnumerator();
            }
        }

        public static bool GetBoolean(string[] keyPath, bool defaultValue, bool suppressLog = false)
        {
            try
            {
                var json = State.RootElement;
                for (var i = 0; i < keyPath.Length; i++)
                {
                    if (!json.TryGetProperty(keyPath[i], out json)) return defaultValue;
                }
                return json.GetBoolean();
            }
            catch (Exception ex)
            {
                if (!(ex is ArgumentNullException || ex is InvalidOperationException)) throw;
                if (!suppressLog) Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{string.Join("] -> [", keyPath)}] is not a boolean; check value");
                return defaultValue;
            }
        }

        public static byte GetByte(string[] keyPath, byte defaultValue, bool suppressLog = false)
        {
            try
            {
                var json = State.RootElement;
                for (var i = 0; i < keyPath.Length; i++)
                {
                    if (!json.TryGetProperty(keyPath[i], out json)) return defaultValue;
                }
                return json.GetByte();
            }
            catch (Exception ex)
            {
                if (!(ex is ArgumentNullException || ex is InvalidOperationException)) throw;
                if (!suppressLog) Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{string.Join("] -> [", keyPath)}] is not a byte; check value");
                return defaultValue;
            }
        }

        public static Int16 GetInt16(string[] keyPath, Int16 defaultValue, bool suppressLog = false)
        {
            try
            {
                var json = State.RootElement;
                for (var i = 0; i < keyPath.Length; i++)
                {
                    if (!json.TryGetProperty(keyPath[i], out json)) return defaultValue;
                }
                return json.GetInt16();
            }
            catch (Exception ex)
            {
                if (!(ex is ArgumentNullException || ex is InvalidOperationException)) throw;
                if (!suppressLog) Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{string.Join("] -> [", keyPath)}] is not a signed 16-bit integer; check value");
                return defaultValue;
            }
        }

        public static Int32 GetInt32(string[] keyPath, Int32 defaultValue, bool suppressLog = false)
        {
            try
            {
                var json = State.RootElement;
                for (var i = 0; i < keyPath.Length; i++)
                {
                    if (!json.TryGetProperty(keyPath[i], out json)) return defaultValue;
                }
                return json.GetInt32();
            }
            catch (Exception ex)
            {
                if (!(ex is ArgumentNullException || ex is InvalidOperationException)) throw;
                if (!suppressLog) Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{string.Join("] -> [", keyPath)}] is not a signed 32-bit integer; check value");
                return defaultValue;
            }
        }

        public static Int64 GetInt64(string[] keyPath, Int64 defaultValue, bool suppressLog = false)
        {
            try
            {
                var json = State.RootElement;
                for (var i = 0; i < keyPath.Length; i++)
                {
                    if (!json.TryGetProperty(keyPath[i], out json)) return defaultValue;
                }
                return json.GetInt64();
            }
            catch (Exception ex)
            {
                if (!(ex is ArgumentNullException || ex is InvalidOperationException)) throw;
                if (!suppressLog) Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{string.Join("] -> [", keyPath)}] is not a signed 64-bit integer; check value");
                return defaultValue;
            }
        }

        public static sbyte GetSByte(string[] keyPath, sbyte defaultValue, bool suppressLog = false)
        {
            try
            {
                var json = State.RootElement;
                for (var i = 0; i < keyPath.Length; i++)
                {
                    if (!json.TryGetProperty(keyPath[i], out json)) return defaultValue;
                }
                return json.GetSByte();
            }
            catch (Exception ex)
            {
                if (!(ex is ArgumentNullException || ex is InvalidOperationException)) throw;
                if (!suppressLog) Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{string.Join("] -> [", keyPath)}] is not a signed byte; check value");
                return defaultValue;
            }
        }

        public static string GetString(string[] keyPath, string defaultValue, bool suppressLog = false)
        {
            try
            {
                var json = State.RootElement;
                for (var i = 0; i < keyPath.Length; i++)
                {
                    if (!json.TryGetProperty(keyPath[i], out json)) return defaultValue;
                }
                return json.GetString();
            }
            catch (Exception ex)
            {
                if (!(ex is ArgumentNullException || ex is InvalidOperationException)) throw;
                if (!suppressLog) Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{string.Join("] -> [", keyPath)}] is not a string; check value");
                return defaultValue;
            }
        }

        public static UInt16 GetUInt16(string[] keyPath, UInt16 defaultValue, bool suppressLog = false)
        {
            try
            {
                var json = State.RootElement;
                for (var i = 0; i < keyPath.Length; i++)
                {
                    if (!json.TryGetProperty(keyPath[i], out json)) return defaultValue;
                }
                return json.GetUInt16();
            }
            catch (Exception ex)
            {
                if (!(ex is ArgumentNullException || ex is InvalidOperationException)) throw;
                if (!suppressLog) Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{string.Join("] -> [", keyPath)}] is not an unsigned 16-bit integer; check value");
                return defaultValue;
            }
        }

        public static UInt32 GetUInt32(string[] keyPath, UInt32 defaultValue, bool suppressLog = false)
        {
            try
            {
                var json = State.RootElement;
                for (var i = 0; i < keyPath.Length; i++)
                {
                    if (!json.TryGetProperty(keyPath[i], out json)) return defaultValue;
                }
                return json.GetUInt32();
            }
            catch (Exception ex)
            {
                if (!(ex is ArgumentNullException || ex is InvalidOperationException)) throw;
                if (!suppressLog) Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{string.Join("] -> [", keyPath)}] is not an unsigned 32-bit integer; check value");
                return defaultValue;
            }
        }

        public static UInt64 GetUInt64(string[] keyPath, UInt64 defaultValue, bool suppressLog = false)
        {
            try
            {
                var json = State.RootElement;
                for (var i = 0; i < keyPath.Length; i++)
                {
                    json.TryGetProperty(keyPath[i], out json);
                }
                return json.GetUInt64();
            }
            catch (Exception ex)
            {
                if (!(ex is ArgumentNullException || ex is InvalidOperationException)) throw;
                if (!suppressLog) Logging.WriteLine(Logging.LogLevel.Error, Logging.LogType.Config, $"Setting [{string.Join("] -> [", keyPath)}] is not an unsigned 64-bit integer; check value");
                return defaultValue;
            }
        }

        public static void Initialize()
        {
            if (string.IsNullOrEmpty(Path))
            {
                SetPathToDefault();
            }

            Reset();
            Load();
        }

        public static void Load()
        {
            Logging.WriteLine(Logging.LogLevel.Warning, Logging.LogType.Config, $"Loading configuration from [{Path}]");

            if (!CanRead())
            {
                throw new InvalidOperationException();
            }

            try
            {
                var jsonOpts = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                    MaxDepth = 10
                };

                string json;
                using (var r = new StreamReader(Path))
                json = r.ReadToEnd();
                var fileState = JsonDocument.Parse(json, jsonOpts);

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
