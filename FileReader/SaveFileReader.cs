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
        private const uint PACKAGE_SIGNATURE = 0x9E2A83C1;

        public string Path { get; set; }
        public SaveFileHeader Header { get; set; }
        public List<SaveFileBodyCompressed> BodyCompressedList { get; set; } = [];
        public SaveFileBody Body { get; set; }

        public SaveFileReader(string path)
        {
            Path = path;

            byte[] bytes = File.ReadAllBytes(Path);
            MemoryStream stream = new(bytes);
            BinaryReader reader = new(stream);

            Header = new SaveFileHeader(ref reader);

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                BodyCompressedList.Add(new SaveFileBodyCompressed(ref reader));
            }
            stream.Dispose();
            reader.Dispose();

            stream = new MemoryStream();

            for (int i = 0; i < BodyCompressedList.Count; i++)
            {
                var memStream = DecompressZlib(BodyCompressedList[i].CompressedBytes);
                memStream.Position = 0;
                memStream.CopyTo(stream);
                memStream.Dispose();
            }
            stream.Position = 0;
            reader = new BinaryReader(stream);

            File.WriteAllBytes($"C:\\Users\\Julian\\source\\repos\\FactoryPlanner\\DecompressedBytes\\DecompressedBytes.txt", stream.GetBuffer());

            Body = new SaveFileBody(ref reader);
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
