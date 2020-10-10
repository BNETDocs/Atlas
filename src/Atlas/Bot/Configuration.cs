using Atlas.Battlenet;

using System;
using System.Collections.Generic;
using System.IO;

namespace Atlas.Bot
{
    class Configuration
    {
        private const byte FileVersion = 1;
        private enum Sections
        {
            General = 0,
            Instance = 1,
            ACL = 2,
        }

        public static Configuration State = null;
        public static string Path = "Atlas.bin";

        public bool CheckForUpdates;
        public List<Instance> Instances;

        public Configuration()
        {
            Load();

            if (Instances == null || Instances.Count == 0)
            {
                Console.WriteLine("[Config] No instances present, creating one.");
                Instance instance = new Instance(true);
                Instances.Add(instance);
            }
        }

        public void Load()
        {
            Reset();
            Console.WriteLine("[Config] Loading configuration.");

            Stream _stream = null;
            Battlenet.BinaryReader buffer = null;
            
            try {
                _stream = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                buffer = new Battlenet.BinaryReader(_stream);

                byte FileVersion = buffer.ReadByte();
                if (FileVersion != 1)
                {
                    Console.Error.WriteLine("[Config] Version is incompatible.");
                }

                Instance instance;

                while (buffer.BaseStream.Position < buffer.BaseStream.Length)
                {
                    byte section = buffer.ReadByte();
                    switch (section)
                    {
                        case (byte)Sections.General:
                            {
                                CheckForUpdates = buffer.ReadBoolean();
                                break;
                            }
                        case (byte)Sections.Instance:
                            {
                                instance = new Instance(false);
                                instance.Deserialize(buffer);
                                Instances.Add(instance);
                                break;
                            }
                        default:
                            {
                                throw new NotImplementedException();
                            }
                    }
                }
            }
            catch (EndOfStreamException)
            {
                Console.Error.WriteLine("[Config] Unexpected end of stream. Possible corruption?");
            }
            catch (NotImplementedException)
            {
                Console.Error.WriteLine("[Config] Invalid section. Possible corruption?");
            }
            finally
            {
                if (buffer != null)
                    buffer.Dispose();

                if (_stream != null)
                    _stream.Dispose();
            }
        }

        public void Reset()
        {
            Console.WriteLine("[Config] Resetting to default configuration.");

            CheckForUpdates = true;
            Instances = new List<Instance>();
        }
        
        public void Save()
        {
            Console.WriteLine("[Config] Saving configuration.");

            Stream _stream = null;
            Battlenet.BinaryWriter buffer = null;

            try
            {
                _stream = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                buffer = new Battlenet.BinaryWriter(_stream);

                buffer.Write((byte)1); // File Version

                buffer.Write((byte)Sections.General);
                buffer.Write(CheckForUpdates);

                if (Instances != null)
                {
                    foreach (Instance instance in Instances)
                    {
                        buffer.Write((byte)Sections.Instance);
                        instance.Serialize(buffer);
                    }
                }
            }
            finally
            {
                if (buffer != null)
                    buffer.Dispose();

                if (_stream != null)
                    _stream.Dispose();
            }
        }

    }
}
