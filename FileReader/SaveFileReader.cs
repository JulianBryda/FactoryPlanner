using FactoryPlanner.FileReader.Structure;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.Zip.Compression;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using log4net;
using System.Security.Cryptography;
using System.Reflection.Metadata;
using FactoryPlanner.FileReader.Structure.Properties;

namespace FactoryPlanner.FileReader
{
    internal class SaveFileReader
    {
        public delegate void ProgressUpdateHandler(object sender, float value);
        public event ProgressUpdateHandler? OnProgressUpdate;

        public delegate void FinishHandler(object sender);
        public event FinishHandler? OnFinish;

        private static SaveFileReader? s_loadedSaveFile;
        public static SaveFileReader LoadedSaveFile
        {
            get
            {
                s_loadedSaveFile ??= new SaveFileReader(null, true);
                return s_loadedSaveFile;
            }
            private set
            {
                s_loadedSaveFile = value;
            }
        }

        public string Path { get; set; }
        public SaveFileHeader Header { get; set; }
        public SaveFileBody Body { get; set; }


        private static readonly ILog s_log = LogManager.GetLogger(typeof(SaveFileReader));

        public SaveFileReader(string? path = null, bool blockThread = false)
        {
            Path = path ?? GetNewestSavePath();
            LoadedSaveFile = this;

            s_log.Info($"Loading save file \"{Path}\"...");

            var task = Task.Run(() =>
            {
                byte[] bytes = File.ReadAllBytes(Path);
                MemoryStream stream = new(bytes);
                BinaryReader reader = new(stream);

                s_log.Info("Loading save file header...");
                Header = new SaveFileHeader(ref reader);
                s_log.Info("Successfully loaded save file header!");

                MemoryStream bodyStream = new();
                BinaryReader bodyReader = new(bodyStream);

                s_log.Info("Inflating chunks...");
                int chunkCount = 0;
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var memStream = DecompressZlib(new SaveFileBodyCompressed(ref reader).CompressedBytes);
                    memStream.Position = 0;
                    memStream.CopyTo(bodyStream);
                    memStream.Dispose();

                    chunkCount++;
                }
                stream.Dispose();
                reader.Dispose();
                s_log.Info($"Successfully inflated {chunkCount} chunks!");

                bodyStream.Position = 0;

                Task.Run(() => ProgressUpdater(ref bodyStream));

                s_log.Info("Loading save file body...");
                Body = new SaveFileBody(ref bodyReader);
                s_log.Info("Successfully loaded save file body!");

                bodyStream.Dispose();
                bodyReader.Dispose();

                OnFinish?.Invoke(this);

                s_log.Info("Finished loading save file!");
            });

            if (blockThread)
                task.Wait();
        }

        public enum Type
        {
            TypePath,
            RootObject,
            InstanceName,
            Index
        }


        public List<ActCompObject> GetActCompObjects(string value, Type type = Type.TypePath) => GetActCompObjects(value.GetHashCode(), type);
        public List<ActCompObject> GetActCompObjects(int value, Type type = Type.TypePath)
        {
            List<ActCompObject> objects = [];

            foreach (var level in Body.Levels)
            {
                for (int i = 0; i < level.ObjectHeaders.Length; i++)
                {
                    int hash = type switch
                    {
                        Type.TypePath => level.ObjectHeaders[i].ActCompHeader.TypePath.GetHashCode(),
                        Type.RootObject => level.ObjectHeaders[i].ActCompHeader.RootObject.GetHashCode(),
                        Type.InstanceName => level.ObjectHeaders[i].ActCompHeader.InstanceName.GetHashCode(),
                        _ => throw new NotImplementedException()
                    };

                    if (value == hash)
                        objects.Add(level.ActCompObjects[i]);

                }
            }

            return objects;
        }

        public ActCompObject? GetActCompObject(string value, Type type = Type.InstanceName) => GetActCompObject(value.GetHashCode(), type);
        public ActCompObject? GetActCompObject(int value, Type type = Type.InstanceName)
        {
            if (type == Type.Index)
            {
                return Body.Levels.Last().ActCompObjects[value];
            }

            foreach (var level in Body.Levels)
            {
                for (int i = 0; i < level.ObjectHeaders.Length; i++)
                {
                    int hash = type switch
                    {
                        Type.TypePath => level.ObjectHeaders[i].ActCompHeader.TypePath.GetHashCode(),
                        Type.RootObject => level.ObjectHeaders[i].ActCompHeader.RootObject.GetHashCode(),
                        Type.InstanceName => level.ObjectHeaders[i].ActCompHeader.InstanceName.GetHashCode(),
                        _ => throw new NotImplementedException()
                    };

                    if (value == hash)
                        return level.ActCompObjects[i];

                }
            }

            return null;
        }

        public ObjectHeader? GetObjectHeader(string value, Type type = Type.InstanceName) => GetObjectHeader(value.GetHashCode(), type);
        public ObjectHeader? GetObjectHeader(int value, Type type = Type.InstanceName)
        {
            if (type == Type.Index)
            {
                return Body.Levels.Last().ObjectHeaders[value];
            }

            foreach (var level in Body.Levels)
            {
                for (int i = 0; i < level.ObjectHeaders.Length; i++)
                {
                    int hash = type switch
                    {
                        Type.TypePath => level.ObjectHeaders[i].ActCompHeader.TypePath.GetHashCode(),
                        Type.RootObject => level.ObjectHeaders[i].ActCompHeader.RootObject.GetHashCode(),
                        Type.InstanceName => level.ObjectHeaders[i].ActCompHeader.InstanceName.GetHashCode(),
                        _ => throw new NotImplementedException()
                    };

                    if (value == hash)
                        return level.ObjectHeaders[i];

                }
            }

            return null;
        }

        /// <summary>
        /// finds the index of the ActCompObject using the pathName
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns>the index of the ActCompObject if found, else -1</returns>
        public int GetIndexOf(string pathName)
        {
            var persistentLevel = Body.Levels.Last();

            for (int i = 0; i < persistentLevel.ObjectHeaders.Length; i++)
            {
                if (pathName == persistentLevel.ObjectHeaders[i].ActCompHeader.InstanceName)
                {
                    return i;
                }
            }

            return -1;
        }

        public int CountActCompHeader(int typePathHash)
        {
            var persistantLevel = Body.Levels.Last();
            int count = 0;

            for (int i = 0; i < persistantLevel.ObjectHeaders.Length; i++)
            {
                int hash = persistantLevel.ObjectHeaders[i].ActCompHeader.TypePath.GetHashCode();

                if (typePathHash == hash)
                    count++;
            }

            return count;
        }

        public static PropertyListEntry? GetPropertyByName(ActCompObject obj, string name)
        {
            foreach (var entry in obj.Properties)
            {
                if (entry.Name == name)
                    return entry;
            }

            return null;
        }



        private void ProgressUpdater(ref MemoryStream stream)
        {
            while (Body == null)
            {
                float prog = (float)stream.Position / stream.Length * 100f;
                OnProgressUpdate?.Invoke(this, prog);

                Thread.Sleep(100);
            }

            OnProgressUpdate?.Invoke(this, 100f);
        }

        private static MemoryStream DecompressZlib(byte[] compressedData)
        {
            using MemoryStream inputStream = new(compressedData);
            using InflaterInputStream inflaterStream = new(inputStream, new Inflater());
            MemoryStream outputStream = new();

            inflaterStream.CopyTo(outputStream);
            return outputStream;
        }

        private static string GetNewestSavePath()
        {
            string? filePath = Environment.GetEnvironmentVariable("LocalAppdata");
            if (filePath == null) throw new ArgumentNullException("Failed to get Path to %Localappdata%!");

            filePath += "\\FactoryGame\\Saved\\SaveGames";

            foreach (var dir in Directory.GetDirectories(filePath))
            {
                string dirName = System.IO.Path.GetFileName(dir);
                if (dirName.All(char.IsDigit))
                {
                    filePath = dir;
                    break;
                }
            }

            System.DateTime lastWrite = System.DateTime.MinValue;
            foreach (var file in Directory.GetFiles(filePath))
            {
                System.DateTime last = File.GetLastWriteTimeUtc(file);
                if (System.IO.Path.GetExtension(file) == ".sav" && last > lastWrite)
                {
                    lastWrite = last;
                    filePath = file;
                }
            }

            return filePath;
        }
    }
}
