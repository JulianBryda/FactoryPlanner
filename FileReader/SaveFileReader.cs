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


        public string Path { get; set; }
        public SaveFileHeader Header { get; set; }
        public SaveFileBody Body { get; set; }

        public SaveFileReader(string path)
        {
            Path = path;

            Task.Run(() =>
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
        }


        public ActCompObject? GetActCompObject(string typePath)
        {
            var persistantLevel = Body.Levels.Last();
            int firstHash = typePath.GetHashCode();

            for (int i = 0; i < persistantLevel.ObjectHeaders.Length; i++)
            {
                int secondHash = persistantLevel.ObjectHeaders[i].ActCompHeader.TypePath.GetHashCode();

                if (firstHash == secondHash)
                {
                    return persistantLevel.ActCompObjects[i];
                }

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
    }
}
