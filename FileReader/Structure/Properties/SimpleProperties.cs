using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.FileReader.Structure.Properties
{
    internal class SimpleByteProperty(ref BinaryReader reader) : Property(ref reader)
    {
        public byte Value { get; set; } = reader.ReadByte();
    }

    internal class SimpleEnumStrProperty(ref BinaryReader reader) : Property(ref reader)
    {
        public string Value { get; set; } = ReadString(ref reader);
    }

    internal class SimpleObjectProperty(ref BinaryReader reader) : Property(ref reader)
    {
        public ObjectReference Value { get; set; } = new ObjectReference(ref reader);
    }

    internal class SimpleIntProperty(ref BinaryReader reader) : Property(ref reader)
    {
        public int Value { get; set; } = reader.ReadInt32();
    }
    internal class SimpleUint32Property(ref BinaryReader reader) : Property(ref reader)
    {
        public uint Value { get; set; } = reader.ReadUInt32();
    }

    internal class SimpleInt64Property(ref BinaryReader reader) : Property(ref reader)
    {
        public long Value { get; set; } = reader.ReadInt64();
    }

    internal class SimpleFloatProperty(ref BinaryReader reader) : Property(ref reader)
    {
        public float Value { get; set; } = reader.ReadSingle();
    }

    internal class SimpleSoftObjectProperty(ref BinaryReader reader) : Property(ref reader)
    {
        public ObjectReference Reference { get; set; } = new ObjectReference(ref reader);
        public uint Value { get; set; } = reader.ReadUInt32();
    }

    internal class SimpleStructProperty : Property
    {
        public SimpleStructProperty(ref BinaryReader reader, uint length) : base(ref reader)
        {
            Name = ReadString(ref reader);
            Type = ReadString(ref reader);
            Size = reader.ReadUInt32();
            reader.BaseStream.Position += 4; // padding
            ElementType = ReadString(ref reader);
            reader.BaseStream.Position += 17; // padding

            List<Property> data = [];
            for (int i = 0; i < length; i++)
            {
                Property? typedData = CheckTypedData(ElementType, ref reader);
                if (typedData != null)
                {
                    data.Add(typedData);
                    continue;
                }

                bool shouldContinue;
                do
                {
                    var entry = new PropertyListEntry(ref reader);
                    
                    shouldContinue = entry.Name != "None";
                    
                    if (shouldContinue)
                        data.Add(entry);
                }
                while (shouldContinue);
            }
            Data = [.. data];

        }

        public string Name { get; set; }
        public string Type { get; set; }
        public uint Size { get; set; }
        public uint Padding { get; set; }
        public string ElementType { get; set; }
        public uint Padding2 { get; set; }
        public uint Padding3 { get; set; }
        public uint Padding4 { get; set; }
        public uint Padding5 { get; set; }
        public byte Padding6 { get; set; }
        public Property[] Data { get; set; }

    }
}
