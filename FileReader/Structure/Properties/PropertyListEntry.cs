using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.FileReader.Structure.Properties
{
    public class PropertyListEntry : Property
    {
        public PropertyListEntry(ref BinaryReader reader) : base(ref reader)
        {
            Name = ReadString(ref reader);
            if (Name != "None")
            {
                byte extrByteTest = reader.ReadByte();
                if (extrByteTest != 0)
                {
                    reader.BaseStream.Position -= 1;
                }

                Type = ReadString(ref reader);
                Size = reader.ReadUInt32();
                Index = reader.ReadUInt32();
                Property = CreateByType(Type, ref reader);
            }
        }

        public string Name { get; set; }
        public string Type { get; set; } = string.Empty;
        public uint Size { get; set; }
        public uint Index { get; set; }
        public Property? Property { get; set; }
    }
}
