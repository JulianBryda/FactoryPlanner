using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.FileReader.Structure
{
    abstract class SaveFile
    {
        public SaveFile(ref BinaryReader reader)
        {
        }

        private static object? ReadFromBinary(ref BinaryReader reader, Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Byte => reader.ReadByte(),
                TypeCode.SByte => reader.ReadSByte(),
                TypeCode.Int16 => reader.ReadInt16(),
                TypeCode.UInt16 => reader.ReadUInt16(),
                TypeCode.Int32 => reader.ReadInt32(),
                TypeCode.UInt32 => reader.ReadUInt32(),
                TypeCode.Int64 => reader.ReadInt64(),
                TypeCode.UInt64 => reader.ReadUInt64(),
                TypeCode.Single => reader.ReadSingle(),
                TypeCode.Double => reader.ReadDouble(),
                TypeCode.Decimal => reader.ReadDecimal(),
                TypeCode.String => ReadString(ref reader),
                TypeCode.Boolean => reader.ReadBoolean(),
                TypeCode.Char => reader.ReadChar(),
                _ => null,
            };
        }

        protected static string ReadString(ref BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length == 0) return "";

            byte[] bytes;
            string content;

            // UTF-16
            if (length < 0)
            {
                length *= -2;

                bytes = reader.ReadBytes(length);
                content = Encoding.Unicode.GetString(bytes);
            }
            else
            {
                bytes = reader.ReadBytes(length);
                content = Encoding.UTF8.GetString(bytes);
            }

            return content[..^1];
        }
    }
}