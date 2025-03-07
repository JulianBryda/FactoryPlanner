using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.FileReader.Structure.Properties
{
    internal class ArrayProperty : Property
    {
        public ArrayProperty(ref BinaryReader reader) : base(ref reader)
        {
            Type = ReadString(ref reader);
            Padding = reader.ReadByte();
            Length = reader.ReadUInt32();

            List<Property> properties = [];
            for (int i = 0; i < Length; i++)
            {
                if (Type == "StructProperty")
                {
                    properties.Add(new SimpleStructProperty(ref reader, Length));
                    break;
                }

                properties.Add(CreateSimpleByType(Type, ref reader));
            }
            Properties = [.. properties];
        }

        public string Type { get; set; }
        public byte Padding { get; set; }
        public uint Length { get; set; }
        public Property[] Properties { get; set; }
    }
}
