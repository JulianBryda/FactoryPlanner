using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Reference = new ObjectReference(ref reader);
            State = reader.ReadUInt32();
            if (State == 0) return;

            ItemState = new ObjectReference(ref reader);
            ItemStateLength = reader.ReadUInt32();

            List<PropertyListEntry> properties = [];
            do
            {
                properties.Add(new PropertyListEntry(ref reader));
            }
            while (properties.Last().Name != "None");
            Properties = [.. properties[..^1]];
        }

        public ObjectReference Reference { get; set; }
        public uint State { get; set; }
        public ObjectReference? ItemState { get; set; }
        public uint ItemStateLength { get; set; }
        public PropertyListEntry[] Properties { get; set; } = [];
    }

    internal class Color(ref BinaryReader reader) : Property(ref reader)
    {
        public byte B { get; set; } = reader.ReadByte();
        public byte G { get; set; } = reader.ReadByte();
        public byte R { get; set; } = reader.ReadByte();
        public byte A { get; set; } = reader.ReadByte();
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
        public double X { get; set; } = reader.ReadDouble();
        public double Y { get; set; } = reader.ReadDouble();
        public double Z { get; set; } = reader.ReadDouble();
        public double W { get; set; } = reader.ReadDouble();
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

    internal class Vector2D(ref BinaryReader reader) : Property(ref reader)
    {
        public double X { get; set; } = reader.ReadDouble();
        public double Y { get; set; } = reader.ReadDouble();
    }

    internal class IntVector4(ref BinaryReader reader) : Property(ref reader)
    {
        public int X { get; set; } = reader.ReadInt32();
        public int Y { get; set; } = reader.ReadInt32();
        public int Z { get; set; } = reader.ReadInt32();
        public int W { get; set; } = reader.ReadInt32();
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
            IdentityCount = reader.ReadUInt32();

            List<Identity> identities = [];
            for (int i = 0; i < IdentityCount; i++)
            {
                identities.Add(new Identity(ref reader));
            }
            Identities = [.. identities];
        }

        public string UUID { get; set; }
        public uint IdentityCount { get; set; }
        public Identity[] Identities { get; set; }

        public class Identity : Property
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

    internal class SkipBytes : Property
    {
        public SkipBytes(ref BinaryReader reader, int byteCount) : base(ref reader)
        {
            reader.BaseStream.Position += byteCount; // skip bytes cause content not needed 
        }
    }

    internal class VectorNetQuantize : Property
    {
        public VectorNetQuantize(ref BinaryReader reader) : base(ref reader)
        {
            List<PropertyListEntry> properties = [];
            do
            {
                properties.Add(new PropertyListEntry(ref reader));
            }
            while (properties.Last().Name != "None");
            Properties = [.. properties[..^1]];
        }

        public Property[] Properties { get; set; }
    }
}
