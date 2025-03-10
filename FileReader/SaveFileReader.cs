using FactoryPlanner.FileReader.Structure;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.Zip.Compression;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.FileReader
{
    internal class SaveFileReader
    {
        public string Path { get; set; }
        public SaveFileHeader Header { get; set; }
        public SaveFileBody Body { get; set; }

        public SaveFileReader(string path)
        {
            Path = path;

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

            Body = new SaveFileBody(ref bodyReader);
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
