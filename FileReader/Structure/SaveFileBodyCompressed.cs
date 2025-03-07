using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace FactoryPlanner.FileReader.Structure
{
    internal class SaveFileBodyCompressed : SaveFile
    {
        public SaveFileBodyCompressed(ref BinaryReader reader) : base(ref reader)
        {
            PackageSignature = reader.ReadUInt32();
            ArchiveHeader = reader.ReadUInt32();
            Padding = reader.ReadByte();
            MaxChunkSize = reader.ReadUInt32();
            CompressionAlgorithm = reader.ReadUInt32();
            CompressedSize = reader.ReadUInt64();
            UncompressedSize = reader.ReadUInt64();
            CompressedSize2 = reader.ReadUInt64();
            UncompressedSize2 = reader.ReadUInt64();
            CompressedBytes = reader.ReadBytes((int)CompressedSize);
        }

        public uint PackageSignature { get; set; } 
        public uint ArchiveHeader { get; set; }
        public byte Padding { get; set; }
        public uint MaxChunkSize { get; set; }
        public uint CompressionAlgorithm { get; set; }
        public ulong CompressedSize { get; set; }
        public ulong UncompressedSize { get; set; } 
        public ulong CompressedSize2 { get; set; }
        public ulong UncompressedSize2 { get; set; }
        public byte[] CompressedBytes { get; set; }
    }
}
