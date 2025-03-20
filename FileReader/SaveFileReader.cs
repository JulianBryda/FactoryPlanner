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

namespace FactoryPlanner.FileReader
{
    internal class SaveFileReader
    {
        public delegate void ProgressUpdateHandler(object sender, float value);
        public event ProgressUpdateHandler OnProgressUpdate;

        public delegate void FinishHandler(object sender);
        public event FinishHandler OnFinish;

        private static SaveFileReader? _loadedSaveFile;
        public static SaveFileReader LoadedSaveFile
        {
            get
            {
                _loadedSaveFile ??= new SaveFileReader(null, true);
                return _loadedSaveFile;
            }
            private set
            {
                _loadedSaveFile = value;
            }
        }

        public string Path { get; set; }
        public SaveFileHeader Header { get; set; }
        public SaveFileBody Body { get; set; }

        public SaveFileReader(string? path = null, bool blockThread = false)
        {
            if (path != null)
            {
                Path = path;
            }
            else
            {
                Path = GetNewestSavePath();
            }
            LoadedSaveFile = this;

            var task = Task.Run(() =>
            {
                byte[] bytes = File.ReadAllBytes(Path);
                MemoryStream stream = new(bytes);
                BinaryReader reader = new(stream);

                Header = new SaveFileHeader(ref reader);

                MemoryStream bodyStream = new();
                BinaryReader bodyReader = new(bodyStream);

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var memStream = DecompressZlib(new SaveFileBodyCompressed(ref reader).CompressedBytes);
                    memStream.Position = 0;
                    memStream.CopyTo(bodyStream);
                    memStream.Dispose();
                }
                stream.Dispose();
                reader.Dispose();

                bodyStream.Position = 0;

                Task.Run(() => ProgressUpdater(ref bodyStream));

                Body = new SaveFileBody(ref bodyReader);

                OnFinish?.Invoke(this);
            });

            if (blockThread)
                task.Wait();
        }


        public List<ActCompObject> GetActCompObjects(int typePathHash)
        {
            List<ActCompObject> objects = [];
            var persistantLevel = Body.Levels.Last();

            for (int i = 0; i < persistantLevel.ObjectHeaders.Length; i++)
            {
                int hash = persistantLevel.ObjectHeaders[i].ActCompHeader.TypePath.GetHashCode();

                if (typePathHash == hash)
                    objects.Add(persistantLevel.ActCompObjects[i]);

            }

            return objects;
        }

        public ActCompObject? GetActCompObject(string pathName)
        {
            var persistantLevel = Body.Levels.Last();
            int pathNameHash = pathName.GetHashCode();

            for (int i = 0; i < persistantLevel.ObjectHeaders.Length; i++)
            {
                int hash = persistantLevel.ObjectHeaders[i].ActCompHeader.InstanceName.GetHashCode();

                if (pathNameHash == hash)
                    return persistantLevel.ActCompObjects[i];

            }

            return null;
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

            DateTime lastWrite = DateTime.MinValue;
            foreach (var file in Directory.GetFiles(filePath))
            {
                DateTime last = File.GetLastWriteTimeUtc(file);
                if (last > lastWrite)
                {
                    lastWrite = last;
                    filePath = file;
                }
            }

            return filePath;
        }
    }
}
