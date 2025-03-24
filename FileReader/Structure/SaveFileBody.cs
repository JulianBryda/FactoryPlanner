using FactoryPlanner.FileReader.Structure.Properties;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace FactoryPlanner.FileReader.Structure
{
    internal class SaveFileBody : SaveFile
    {
        public SaveFileBody(ref BinaryReader reader) : base(ref reader)
        {
            UncompressedSize = reader.ReadUInt64();
            _ = reader.ReadUInt32(); // should be 6
            string none = ReadString(ref reader); // should be "None"
            _ = reader.ReadUInt32(); // should be 0
            _ = reader.ReadUInt32();
            _ = reader.ReadUInt32(); // should be 1
            string none2 = ReadString(ref reader); // should be "None"
            _ = reader.ReadUInt32();

            List<GroupingGrid> groupingGrids = [];
            for (int i = 0; i < 5; i++) // 5 grouping grids fix
            {
                groupingGrids.Add(new GroupingGrid(ref reader));
            }
            GroupingGrid = [.. groupingGrids];

            SublevelCount = reader.ReadUInt32();

            List<Level> levels = [];
            for (int i = 0; i <= SublevelCount; i++) // one more level than sublevel count, last one ist persistent level
            {
                levels.Add(new Level(ref reader, i == SublevelCount));
            }

            Levels = [.. levels];
        }

        public ulong UncompressedSize { get; set; }
        public GroupingGrid[] GroupingGrid { get; set; }
        public uint SublevelCount { get; set; }
        public Level[] Levels { get; set; }
    }

    internal class GroupingGrid : SaveFile
    {
        public GroupingGrid(ref BinaryReader reader) : base(ref reader)
        {
            GridName = ReadString(ref reader);
            _ = reader.ReadUInt32();
            _ = reader.ReadUInt32();
            LevelCount = reader.ReadUInt32();

            for (int i = 0; i < LevelCount; i++)
            {
                string test = ReadString(ref reader);
                _ = reader.ReadUInt32();
            }
        }

        public string GridName { get; set; }
        public uint LevelCount { get; set; }
    }

    internal class Level : SaveFile
    {
        public Level(ref BinaryReader reader, bool isPersistentLevel) : base(ref reader)
        {
            LevelName = (isPersistentLevel) ? "Level " : ReadString(ref reader);
            ObjectHeaderAndCollectablesSize = reader.ReadUInt64();
            ObjectHeaderCount = reader.ReadUInt32();

            long readIndex = reader.BaseStream.Position;

            List<ObjectHeader> headers = [];
            for (int i = 0; i < ObjectHeaderCount; i++)
            {
                headers.Add(new ObjectHeader(ref reader));
            }
            ObjectHeaders = [.. headers];

            readIndex = reader.BaseStream.Position - readIndex;

            CollectablesCount = reader.ReadUInt32();

            reader.BaseStream.Position += (long)(ObjectHeaderAndCollectablesSize - 8) - readIndex;
            //List<ObjectReference> collectables = [];
            //for (int i = 0; i < CollectablesCount; i++)
            //{
            //    collectables.Add(new ObjectReference(ref reader));
            //    Debug.WriteLine($"Collectable: {i} Stream Position: {reader.BaseStream.Position}");
            //}
            //Collectables = [.. collectables];

            ObjectsSize = reader.ReadUInt64();
            ObjectCount = reader.ReadUInt32();

            List<ActCompObject> actCompObjects = [];
            foreach (var header in ObjectHeaders)
            {
                if (header.Type == ObjectHeader.HeaderType.ActorHeader)
                {
                    actCompObjects.Add(new ActorObject(ref reader));
                }
                else
                {
                    actCompObjects.Add(new ComponentObject(ref reader));
                }
            }
            ActCompObjects = [.. actCompObjects];

            SecondCollectablesCount = reader.ReadUInt32();

            List<ObjectReference> secondCollectables = [];
            for (int i = 0; i < SecondCollectablesCount; i++)
            {
                secondCollectables.Add(new ObjectReference(ref reader));
            }
            SecondCollectables = [.. secondCollectables];
        }

        public string LevelName { get; set; } = string.Empty;
        public ulong ObjectHeaderAndCollectablesSize { get; set; }
        public uint ObjectHeaderCount { get; set; }
        public ObjectHeader[] ObjectHeaders { get; set; } = [];
        public uint CollectablesCount { get; set; }
        public ObjectReference[] Collectables { get; set; }
        public ulong ObjectsSize { get; set; }
        public uint ObjectCount { get; set; }
        public ActCompObject[] ActCompObjects { get; set; }
        public uint SecondCollectablesCount { get; set; }
        public ObjectReference[] SecondCollectables { get; set; }
    }

    internal class ObjectHeader : SaveFile
    {
        public ObjectHeader(ref BinaryReader reader) : base(ref reader)
        {
            Type = (HeaderType)reader.ReadUInt32();

            if (Type == HeaderType.ActorHeader)
            {
                ActCompHeader = new ActorHeader(ref reader);
            }
            else if (Type == HeaderType.ComponentHeader)
            {
                ActCompHeader = new ComponentHeader(ref reader);
            }
            else
            {
                throw new Exception($"Type Error in ObjectHeader on read position {reader.BaseStream.Position}!");
            }
        }

        public enum HeaderType : uint
        {
            ComponentHeader = 0,
            ActorHeader = 1
        }
        public HeaderType Type { get; set; }
        public ActCompHeader ActCompHeader { get; set; }
    }

    internal class ActCompHeader(ref BinaryReader reader) : SaveFile(ref reader)
    {
        public string TypePath { get; set; } = ReadString(ref reader);
        public string RootObject { get; set; } = ReadString(ref reader);
        public string InstanceName { get; set; } = ReadString(ref reader);
    }

    internal class ActorHeader : ActCompHeader
    {
        public ActorHeader(ref BinaryReader reader) : base(ref reader)
        {
            NeedTransform = reader.ReadUInt32();
            RotationX = reader.ReadSingle();
            RotationY = reader.ReadSingle();
            RotationZ = reader.ReadSingle();
            RotationW = reader.ReadSingle();
            PositionX = reader.ReadSingle();
            PositionY = reader.ReadSingle();
            PositionZ = reader.ReadSingle();
            ScaleX = reader.ReadSingle();
            ScaleY = reader.ReadSingle();
            ScaleZ = reader.ReadSingle();
            WasPlacedInLevel = reader.ReadUInt32();
        }

        public uint NeedTransform { get; set; }
        public float RotationX { get; set; }
        public float RotationY { get; set; }
        public float RotationZ { get; set; }
        public float RotationW { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float ScaleX { get; set; }
        public float ScaleY { get; set; }
        public float ScaleZ { get; set; }
        public uint WasPlacedInLevel { get; set; }
    }

    internal class ComponentHeader : ActCompHeader
    {
        public ComponentHeader(ref BinaryReader reader) : base(ref reader)
        {
            ParentActorName = ReadString(ref reader);
        }

        public string ParentActorName { get; set; }
    }

    internal class ActCompObject : SaveFile
    {
        public ActCompObject(ref BinaryReader reader) : base(ref reader)
        {
            SaveVersion = reader.ReadUInt32();
            Flag = reader.ReadUInt32();
            Size = reader.ReadUInt32();
        }

        public uint SaveVersion { get; set; }
        public uint Flag { get; set; }
        public uint Size { get; set; }
        public PropertyListEntry[] Properties { get; set; } = [];
    }

    internal class ActorObject : ActCompObject
    {
        public ActorObject(ref BinaryReader reader) : base(ref reader)
        {
            long newPosition = reader.BaseStream.Position + Size; // current position + size of object

            ParentObjectReference = new ObjectReference(ref reader);
            ComponentCount = reader.ReadUInt32();

            List<ObjectReference> components = [];
            for (int i = 0; i < ComponentCount; i++)
            {
                components.Add(new ObjectReference(ref reader));
            }
            Components = [.. components];

            List<PropertyListEntry> properties = [];
            do
            {
                properties.Add(new PropertyListEntry(ref reader));
            }
            while (properties.Last().Name != "None");
            Properties = [..properties[..^1]];

            reader.BaseStream.Position = newPosition;
        }

        public ObjectReference ParentObjectReference { get; set; }
        public uint ComponentCount { get; set; }
        public ObjectReference[] Components { get; set; }
        public byte[] TrailingBytes { get; set; } = [];
    }

    internal class ComponentObject : ActCompObject
    {
        public ComponentObject(ref BinaryReader reader) : base(ref reader)
        {
            long newPosition = reader.BaseStream.Position + Size; // current position + size of object

            List<PropertyListEntry> properties = [];
            do
            {
                properties.Add(new PropertyListEntry(ref reader));
            }
            while (properties.Last().Name != "None");
            Properties = [.. properties[..^1]];

            reader.BaseStream.Position = newPosition;
        }

        public byte[] TrailingBytes { get; set; } = [];
    }

    internal class ObjectReference(ref BinaryReader reader) : SaveFile(ref reader)
    {
        public string LevelName { get; set; } = ReadString(ref reader);
        public string PathName { get; set; } = ReadString(ref reader);
    }
}
