using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace FactoryPlanner.FileReader.Structure
{
    internal class SaveFileHeader(ref BinaryReader reader) : SaveFile(ref reader)
    {
        public uint HeaderVersion { get; set; } = reader.ReadUInt32();
        public uint SaveVersion { get; set; } = reader.ReadUInt32();
        public uint BuildVersion { get; set; } = reader.ReadUInt32();
        public string MapName { get; set; } = ReadString(ref reader);
        public string MapOptions { get; set; } = ReadString(ref reader);
        public string SessionName { get; set; } = ReadString(ref reader);
        public uint PlayedSeconds { get; set; } = reader.ReadUInt32();
        public ulong SaveTimestamp { get; set; } = reader.ReadUInt64();
        public byte SessionVisibility { get; set; } = reader.ReadByte();
        public uint EditorObjectVersion { get; set; } = reader.ReadUInt32();
        public string ModMetadata {  get; set; } = ReadString(ref reader);
        public uint ModFlags { get; set; } = reader.ReadUInt32();
        public string SaveIdentifier { get; set; } = ReadString(ref reader);
        public uint IsWorldPartitioned { get; set; } = reader.ReadUInt32();
        public byte[] MD5Hash { get; set; } = reader.ReadBytes(20);
        public int IsCreativeMode { get; set; } = reader.ReadInt32();
    }
}
