using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FactoryPlanner.FileReader.Structure.Properties
{
    internal class Property(ref BinaryReader reader) : SaveFile(ref reader)
    {

        protected static Property CreateByType(string type, ref BinaryReader reader)
        {
            return type switch
            {
                "ArrayProperty" => new ArrayProperty(ref reader),
                "BoolProperty" => new BoolProperty(ref reader),
                "ByteProperty" => new ByteProperty(ref reader),
                "EnumProperty" => new EnumProperty(ref reader),
                "FloatProperty" => new FloatProperty(ref reader),
                "DoubleProperty" => new DoubleProperty(ref reader),
                "IntProperty" => new IntProperty(ref reader),
                "Int8Property" => new Int8Property(ref reader),
                "UInt32Property" => new UInt32Property(ref reader),
                "Int64Property" => new Int64Property(ref reader),
                "MapProperty" => new MapProperty(ref reader),
                "NameProperty" => new NameProperty(ref reader),
                "ObjectProperty" => new ObjectProperty(ref reader),
                "SoftObjectProperty" => new SoftObjectProperty(ref reader),
                "SetProperty" => new SetProperty(ref reader),
                "StrProperty" => new StrProperty(ref reader),
                "StructProperty" => new StructProperty(ref reader),
                "TextProperty" => new TextProperty(ref reader),
                _ => throw new Exception("Type not known!"),
            };
        }
        protected static Property CreateSimpleByType(string type, ref BinaryReader reader)
        {
            return type switch
            {
                "ByteProperty" => new SimpleByteProperty(ref reader),
                "EnumProperty" or "StrProperty" => new SimpleEnumStrProperty(ref reader),
                "InterfaceProperty" or "ObjectProperty" => new SimpleObjectProperty(ref reader),
                "IntProperty" => new SimpleIntProperty(ref reader),
                "Int64Property" => new SimpleInt64Property(ref reader),
                "FloatProperty" => new SimpleFloatProperty(ref reader),
                "SoftObjectProperty" => new SimpleSoftObjectProperty(ref reader),
                "StructProperty" => throw new Exception("Use new SimpleStructProperty() instead!"),
                _ => throw new Exception("Type not known!"),
            };
        }

        protected static Property? CheckTypedData(string type, ref BinaryReader reader)
        {
            return type switch
            {
                "IntVector4" => new IntVector4(ref reader),
                "Color" => new Color(ref reader),
                "Box" => new Box(ref reader),
                "FluidBox" => new FluidBox(ref reader),
                "InventoryItem" => new InventoryItem(ref reader),
                "LinearColor" => new LinearColor(ref reader),
                "Quat" => new Quat(ref reader),
                "Vector4" => new Quat(ref reader),
                "RailroadTrackPosition" => new RailroadTrackPosition(ref reader),
                "Vector" => new Vector(ref reader),
                "Rotator" => new Vector(ref reader),
                "Vector2D" => new Vector2D(ref reader),
                "DateTime" => new DateTime(ref reader),
                "ClientIdentityInfo" => new ClientIdentityInfo(ref reader),
                _ => null
            };
        }
    }


    internal class BoolProperty(ref BinaryReader reader) : Property(ref reader)
    {
        public byte Value { get; set; } = reader.ReadByte();
        public PropertyGuid PropertyGuid { get; set; } = new PropertyGuid(ref reader);
    }

    internal class ByteProperty : Property
    {
        public ByteProperty(ref BinaryReader reader) : base(ref reader)
        {
            Type = ReadString(ref reader);
            Padding = reader.ReadByte();

            Value = (Type == "None") ? reader.ReadByte().ToString() : ReadString(ref reader);
        }

        public string Type { get; set; } = string.Empty;
        public byte Padding { get; set; }
        public string Value { get; set; } = string.Empty; // if Type == None than this is byte
    }

    internal class EnumProperty(ref BinaryReader reader) : Property(ref reader)
    {
        public string Type { get; set; } = ReadString(ref reader);
        public PropertyGuid PropertyGuid { get; set; } = new PropertyGuid(ref reader);
        public string Value { get; set; } = ReadString(ref reader);
    }

    internal class FloatProperty(ref BinaryReader reader) : Property(ref reader)
    {
        public PropertyGuid PropertyGuid { get; set; } = new PropertyGuid(ref reader);
        public float Value { get; set; } = reader.ReadSingle();
    }

    internal class DoubleProperty(ref BinaryReader reader) : Property(ref reader)
    {
        public PropertyGuid PropertyGuid { get; set; } = new PropertyGuid(ref reader);
        public double Value { get; set; } = reader.ReadDouble();
    }

    internal class IntProperty(ref BinaryReader reader) : Property(ref reader)
    {
        public PropertyGuid PropertyGuid { get; set; } = new PropertyGuid(ref reader);
        public int Value { get; set; } = reader.ReadInt32();
    }

    internal class Int8Property(ref BinaryReader reader) : Property(ref reader)
    {
        public PropertyGuid PropertyGuid { get; set; } = new PropertyGuid(ref reader);
        public byte Value { get; set; } = reader.ReadByte();
    }

    internal class UInt32Property(ref BinaryReader reader) : Property(ref reader)
    {
        public PropertyGuid PropertyGuid { get; set; } = new PropertyGuid(ref reader);
        public uint Value { get; set; } = reader.ReadUInt32();
    }

    internal class Int64Property(ref BinaryReader reader) : Property(ref reader)
    {
        public PropertyGuid PropertyGuid { get; set; } = new PropertyGuid(ref reader);
        public long Value { get; set; } = reader.ReadInt64();
    }

    internal class MapProperty : Property
    {
        public MapProperty(ref BinaryReader reader) : base(ref reader)
        {
            KeyType = ReadString(ref reader);
            ValueType = ReadString(ref reader);
            reader.BaseStream.Position += 1; // padding
            ModeType = reader.ReadUInt32();
            NumberOfElements = reader.ReadUInt32();

            Dictionary<Property, Property> mapElements = [];
            for (int i = 0; i < NumberOfElements; i++)
            {
                Property keyProperty = KeyType switch
                {
                    "ObjectProperty" => new SimpleObjectProperty(ref reader),
                    "IntProperty" => new SimpleIntProperty(ref reader),
                    "StructProperty" => new KeyStructProperty(ref reader),
                    _ => throw new Exception("Unknow type!"),
                };

                Property valueProperty = ValueType switch
                {
                    "ByteProperty" => new SimpleByteProperty(ref reader),
                    "IntProperty" => new SimpleIntProperty(ref reader),
                    "Int64Property" => new SimpleInt64Property(ref reader),
                    "StructProperty" => new ValueStructProperty(ref reader),
                    "ObjectProperty" => new SimpleObjectProperty(ref reader),
                    _ => throw new Exception("Unknow type!"),
                };

                mapElements.Add(keyProperty, valueProperty);
            }
            MapElements = mapElements;
        }

        public string KeyType { get; set; }
        public string ValueType { get; set; }
        public byte Padding { get; set; }
        public uint ModeType { get; set; }
        public uint NumberOfElements { get; set; }
        public Dictionary<Property, Property> MapElements { get; set; }

        private class KeyStructProperty(ref BinaryReader reader) : Property(ref reader)
        {
            public int Value1 { get; set; } = reader.ReadInt32();
            public int Value2 { get; set; } = reader.ReadInt32();
            public int Value3 { get; set; } = reader.ReadInt32();
        }

        private class ValueStructProperty : Property
        {
            public ValueStructProperty(ref BinaryReader reader) : base(ref reader)
            {
                List<PropertyListEntry> properties = [];
                do
                {
                    properties.Add(new PropertyListEntry(ref reader));
                }
                while (properties.Last().Name != "None");
                Properties = [.. properties];
            }

            public Property[] Properties { get; set; }
        }
    }

    internal class NameProperty(ref BinaryReader reader) : Property(ref reader)
    {
        public PropertyGuid PropertyGuid { get; set; } = new PropertyGuid(ref reader);
        public string Value { get; set; } = ReadString(ref reader);
    }

    internal class ObjectProperty(ref BinaryReader reader) : Property(ref reader)
    {
        public PropertyGuid PropertyGuid { get; set; } = new PropertyGuid(ref reader);
        public ObjectReference Reference { get; set; } = new ObjectReference(ref reader);
    }

    internal class SoftObjectProperty(ref BinaryReader reader) : Property(ref reader)
    {
        public PropertyGuid PropertyGuid { get; set; } = new PropertyGuid(ref reader);
        public ObjectReference Reference { get; set; } = new ObjectReference(ref reader);
        public uint Value { get; set; } = reader.ReadUInt32();
    }

    internal class SetProperty : Property
    {
        public SetProperty(ref BinaryReader reader) : base(ref reader)
        {
            Type = ReadString(ref reader);
            reader.BaseStream.Position += 5; // padding
            Length = reader.ReadUInt32();

            List<Property> properties = [];
            for (int i = 0; i < Length; i++)
            {
                properties.Add(Type switch
                {
                    "UInt32Property" => new SimpleUint32Property(ref reader),
                    "StructProperty" => new SetStructProperty(ref reader),
                    "ObjectProperty" => new SimpleObjectProperty(ref reader),
                    _ => throw new Exception("Type unknown!")
                });
            }
            SetElements = [.. properties];
        }

        public string Type { get; set; }
        public byte Padding { get; set; }
        public uint Padding2 { get; set; }
        public uint Length { get; set; }
        public Property[] SetElements { get; set; }

        private class SetStructProperty(ref BinaryReader reader) : Property(ref reader)
        {
            public ulong Value1 { get; set; } = reader.ReadUInt64();
            public ulong Value2 { get; set; } = reader.ReadUInt64();
        }
    }

    internal class StrProperty(ref BinaryReader reader) : Property(ref reader)
    {
        public PropertyGuid PropertyGuid { get; set; } = new PropertyGuid(ref reader);
        public string Value { get; set; } = ReadString(ref reader);
    }

    internal class StructProperty : Property
    {
        public StructProperty(ref BinaryReader reader) : base(ref reader)
        {
            Type = ReadString(ref reader);
            _ = reader.ReadBytes(17); // padding

            Property? typedData = CheckTypedData(Type, ref reader);
            if (typedData != null)
            {
                Properties = [typedData];
                return;
            }

            List<PropertyListEntry> properties = [];
            do
            {
                properties.Add(new PropertyListEntry(ref reader));
            }
            while (properties.Last().Name != "None");
            Properties = [.. properties];
        }

        public string Type { get; set; }
        public Property[] Properties { get; set; }
    }

    internal class TextProperty(ref BinaryReader reader) : Property(ref reader) // could be bwoken
    {
        public PropertyGuid PropertyGuid { get; set; } = new PropertyGuid(ref reader);
        public uint Flags { get; set; } = reader.ReadUInt32();
        public byte HistoryType { get; set; } = reader.ReadByte();
        public uint IsTextCultureInvariant { get; set; } = reader.ReadUInt32();
        public string Value { get; set; } = ReadString(ref reader);
    }

    internal class PropertyGuid
    {
        public PropertyGuid(ref BinaryReader reader)
        {
            HasGuid = reader.ReadByte();
            if (HasGuid == 1)
            {
                Guid = reader.ReadBytes(16);
            }
        }

        public byte HasGuid { get; set; }
        public byte[] Guid { get; set; } = [];
    }
}
