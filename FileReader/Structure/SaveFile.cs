using log4net;
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
    public abstract class SaveFile
    {
        protected static readonly ILog s_log = LogManager.GetLogger(typeof(SaveFile));
        public SaveFile(ref BinaryReader reader)
        {
        }

        protected static string ReadString(ref BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length == 0) return "";
#if DEBUG
            if (length >= 500)
                s_log.Warn($"Long string length \"{length}\" at position {reader.BaseStream.Position}!");
#endif

            byte[] bytes;
            string content;

            // UTF-16
            if (length < 0)
            {
                length *= -2;

                bytes = reader.ReadBytes(length);
                content = Encoding.Unicode.GetString(bytes);
            }
            // UTF-8
            else
            {
                bytes = reader.ReadBytes(length);
                content = Encoding.UTF8.GetString(bytes);
            }
                 
            return content[..^1];
        }
    }
}