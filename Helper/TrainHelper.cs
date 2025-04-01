using Avalonia.Controls;
using Avalonia.Input.TextInput;
using FactoryPlanner.FileReader;
using FactoryPlanner.FileReader.Structure;
using FactoryPlanner.FileReader.Structure.Properties;
using FactoryPlanner.Models;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static FactoryPlanner.Models.DockingStation;

namespace FactoryPlanner.Helper
{
    internal class TrainHelper
    {
        private static readonly SaveFileReader s_saveFileReader = SaveFileReader.LoadedSaveFile;
        private static readonly ILog s_log = LogManager.GetLogger(typeof(TrainHelper));

        public static ActorObject GetRailroadSubsystem()
        {
            return (ActorObject?)s_saveFileReader.GetActCompObject("Persistent_Level:PersistentLevel.RailroadSubsystem") ?? throw new NullReferenceException("Failed to load RailroadSubSystem!");
        }

        public static List<Train> GetTrainsByStop(string stationIdentifierPathName)
        {
            ActorObject subsystem = GetRailroadSubsystem();
            PropertyListEntry trainEntry = SaveFileReader.GetPropertyByName(subsystem, "mTrains") ?? throw new NullReferenceException("Failed to get mTrains from Railroadsubsytem!");
            ArrayProperty trainProperty = (ArrayProperty?)trainEntry.Property ?? throw new NullReferenceException("Failed to get trainProperty!");

            List<Train> trains = [];
            foreach (SimpleObjectProperty trainReference in trainProperty.Properties.Cast<SimpleObjectProperty>())
            {
                int trainIndex = s_saveFileReader.GetIndexOf(trainReference.Value.PathName);
                Train? train = GetTrain(trainIndex, 20);
                if (train == null || train.TimeTable == null || train.TimeTable.Stops.Any(o => o.StationIdentifierPathName == stationIdentifierPathName) == false) continue;

                trains.Add(train);
            }

            return trains;
        }

        public static Train? GetTrain(int trainIndex, int searchRange)
        {
            int min = 0;
            int max = s_saveFileReader.Body.Levels.Last().ActCompObjects.Length;

            string layout = "";
            TimeTable? timeTable = null;
            List<ActorObject> locomotives = [];
            List<FreightWagon> freightWagons = [];
            for (int i = trainIndex - searchRange; i < trainIndex + searchRange; i++)
            {
                if (i < min || i > max) continue;

                ObjectHeader? header = s_saveFileReader.GetObjectHeader(i, SaveFileReader.Type.Index);
                if (header == null || !IsWholeTrain(header.ActCompHeader.InstanceName)) continue;

                ActorObject? trainPart = (ActorObject?)s_saveFileReader.GetActCompObject(i, SaveFileReader.Type.Index);
                if (trainPart == null) continue;

                if (IsTrain(header.ActCompHeader.InstanceName))
                {
                    timeTable = LoadTimeTable(trainPart);
                }
                else if (IsLocomotive(header.ActCompHeader.InstanceName))
                {
                    locomotives.Add(trainPart);
                    layout += $"L{locomotives.Count - 1};";
                }
                else if (IsFreightWagon(header.ActCompHeader.InstanceName))
                {
                    freightWagons.Add(new FreightWagon()
                    {
                        Wagon = trainPart,
                        Items = []
                    });
                    layout += $"F{freightWagons.Count - 1};";
                }
            }

            Train train = new()
            {
                Name = "",
                Layout = layout,
                IsReversed = false,
                TimeTable = timeTable,
                Locomotives = locomotives,
                FreightWagons = freightWagons,
            };
            train.IsReversed = IsTrainReversed(train);

            return train;
        }

        private static TimeTable? LoadTimeTable(ActorObject trainObject)
        {
            PropertyListEntry? timeTableEntry = SaveFileReader.GetPropertyByName(trainObject, "TimeTable");
            if (timeTableEntry == null) return null;

            ObjectProperty? timeTableProperty = (ObjectProperty?)timeTableEntry.Property;
            if (timeTableProperty == null) return null;

            ActorObject? timeTable = (ActorObject?)s_saveFileReader.GetActCompObject(timeTableProperty.Reference.PathName);
            if (timeTable == null) return null;

            return new TimeTable()
            {
                CurrentStopIndex = GetCurrentTimeTableStopIndex(timeTable),
                Stops = LoadTimeTableStops(timeTable)
            };
        }

        /// <summary>
        /// gets the index of the current stop of the timetable
        /// </summary>
        /// <param name="timeTable"></param>
        /// <returns>The index of the current stop if found, else -1</returns>
        private static int GetCurrentTimeTableStopIndex(ActorObject timeTable)
        {
            PropertyListEntry? stopsProperty = SaveFileReader.GetPropertyByName(timeTable, "mCurrentStop");
            if (stopsProperty == null) return 0; // probably only one stop available

            IntProperty? indexProperty = (IntProperty?)stopsProperty.Property;
            if (indexProperty == null) throw new NullReferenceException("IntProperty of mCurrentStop was null!"); // should not happen

            return indexProperty.Value;
        }

        private static List<TimeTableStop> LoadTimeTableStops(ActorObject timeTable)
        {
            List<TimeTableStop> timeTableStops = [];

            PropertyListEntry? stopsProperty = SaveFileReader.GetPropertyByName(timeTable, "mStops");
            if (stopsProperty == null) return timeTableStops;

            ArrayProperty? timeTableStopsProperty = (ArrayProperty?)stopsProperty.Property;
            if (timeTableStopsProperty == null) return timeTableStops;

            foreach (SimpleStructProperty item in timeTableStopsProperty.Properties.Cast<SimpleStructProperty>())
            {
                for (int i = 0; i < item.Data.Length; i += 2)
                {
                    PropertyListEntry stationEntry = (PropertyListEntry)item.Data[i];
                    ObjectProperty? stationProperty = (ObjectProperty?)stationEntry.Property;
                    if (stationProperty == null) continue;

                    PropertyListEntry dockingRuleSetEntry = (PropertyListEntry)item.Data[i + 1];
                    StructProperty? dockingRuleSetProperty = (StructProperty?)dockingRuleSetEntry.Property;
                    if (dockingRuleSetProperty == null) continue;

                    PropertyListEntry? dockingDefinitionProperty = (PropertyListEntry?)dockingRuleSetProperty.Properties.FirstOrDefault(o => ((PropertyListEntry)o).Name == "DockingDefinition");
                    if (dockingDefinitionProperty == null) continue;

                    EnumProperty? dockingDefinition = (EnumProperty?)dockingDefinitionProperty.Property;
                    if (dockingDefinition == null) continue;

                    var loadFilter = GetFilterDescriptors(dockingRuleSetProperty, "LoadFilterDescriptors");
                    var unloadFilter = GetFilterDescriptors(dockingRuleSetProperty, "UnloadFilterDescriptors");

                    timeTableStops.Add(new()
                    {
                        StationIdentifierPathName = stationProperty.Reference.PathName,
                        LoadType = dockingDefinition.Value,
                        LoadFilter = loadFilter,
                        UnloadFilter = unloadFilter
                    });
                }
            }

            return timeTableStops;
        }

        private static List<string> GetFilterDescriptors(StructProperty dockingRuleSetProperty, string descriptorName)
        {
            List<string> descriptors = [];

            PropertyListEntry? entry = (PropertyListEntry?)dockingRuleSetProperty.Properties.FirstOrDefault(o => ((PropertyListEntry)o).Name == descriptorName);
            if (entry == null) return descriptors;

            ArrayProperty? property = (ArrayProperty?)entry.Property;
            if (property == null) return descriptors;

            foreach (SimpleObjectProperty item in property.Properties.Cast<SimpleObjectProperty>())
            {
                descriptors.Add(item.Value.PathName);
            }

            return descriptors;
        }

        /// <summary>
        /// Determines if a train is reversed or not. A train should only be reversable if it has one locomotive at the start and one at the end
        /// </summary>
        /// <param name="train"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public static bool IsTrainReversed(Train train)
        {
            ActorObject locomotive = train.Locomotives.First();
            PropertyListEntry trackProperty = SaveFileReader.GetPropertyByName(locomotive, "mTrackPosition") ?? throw new NullReferenceException("Locomotive does not have mTrackPosition!");
            StructProperty? trackStruct = (StructProperty?)trackProperty.Property ?? throw new NullReferenceException("Failed to get StructProperty");
            RailroadTrackPosition trackPosition = (RailroadTrackPosition)trackStruct.Properties.First();

            return trackPosition.Forward == -1; // -1 reversed | +1 not reversed
        }

        /// <summary>
        /// Determines if a train is reversed at a specific station by tracking down the path from the current position to the desired station
        /// </summary>
        /// <param name="train"></param>
        /// <param name="stationIdentifierPathName"></param>
        /// <returns></returns>
        public static bool IsTrainReversedAtStation(Train train, string stationIdentifierPathName)
        {
            if (train.TimeTable == null) throw new NullReferenceException("Timetable of train is null!");

            int startIndex = train.TimeTable.CurrentStopIndex;
            bool isReversed = train.IsReversed;

            for (int j = 0; j < 2; j++)
            {
                for (int i = startIndex; i < train.TimeTable.Stops.Count; i++)
                {
                    var stop = train.TimeTable.Stops[i];

                    if (stop.StationIdentifierPathName == stationIdentifierPathName)
                        return isReversed;
                    else if (IsTrainStationDeadEnd(stop.StationIdentifierPathName))
                        isReversed = !isReversed;
                }

                startIndex = 0;
            }

            throw new Exception("Failed to determine if train is reversed at station");
        }

        public static ActorObject GetStationFromIdentifier(ActorObject stationIdentifier)
        {
            ObjectProperty stationProperty = (ObjectProperty?)stationIdentifier.Properties.First().Property ?? throw new NullReferenceException("Objectproperty mStation of StationIdentifier is null!");
            string stationPathName = stationProperty.Reference.PathName;
            ActorObject station = (ActorObject?)s_saveFileReader.GetActCompObject(stationPathName) ?? throw new NullReferenceException("Stationidentifier has no Station!");

            return station;
        }


        public static bool IsTrainStation(string pathName)
        {
            string trainStation = "Persistent_Level:PersistentLevel.Build_TrainStation";
            return pathName[..trainStation.Length] == trainStation;
        }

        public static bool IsDockingStation(string pathName)
        {
            string dockingStation = "Persistent_Level:PersistentLevel.Build_TrainDockingStation";
            return pathName[..dockingStation.Length] == dockingStation;
        }

        public static bool IsWholeTrain(string pathName)
        {
            return IsTrain(pathName) || IsLocomotive(pathName) || IsFreightWagon(pathName);
        }

        public static bool IsTrain(string pathName)
        {
            string train = "Persistent_Level:PersistentLevel.BP_Train";
            return pathName[..train.Length] == train;
        }

        public static bool IsLocomotive(string pathName)
        {
            string locomotive = "Persistent_Level:PersistentLevel.BP_Locomotive";
            return pathName[..locomotive.Length] == locomotive;
        }

        public static bool IsFreightWagon(string pathName)
        {
            string freightWagon = "Persistent_Level:PersistentLevel.BP_FreightWagon";
            return pathName[..freightWagon.Length] == freightWagon;
        }

        public static bool IsTrainStationDeadEnd(string stationIdentifierPathName) =>
            IsTrainStationDeadEnd(GetStationFromIdentifier((ActorObject?)s_saveFileReader.GetActCompObject(stationIdentifierPathName) ?? throw new Exception("Could not get Station Identifier!")));
        public static bool IsTrainStationDeadEnd(ActorObject trainStation)
        {
            PropertyListEntry track = SaveFileReader.GetPropertyByName(trainStation, "mRailroadTrack") ?? throw new NullReferenceException("Station has no integrated track!"); // if this error get's triggered i am cooked
            ObjectProperty trackProperty = (ObjectProperty?)track.Property ?? throw new NullReferenceException("Track Property is null!"); // if this error get's triggered i am cooked too
            ActorObject integratedTrack = (ActorObject?)s_saveFileReader.GetActCompObject(trackProperty.Reference.PathName) ?? throw new NullReferenceException("Failed to get integrated track!"); // if this error get's triggered i am cooked too

            bool front = TraceRailroad(integratedTrack, 0, 5);
            bool back = TraceRailroad(integratedTrack, 1, 5);

            return front == false || back == false;
        }

        public static bool TraceRailroad(ActorObject track, int direction, int depth)
        {
            if (depth == 0) return true;

            ObjectReference? connectedReference = track.Components.FirstOrDefault(o => o.PathName.Contains($"TrackConnection{direction}"));
            if (connectedReference == null) return false;

            ComponentObject? connected = (ComponentObject?)s_saveFileReader.GetActCompObject(connectedReference.PathName);
            if (connected == null) return false;

            PropertyListEntry? connectedComponents = SaveFileReader.GetPropertyByName(connected, "mConnectedComponents");
            if (connectedComponents == null) return false;

            ArrayProperty? connectedComponentsProperty = (ArrayProperty?)connectedComponents.Property;
            if (connectedComponentsProperty == null) return false;

            foreach (SimpleObjectProperty item in connectedComponentsProperty.Properties.Cast<SimpleObjectProperty>())
            {
                ActorObject? nextTrack = (ActorObject?)s_saveFileReader.GetActCompObject(item.Value.PathName[..item.Value.PathName.LastIndexOf('.')]);
                if (nextTrack == null) return false;

                return TraceRailroad(nextTrack, direction, depth - 1);
            }

            return false;
        }
    }
}
