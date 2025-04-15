using FactoryPlanner.FileReader;
using FactoryPlanner.FileReader.Structure;
using FactoryPlanner.FileReader.Structure.Properties;
using FactoryPlanner.Models;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using static FactoryPlanner.Models.DockingStation;

namespace FactoryPlanner.Helper
{
    internal class TrainHelper
    {
        private static readonly SaveFileReader s_saveFileReader = SaveFileReader.LoadedSaveFile;
        private static readonly ILog s_log = LogManager.GetLogger(typeof(TrainHelper));

        private static readonly Dictionary<int, Train> s_trainCache = [];

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

                Train? train = GetTrain(trainIndex, 20, trainReference.Value.PathName, false);
                if (train == null || train.TimeTable == null || !train.TimeTable.Stops.Any(o => o.StationIdentifierPathName == stationIdentifierPathName)) continue;
                SetFreightWagonItems(train);

                trains.Add(train);
            }

            return trains;
        }

        public static Train? GetTrain(int trainIndex, int searchRange, string pathName, bool setFreightItems = true)
        {
            //if (s_trainCache.TryGetValue(pathName.GetHashCode(), out Train? outTrain))
            //{
            //    if (setFreightItems && !outTrain.FreightItemsSet)
            //        SetFreightWagonItems(outTrain);

            //    return outTrain;
            //}

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
                FreightItemsSet = setFreightItems,
                IsReversed = false,
                TimeTable = timeTable,
                Locomotives = locomotives,
                FreightWagons = freightWagons,
            };
            train.IsReversed = IsTrainReversed(train);

            //s_trainCache.Add(pathName.GetHashCode(), train);

            if (setFreightItems)
                SetFreightWagonItems(train);

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

                    var loadFilter = TrainStationHelper.GetFilterDescriptors(dockingRuleSetProperty, "LoadFilterDescriptors");
                    var unloadFilter = TrainStationHelper.GetFilterDescriptors(dockingRuleSetProperty, "UnloadFilterDescriptors");

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

        private static void SetFreightWagonItems(Train train)
        {
            if (train.TimeTable == null) return;

            foreach (TimeTableStop stop in train.TimeTable.Stops)
            {
                ActorObject? stationIdentifier = (ActorObject?)s_saveFileReader.GetActCompObject(stop.StationIdentifierPathName);
                if (stationIdentifier == null) continue;

                string stationName = TrainStationHelper.GetTrainStationName(stationIdentifier);
                bool isTrainReversed = IsTrainReversedAtStation(train, stop.StationIdentifierPathName);
                ActorObject actorStation = TrainStationHelper.GetStationFromIdentifier(stationIdentifier);
                TrainStation station = TrainStationHelper.GetTrainStation(actorStation, stationName, stop.StationIdentifierPathName, false);

                for (int i = 0; i < station.DockingStations.Count && i < train.FreightWagons.Count; i++)
                {
                    int freightIndex = i;
                    if (isTrainReversed)
                        freightIndex = train.FreightWagons.Count - 1 - i;

                    var dockingStation = station.DockingStations[i];
                    var freightWagon = train.FreightWagons[freightIndex];

                    SetTrainItems(dockingStation, freightWagon, stop);
                }
            }
        }

        private static void SetTrainItems(DockingStation dockingStation, FreightWagon freightWagon, TimeTableStop stop)
        {
            foreach (var item in dockingStation.OutgoingItems)
            {
                if (stop.LoadFilter.Count > 0 &&
                    !stop.LoadFilter.Any(o => o[(o.LastIndexOf('.') + 1)..] == item.ItemPathName)) continue;

                // freight wagon
                if (freightWagon.Items.Find(o => o.ItemPathName == item.ItemPathName) is Item foundItem)
                    foundItem.Rate += item.Rate;
                else
                    freightWagon.Items.Add(item);

                // i could add a neededItems property to DockingStations for exports in the future
            }
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
            PropertyListEntry? reversedProperty = SaveFileReader.GetPropertyByName(locomotive, "mIsOrientationReversed");

            return reversedProperty != null;
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
                    else if (TrainStationHelper.IsTrainStationDeadEnd(stop.StationIdentifierPathName))
                        isReversed = !isReversed;
                }

                startIndex = 0;
            }

            throw new Exception("Failed to determine if train is reversed at station");
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
    }
}
