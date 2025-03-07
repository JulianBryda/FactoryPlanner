using FactoryPlanner.FileReader.Structure.Properties;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
                Debug.WriteLine($"Sublevel: {i} Stream Position: {reader.BaseStream.Position}");
                levels.Add(new Level(ref reader, i == SublevelCount));
            }
            var test = levels.Last();

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

            List<ActorObject> actorObjects = [];
            List<ComponentObject> componentObjects = [];
            foreach (var header in ObjectHeaders)
            {
                if (header.Type == ObjectHeader.HeaderType.ActorHeader)
                {
                    actorObjects.Add(new ActorObject(ref reader));
                }
                else
                {
                    componentObjects.Add(new ComponentObject(ref reader));
                }
            }
            ActorObjects = [.. actorObjects];
            ComponentObjects = [.. componentObjects];

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
        public ActorObject[] ActorObjects { get; set; }
        public ComponentObject[] ComponentObjects { get; set; }
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
                ActorHeader = new ActorHeader(ref reader);
            }
            else if (Type == HeaderType.ComponentHeader)
            {
                ComponentHeader = new ComponentHeader(ref reader);
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
        public ActorHeader? ActorHeader { get; set; }
        public ComponentHeader? ComponentHeader { get; set; }
    }

    internal class ActorHeader(ref BinaryReader reader) : SaveFile(ref reader)
    {
        public string TypePath { get; set; } = ReadString(ref reader);
        public string RootObject { get; set; } = ReadString(ref reader);
        public string InstanceName { get; set; } = ReadString(ref reader);
        public uint NeedTransform { get; set; } = reader.ReadUInt32();
        public float RotationX { get; set; } = reader.ReadSingle();
        public float RotationY { get; set; } = reader.ReadSingle();
        public float RotationZ { get; set; } = reader.ReadSingle();
        public float RotationW { get; set; } = reader.ReadSingle();
        public float PositionX { get; set; } = reader.ReadSingle();
        public float PositionY { get; set; } = reader.ReadSingle();
        public float PositionZ { get; set; } = reader.ReadSingle();
        public float ScaleX { get; set; } = reader.ReadSingle();
        public float ScaleY { get; set; } = reader.ReadSingle();
        public float ScaleZ { get; set; } = reader.ReadSingle();
        public uint WasPlacedInLevel { get; set; } = reader.ReadUInt32();
    }

    internal class ComponentHeader(ref BinaryReader reader) : SaveFile(ref reader)
    {
        public string TypePath { get; set; } = ReadString(ref reader);
        public string RootObject { get; set; } = ReadString(ref reader);
        public string InstanceName { get; set; } = ReadString(ref reader);
        public string ParentActorName { get; set; } = ReadString(ref reader);
    }

    internal class ActorObject : SaveFile
    {
        public ActorObject(ref BinaryReader reader) : base(ref reader)
        {
            SaveVersion = reader.ReadUInt32();
            Flag = reader.ReadUInt32();
            Size = reader.ReadUInt32();

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
            Properties = [.. properties];

            reader.BaseStream.Position = newPosition;
        }

        public uint SaveVersion { get; set; }
        public uint Flag { get; set; }
        public uint Size { get; set; }
        public ObjectReference ParentObjectReference { get; set; }
        public uint ComponentCount { get; set; }
        public ObjectReference[] Components { get; set; }
        public Property[] Properties { get; set; } = [];
        public byte[] TrailingBytes { get; set; } = [];
    }

    internal class ComponentObject : SaveFile
    {
        public ComponentObject(ref BinaryReader reader) : base(ref reader)
        {
            SaveVersion = reader.ReadUInt32();
            Flag = reader.ReadUInt32();
            Size = reader.ReadUInt32();

            long newPosition = reader.BaseStream.Position + Size; // current position + size of object



            reader.BaseStream.Position = newPosition;
        }

        public uint  SaveVersion { get; set; }
        public uint Flag { get; set; }
        public uint Size { get; set; }
        public Property[] Properties { get; set; } = [];
        public byte[] TrailingBytes { get; set; } = [];
    }

    internal class ObjectReference(ref BinaryReader reader) : SaveFile(ref reader)
    {
        public string LevelName { get; set; } = ReadString(ref reader);
        public string PathName { get; set; } = ReadString(ref reader);
    }
}
