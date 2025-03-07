using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.FileReader.Structure.Properties
{
    internal class Box(ref BinaryReader reader) : Property(ref reader)
    {
        public double MinX { get; set; } = reader.ReadDouble();
        public double MinY { get; set; } = reader.ReadDouble();
        public double MinZ { get; set; } = reader.ReadDouble();
        public double MaxX { get; set; } = reader.ReadDouble();
        public double MaxY { get; set; } = reader.ReadDouble();
        public double MaxZ { get; set; } = reader.ReadDouble();
        public byte IsValid { get; set; } = reader.ReadByte();
    }

    internal class FluidBox(ref BinaryReader reader) : Property(ref reader)
    {
        public float Value { get; set; } = reader.ReadSingle();
    }

    internal class InventoryItem : Property
    {
        public InventoryItem(ref BinaryReader reader) : base(ref reader)
        {
            Padding = reader.ReadUInt32();
            ItemName = ReadString(ref reader);
            HasFlag = reader.ReadUInt32();
            Padding2 = reader.ReadUInt32();
            ItemType = ReadString(ref reader);
            PropertySize = reader.ReadUInt32();

            List<PropertyListEntry> properties = [];
            do
            {
                properties.Add(new PropertyListEntry(ref reader));
            }
            while (properties.Last().Name != "None");
            Properties = [.. properties];
        }

        public uint Padding { get; set; }
        public string ItemName { get; set; }
        public uint HasFlag { get; set; }
        public uint Padding2 { get; set; }
        public string ItemType { get; set; }
        public uint PropertySize { get; set; }
        public PropertyListEntry[] Properties { get; set; }
    }

    internal class LinearColor(ref BinaryReader reader) : Property(ref reader)
    {
        public float R { get; set; } = reader.ReadSingle();
        public float G { get; set; } = reader.ReadSingle();
        public float B { get; set; } = reader.ReadSingle();
        public float A { get; set; } = reader.ReadSingle();
    }

    internal class Quat(ref BinaryReader reader) : Property(ref reader)
    {
        public float X { get; set; } = reader.ReadSingle();
        public float Y { get; set; } = reader.ReadSingle();
        public float Z { get; set; } = reader.ReadSingle();
        public float W { get; set; } = reader.ReadSingle();
    }

    internal class RailroadTrackPosition(ref BinaryReader reader) : Property(ref reader)
    {
        public ObjectReference Reference { get; set; } = new ObjectReference(ref reader);
        public float Offset { get; set; } = reader.ReadSingle();
        public float Forward { get; set; } = reader.ReadSingle();
    }

    internal class Vector(ref BinaryReader reader) : Property(ref reader)
    {
        public double X { get; set; } = reader.ReadDouble();
        public double Y { get; set; } = reader.ReadDouble();
        public double Z { get; set; } = reader.ReadDouble();
    }

    internal class DateTime(ref BinaryReader reader) : Property(ref reader)
    {
        public long Value { get; set; } = reader.ReadInt64();
    }

    internal class ClientIdentityInfo : Property
    {
        public ClientIdentityInfo(ref BinaryReader reader) : base(ref reader)
        {
            UUID = ReadString(ref reader);

        }

        public string UUID { get; set; }
        public uint IdentityCount { get; set; }

        private class Identity : Property
        {
            public Identity(ref BinaryReader reader) : base(ref reader)
            {
                Platform = reader.ReadByte();
                DataSize = reader.ReadUInt32();

                reader.BaseStream.Position += DataSize;
            }

            public byte Platform { get; set; } // 1 = Epic | 6 = Steam
            public uint DataSize { get; set; }
            // Identity Data skiped for now
        }
    }
}
