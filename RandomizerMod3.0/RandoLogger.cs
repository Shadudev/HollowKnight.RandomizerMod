using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;
using Modding;
using RandomizerMod.Randomization;

namespace RandomizerMod
{
    public static class RandoLogger
    {
        public static ProgressionManager pm;
        public static HashSet<string> obtainedLocations;
        public static HashSet<string> uncheckedLocations;
        public static HashSet<string> randomizedLocations;
        public static HashSet<string> obtainedTransitions;
        public static HashSet<string> uncheckedTransitions;
        public static HashSet<string> randomizedTransitions;

        private static void MakeHelperLists()
        {
            {
                randomizedLocations = ItemManager.GetRandomizedLocations();
                obtainedLocations = new HashSet<string>(RandomizerMod.Instance.Settings.GetLocationsFound());
                uncheckedLocations = new HashSet<string>();
                pm = new ProgressionManager(RandomizerState.Completed, concealRandomItems: true);

                if (RandomizerMod.Instance.Settings.RandomizeRooms)
                {
                    pm.Add(LogicManager.GetStartLocation(RandomizerMod.Instance.Settings.StartName).roomTransition);
                }
                else
                {
                    pm.Add(LogicManager.GetStartLocation(RandomizerMod.Instance.Settings.StartName).waypoint);
                    if (RandomizerMod.Instance.Settings.RandomizeAreas)
                    {
                        pm.Add(LogicManager.GetStartLocation(RandomizerMod.Instance.Settings.StartName).areaTransition);
                    }
                }


                foreach (string item in RandomizerMod.Instance.Settings.GetItemsFound())
                {
                    if (LogicManager.GetItemDef(item).progression)
                    {
                        pm.Add(LogicManager.RemoveDuplicateSuffix(item));
                    }
                }

                if (RandomizerMod.Instance.Settings.RandomizeTransitions)
                {
                    obtainedTransitions = new HashSet<string>();
                    uncheckedTransitions = new HashSet<string>();
                    randomizedTransitions = new HashSet<string>(LogicManager.TransitionNames());

                    foreach (string transition in RandomizerMod.Instance.Settings.GetTransitionsFound())
                    {
                        obtainedTransitions.Add(transition);
                        pm.Add(transition);
                    }
                }
            }

            foreach (string location in randomizedLocations)
            {
                string altLocation = location; // clumsy way to be able to switch out items without spoiling their costs

                if (obtainedLocations.Contains(location)) continue;

                if (!LogicManager.ShopNames.Contains(location))
                {
                    if (LogicManager.GetItemDef(location).costType == Actions.AddYNDialogueToShiny.CostType.Essence)
                    {
                        altLocation = "Seer";
                    }
                    else if (LogicManager.GetItemDef(location).costType == Actions.AddYNDialogueToShiny.CostType.Grub)
                    {
                        altLocation = "Grubfather";
                    }
                }

                if (pm.CanGet(altLocation))
                {
                    uncheckedLocations.Add(altLocation);
                }
            }

            if (!RandomizerMod.Instance.Settings.RandomizeTransitions) return;

            foreach (string transition in randomizedTransitions)
            {
                if (obtainedTransitions.Contains(transition))
                {
                    continue;
                }
                if (pm.Has(transition))
                {
                    obtainedTransitions.Add(transition);
                }
                else if (pm.CanGet(transition))
                {
                    uncheckedTransitions.Add(transition);
                }
            }
        }

        public static void LogHelper(string message)
        {
            File.AppendAllText(Path.Combine(Application.persistentDataPath, "RandomizerHelperLog.txt"), message + Environment.NewLine);
        }

        public static void UpdateHelperLog()
        {
            new Thread(() =>
            {
                Stopwatch helperWatch = new Stopwatch();
                helperWatch.Start();

                string log = string.Empty;
                void AddToLog(string message) => log += message + Environment.NewLine;

                MakeHelperLists();

                AddToLog($"Current scene: {GameManager.instance.sceneName}");
                if (RandomizerMod.Instance.Settings.RandomizeTransitions)
                {
                    if (!string.IsNullOrEmpty(RandomizerMod.Instance.LastRandomizedEntrance) && !string.IsNullOrEmpty(RandomizerMod.Instance.LastRandomizedExit))
                    {
                        AddToLog($"Last randomized transition: {{{RandomizerMod.Instance.LastRandomizedEntrance}}}-->{{{RandomizerMod.Instance.LastRandomizedExit}}}");
                    }
                    else
                    {
                        AddToLog($"Last randomized transition: n/a");
                    }
                }

                if (!RandomizerMod.Instance.Settings.RandomizeGrubs)
                {
                    AddToLog(Environment.NewLine + "Reachable grubs: " + pm.obtained[LogicManager.grubIndex]);
                }
                if (!RandomizerMod.Instance.Settings.RandomizeWhisperingRoots)
                {
                    AddToLog("Reachable essence: " + pm.obtained[LogicManager.essenceIndex]);
                }

                // UNCHECKED ITEMS
                {
                    AddToLog(Environment.NewLine + Environment.NewLine + "REACHABLE ITEM LOCATIONS");
                    AddToLog($"There are {uncheckedLocations.Count} unchecked reachable locations.");

                    Dictionary<string, List<string>> AreaSortedItems = new Dictionary<string, List<string>>();
                    List<string> shops = LogicManager.ShopNames.Union(new List<string> { "Seer", "Grubfather" }).ToList();

                    foreach (string location in uncheckedLocations)
                    {
                        if (shops.Contains(location))
                        {
                            if (!AreaSortedItems.ContainsKey("Shops"))
                            {
                                AreaSortedItems.Add("Shops", new List<string>());
                            }
                            AreaSortedItems["Shops"].Add(location);
                            continue;
                        }

                        if (AreaSortedItems.ContainsKey(LogicManager.GetItemDef(location).areaName)) continue;

                        AreaSortedItems.Add(
                            LogicManager.GetItemDef(location).areaName,
                            uncheckedLocations.Where(loc => !shops.Contains(loc) && LogicManager.GetItemDef(loc).areaName == LogicManager.GetItemDef(location).areaName).ToList()
                            );
                    }

                    foreach (var area in AreaSortedItems)
                    {
                        AddToLog(Environment.NewLine + area.Key.Replace('_', ' '));
                        foreach (string location in area.Value)
                        {
                            AddToLog(" - " + location.Replace('_', ' '));
                        }
                    }
                }

                // UNCHECKED TRANSITIONS (AREA RANDOMIZER VERSION)
                if (RandomizerMod.Instance.Settings.RandomizeAreas)
                {
                    AddToLog(Environment.NewLine + Environment.NewLine + "REACHABLE TRANSITIONS");

                    Dictionary<string, List<string>> AreaSortedTransitions = new Dictionary<string, List<string>>();
                    foreach (string transition in uncheckedTransitions)
                    {
                        if (AreaSortedTransitions.ContainsKey(LogicManager.GetTransitionDef(transition).areaName)) continue;

                        AreaSortedTransitions.Add(
                            LogicManager.GetTransitionDef(transition).areaName,
                            uncheckedTransitions.Where(t => LogicManager.GetTransitionDef(t).areaName == LogicManager.GetTransitionDef(transition).areaName).ToList()
                            );
                    }

                    foreach (var area in AreaSortedTransitions)
                    {
                        AddToLog(Environment.NewLine + area.Key.Replace('_', ' '));
                        foreach (string transition in area.Value)
                        {
                            AddToLog(" - " + transition);
                        }
                    }
                }
                else if (RandomizerMod.Instance.Settings.RandomizeRooms)
                {
                    AddToLog(Environment.NewLine + Environment.NewLine + "REACHABLE TRANSITIONS");

                    Dictionary<string, List<string>> SceneSortedTransitions = new Dictionary<string, List<string>>();
                    foreach (string transition in uncheckedTransitions)
                    {
                        if (SceneSortedTransitions.ContainsKey(LogicManager.GetTransitionDef(transition).sceneName.Split('-').First())) continue;

                        SceneSortedTransitions.Add(
                            LogicManager.GetTransitionDef(transition).sceneName.Split('-').First(),
                            uncheckedTransitions.Where
                                (t => LogicManager.GetTransitionDef(t).sceneName.Split('-').First()
                                    == LogicManager.GetTransitionDef(transition).sceneName.Split('-').First()).ToList()
                            );
                    }

                    foreach (var room in SceneSortedTransitions)
                    {
                        AddToLog(Environment.NewLine + room.Key.Replace('_', ' '));
                        foreach (string transition in room.Value)
                        {
                            AddToLog(" - " + transition);
                        }
                    }
                }

                {
                    AddToLog(Environment.NewLine + Environment.NewLine + "CHECKED ITEM LOCATIONS");
                    Dictionary<string, List<string>> AreaSortedItems = new Dictionary<string, List<string>>();
                    List<string> shops = LogicManager.ShopNames.Union(new List<string> { "Seer", "Grubfather" }).ToList();

                    foreach (string location in obtainedLocations)
                    {
                        if (shops.Contains(location))
                        {
                            if (!AreaSortedItems.ContainsKey("Shops"))
                            {
                                AreaSortedItems.Add("Shops", new List<string>());
                            }
                            AreaSortedItems["Shops"].Add(location);
                            continue;
                        }

                        if (AreaSortedItems.ContainsKey(LogicManager.GetItemDef(location).areaName)) continue;

                        AreaSortedItems.Add(
                            LogicManager.GetItemDef(location).areaName,
                            obtainedLocations.Where(loc => !shops.Contains(loc) && LogicManager.GetItemDef(loc).areaName == LogicManager.GetItemDef(location).areaName).ToList()
                            );
                    }

                    foreach (var area in AreaSortedItems)
                    {
                        AddToLog(Environment.NewLine + area.Key.Replace('_', ' '));
                        foreach (string location in area.Value)
                        {
                            AddToLog(" - " + location.Replace('_', ' '));
                        }
                    }
                }

                helperWatch.Stop();
                File.Create(Path.Combine(Application.persistentDataPath, "RandomizerHelperLog.txt")).Dispose();
                LogHelper("Generating helper log:");
                LogHelper(log);
                LogHelper("Generated helper log in " + helperWatch.Elapsed.TotalSeconds + " seconds.");
            }).Start();
        }

        public static void LogTracker(string message)
        {
            File.AppendAllText(Path.Combine(Application.persistentDataPath, "RandomizerTrackerLog.txt"), message + Environment.NewLine);
        }
        public static void InitializeTracker()
        {
            File.Create(Path.Combine(Application.persistentDataPath, "RandomizerTrackerLog.txt")).Dispose();
            string log = "Starting tracker log for new randomizer file.";
            void AddToLog(string s) => log += "\n" + s;
            AddToLog("SETTINGS");
            AddToLog($"Seed: {RandomizerMod.Instance.Settings.Seed}");
            AddToLog($"Mode: " + // :)
                        $"{(RandomizerMod.Instance.Settings.RandomizeRooms ? (RandomizerMod.Instance.Settings.ConnectAreas ? "Connected-Area Room Randomizer" : "Room Randomizer") : (RandomizerMod.Instance.Settings.RandomizeAreas ? "Area Randomizer" : "Item Randomizer"))}");
            AddToLog($"Cursed: {RandomizerMod.Instance.Settings.Cursed}");
            AddToLog($"Start location: {RandomizerMod.Instance.Settings.StartName}");
            AddToLog($"Random start items: {RandomizerMod.Instance.Settings.RandomizeStartItems}");
            AddToLog("REQUIRED SKIPS");
            AddToLog($"Mild skips: {RandomizerMod.Instance.Settings.MildSkips}");
            AddToLog($"Shade skips: {RandomizerMod.Instance.Settings.ShadeSkips}");
            AddToLog($"Fireball skips: {RandomizerMod.Instance.Settings.FireballSkips}");
            AddToLog($"Acid skips: {RandomizerMod.Instance.Settings.AcidSkips}");
            AddToLog($"Spike tunnels: {RandomizerMod.Instance.Settings.SpikeTunnels}");
            AddToLog($"Dark Rooms: {RandomizerMod.Instance.Settings.DarkRooms}");
            AddToLog($"Spicy skips: {RandomizerMod.Instance.Settings.SpicySkips}");
            AddToLog("RANDOMIZED Pools");
            AddToLog($"Dreamers: {RandomizerMod.Instance.Settings.RandomizeDreamers}");
            AddToLog($"Skills: {RandomizerMod.Instance.Settings.RandomizeSkills}");
            AddToLog($"Charms: {RandomizerMod.Instance.Settings.RandomizeCharms}");
            AddToLog($"Keys: {RandomizerMod.Instance.Settings.RandomizeKeys}");
            AddToLog($"Geo chests: {RandomizerMod.Instance.Settings.RandomizeGeoChests}");
            AddToLog($"Mask shards: {RandomizerMod.Instance.Settings.RandomizeMaskShards}");
            AddToLog($"Vessel fragments: {RandomizerMod.Instance.Settings.RandomizeVesselFragments}");
            AddToLog($"Pale ore: {RandomizerMod.Instance.Settings.RandomizePaleOre}");
            AddToLog($"Charm notches: {RandomizerMod.Instance.Settings.RandomizeCharmNotches}");
            AddToLog($"Rancid eggs: {RandomizerMod.Instance.Settings.RandomizeRancidEggs}");
            AddToLog($"Relics: {RandomizerMod.Instance.Settings.RandomizeRelics}");
            AddToLog($"Stags: {RandomizerMod.Instance.Settings.RandomizeStags}");
            AddToLog($"Maps: {RandomizerMod.Instance.Settings.RandomizeMaps}");
            AddToLog($"Grubs: {RandomizerMod.Instance.Settings.RandomizeGrubs}");
            AddToLog($"Whispering roots: {RandomizerMod.Instance.Settings.RandomizeWhisperingRoots}");
            AddToLog($"Duplicate major items: {RandomizerMod.Instance.Settings.DuplicateMajorItems}");
            AddToLog("QUALITY OF LIFE");
            AddToLog($"Grubfather: {RandomizerMod.Instance.Settings.Grubfather}");
            AddToLog($"Salubra: {RandomizerMod.Instance.Settings.CharmNotch}");
            AddToLog($"Early geo: {RandomizerMod.Instance.Settings.EarlyGeo}");
            AddToLog($"Extra platforms: {RandomizerMod.Instance.Settings.ExtraPlatforms}");
            AddToLog($"Levers: {RandomizerMod.Instance.Settings.LeverSkips}");
            AddToLog($"Jiji: {RandomizerMod.Instance.Settings.Jiji}");
            LogTracker(log);
        }
        public static void LogTransitionToTracker(string entrance, string exit)
        {
            string message = string.Empty;
            if (RandomizerMod.Instance.Settings.RandomizeAreas)
            {
                string area1 = LogicManager.GetTransitionDef(entrance).areaName.Replace('_', ' ');
                string area2 = LogicManager.GetTransitionDef(exit).areaName.Replace('_', ' ');
                message = $"TRANSITION --- {{{entrance}}}-->{{{exit}}}" +
                    $"\n                ({area1} to {area2})";
            }
            else if (RandomizerMod.Instance.Settings.RandomizeRooms)
            {
                message = $"TRANSITION --- {{{entrance}}}-->{{{exit}}}";
            }

            LogTracker(message);
        }
        public static void LogItemToTracker(string item, string location)
        {
            // don't spoil duplicate items!
            if (LogicManager.GetItemDef(item).majorItem && RandomizerMod.Instance.Settings.DuplicateMajorItems)
            {
                item = LogicManager.RemoveDuplicateSuffix(item) + $"({new System.Random().Next(10)}?)";
            }

            string message = $"ITEM --- {{{item}}} at {{{location}}}";
            LogTracker(message);
        }

        public static void LogHintToTracker(string hint, bool jiji = true, bool quirrel = false)
        {
            if (jiji) LogTracker("HINT " + RandomizerMod.Instance.Settings.JijiHintCounter + " --- " + hint);
            else if (quirrel) LogTracker("HINT (QUIRREL) --- " + hint);
            else LogTracker("HINT --- " + hint);
        }

        public static void LogSpoiler(string message)
        {
            File.AppendAllText(Path.Combine(Application.persistentDataPath, "RandomizerSpoilerLog.txt"), message + Environment.NewLine);
        }

        public static void InitializeSpoiler()
        {
            File.Create(Path.Combine(Application.persistentDataPath, "RandomizerSpoilerLog.txt")).Dispose();
            LogSpoiler("Randomization completed with seed: " + RandomizerMod.Instance.Settings.Seed);
        }

        public static void LogTransitionToSpoiler(string entrance, string exit)
        {
            string message = "Entrance " + entrance + " linked to exit " + exit;
            LogSpoiler(message);
        }

        public static void LogItemToSpoiler(string item, string location)
        {
            string message = $"Putting item \"{item.Replace('_', ' ')}\" at \"{location.Replace('_', ' ')}\"";
            LogSpoiler(message);
        }
        public static void LogAllToSpoiler((int, string, string)[] orderedILPairs, (string, string)[] transitionPlacements)
        {
            RandomizerMod.Instance.Log("Generating spoiler log...");
            new Thread(() =>
            {
                Stopwatch spoilerWatch = new Stopwatch();
                spoilerWatch.Start();

                string log = string.Empty;
                void AddToLog(string message) => log += message + Environment.NewLine;

                AddToLog(GetItemSpoiler(orderedILPairs));
                AddToLog(GetTransitionSpoiler(transitionPlacements));

                try
                {
                    AddToLog(Environment.NewLine + "SETTINGS");
                    AddToLog($"Seed: {RandomizerMod.Instance.Settings.Seed}");
                    AddToLog($"Mode: " + // :)
                        $"{(RandomizerMod.Instance.Settings.RandomizeRooms ? (RandomizerMod.Instance.Settings.ConnectAreas ? "Connected-Area Room Randomizer" : "Room Randomizer") : (RandomizerMod.Instance.Settings.RandomizeAreas ? "Area Randomizer" : "Item Randomizer"))}");
                    AddToLog($"Cursed: {RandomizerMod.Instance.Settings.Cursed}");
                    AddToLog($"Start location: {RandomizerMod.Instance.Settings.StartName}");
                    AddToLog($"Random start items: {RandomizerMod.Instance.Settings.RandomizeStartItems}");
                    AddToLog("REQUIRED SKIPS");
                    AddToLog($"Mild skips: {RandomizerMod.Instance.Settings.MildSkips}");
                    AddToLog($"Shade skips: {RandomizerMod.Instance.Settings.ShadeSkips}");
                    AddToLog($"Fireball skips: {RandomizerMod.Instance.Settings.FireballSkips}");
                    AddToLog($"Acid skips: {RandomizerMod.Instance.Settings.AcidSkips}");
                    AddToLog($"Spike tunnels: {RandomizerMod.Instance.Settings.SpikeTunnels}");
                    AddToLog($"Dark Rooms: {RandomizerMod.Instance.Settings.DarkRooms}");
                    AddToLog($"Spicy skips: {RandomizerMod.Instance.Settings.SpicySkips}");
                    AddToLog("RANDOMIZED LOCATIONS");
                    AddToLog($"Dreamers: {RandomizerMod.Instance.Settings.RandomizeDreamers}");
                    AddToLog($"Skills: {RandomizerMod.Instance.Settings.RandomizeSkills}");
                    AddToLog($"Charms: {RandomizerMod.Instance.Settings.RandomizeCharms}");
                    AddToLog($"Keys: {RandomizerMod.Instance.Settings.RandomizeKeys}");
                    AddToLog($"Geo chests: {RandomizerMod.Instance.Settings.RandomizeGeoChests}");
                    AddToLog($"Mask shards: {RandomizerMod.Instance.Settings.RandomizeMaskShards}");
                    AddToLog($"Vessel fragments: {RandomizerMod.Instance.Settings.RandomizeVesselFragments}");
                    AddToLog($"Pale ore: {RandomizerMod.Instance.Settings.RandomizePaleOre}");
                    AddToLog($"Charm notches: {RandomizerMod.Instance.Settings.RandomizeCharmNotches}");
                    AddToLog($"Rancid eggs: {RandomizerMod.Instance.Settings.RandomizeRancidEggs}");
                    AddToLog($"Relics: {RandomizerMod.Instance.Settings.RandomizeRelics}");
                    AddToLog($"Stags: {RandomizerMod.Instance.Settings.RandomizeStags}");
                    AddToLog($"Maps: {RandomizerMod.Instance.Settings.RandomizeMaps}");
                    AddToLog($"Grubs: {RandomizerMod.Instance.Settings.RandomizeGrubs}");
                    AddToLog($"Whispering roots: {RandomizerMod.Instance.Settings.RandomizeWhisperingRoots}");
                    AddToLog($"Duplicate major items: {RandomizerMod.Instance.Settings.DuplicateMajorItems}");
                    AddToLog("QUALITY OF LIFE");
                    AddToLog($"Grubfather: {RandomizerMod.Instance.Settings.Grubfather}");
                    AddToLog($"Salubra: {RandomizerMod.Instance.Settings.CharmNotch}");
                    AddToLog($"Early geo: {RandomizerMod.Instance.Settings.EarlyGeo}");
                    AddToLog($"Extra platforms: {RandomizerMod.Instance.Settings.ExtraPlatforms}");
                    AddToLog($"Levers: {RandomizerMod.Instance.Settings.LeverSkips}");
                    AddToLog($"Jiji: {RandomizerMod.Instance.Settings.Jiji}");
                }
                catch
                {
                    AddToLog("Error logging randomizer settings!?!?");
                }

                spoilerWatch.Stop();
                LogSpoiler(log);
                LogSpoiler("Generated spoiler log in " + spoilerWatch.Elapsed.TotalSeconds + " seconds.");
            }).Start();
        }

        private static string GetTransitionSpoiler((string, string)[] transitionPlacements)
        {
            string log = string.Empty;
            void AddToLog(string message) => log += message + Environment.NewLine;

            try
            {
                if (RandomizerMod.Instance.Settings.RandomizeAreas)
                {
                    Dictionary<string, List<string>> areaTransitions = new Dictionary<string, List<string>>();
                    foreach (string transition in LogicManager.TransitionNames())
                    {
                        string area = LogicManager.GetTransitionDef(transition).areaName;
                        if (!areaTransitions.ContainsKey(area))
                        {
                            areaTransitions[area] = new List<string>();
                        }
                    }

                    foreach ((string, string) pair in transitionPlacements)
                    {
                        string area = LogicManager.GetTransitionDef(pair.Item1).areaName;
                        areaTransitions[area].Add(pair.Item1 + " --> " + pair.Item2);
                    }

                    AddToLog(Environment.NewLine + "TRANSITIONS");
                    foreach (KeyValuePair<string, List<string>> kvp in areaTransitions)
                    {
                        if (kvp.Value.Count > 0)
                        {
                            AddToLog(Environment.NewLine + kvp.Key.Replace('_', ' ') + ":");
                            foreach (string transition in kvp.Value) AddToLog(transition);
                        }
                    }
                }

                if (RandomizerMod.Instance.Settings.RandomizeRooms)
                {
                    Dictionary<string, List<string>> roomTransitions = new Dictionary<string, List<string>>();
                    foreach (string transition in LogicManager.TransitionNames())
                    {
                        string room = LogicManager.GetTransitionDef(transition).sceneName;
                        if (!roomTransitions.ContainsKey(room))
                        {
                            roomTransitions[room] = new List<string>();
                        }
                    }

                    foreach ((string, string) pair in transitionPlacements)
                    {
                        string room = LogicManager.GetTransitionDef(pair.Item1).sceneName;
                        roomTransitions[room].Add(pair.Item1 + " --> " + pair.Item2);
                    }

                    AddToLog(Environment.NewLine + "TRANSITIONS");
                    foreach (KeyValuePair<string, List<string>> kvp in roomTransitions)
                    {
                        if (kvp.Value.Count > 0)
                        {
                            AddToLog(Environment.NewLine + kvp.Key.Replace('_', ' ') + ":");
                            foreach (string transition in kvp.Value) AddToLog(transition);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                RandomizerMod.Instance.LogError("Error while creating transition spoiler log: " + e);
            }
            return log;
        }

        private static string GetItemSpoiler((int, string, string)[] orderedILPairs)
        {
            string log = string.Empty;
            void AddToLog(string message) => log += message + Environment.NewLine;
            try
            {
                orderedILPairs = orderedILPairs.OrderBy(triplet => triplet.Item1).ToArray();

                Dictionary<string, List<string>> areaItemLocations = new Dictionary<string, List<string>>();
                foreach (var triplet in orderedILPairs)
                {
                    string location = triplet.Item3;
                    if (LogicManager.TryGetItemDef(location, out ReqDef locationDef))
                    {
                        string area = locationDef.areaName;
                        if (!areaItemLocations.ContainsKey(area))
                        {
                            areaItemLocations[area] = new List<string>();
                        }
                    }
                    else if (!areaItemLocations.ContainsKey(location))
                    {
                        areaItemLocations[location] = new List<string>();
                    }
                }

                List<string> progression = new List<string>();
                foreach ((int, string, string) pair in orderedILPairs)
                {
                    string cost = "";
                    if (LogicManager.TryGetItemDef(pair.Item3, out ReqDef itemDef)) {
                        if (itemDef.cost != 0) cost = $" [{itemDef.cost} {itemDef.costType.ToString("g")}]";
                    }
                    else cost = $" [{RandomizerMod.Instance.Settings.GetShopCost(pair.Item2)} Geo]";

                    if (LogicManager.GetItemDef(pair.Item2).progression) progression.Add($"({pair.Item1}) {pair.Item2}<---at--->{pair.Item3}{cost}");
                    if (LogicManager.TryGetItemDef(pair.Item3, out ReqDef locationDef))
                    {
                        areaItemLocations[locationDef.areaName].Add($"({pair.Item1}) {pair.Item2}<---at--->{pair.Item3}{cost}");
                    }
                    else areaItemLocations[pair.Item3].Add($"{pair.Item2}{cost}");
                }

                AddToLog(Environment.NewLine + "PROGRESSION ITEMS");
                foreach (string item in progression) AddToLog(item.Replace('_', ' '));

                AddToLog(Environment.NewLine + "ALL ITEMS");
                foreach (KeyValuePair<string, List<string>> kvp in areaItemLocations)
                {
                    if (kvp.Value.Count > 0)
                    {
                        string title = kvp.Key;
                        if (LogicManager.ShopNames.Contains(title)) title = $"({orderedILPairs.First(triplet => triplet.Item3 == title).Item1}) {title}";
                        title = CleanAreaName(title);
                        AddToLog(Environment.NewLine + title + ":");
                        foreach (string item in kvp.Value) AddToLog(item.Replace('_', ' '));
                    }
                }
            }
            catch (Exception e)
            {
                RandomizerMod.Instance.LogError("Error while creating item spoiler log: " + e);
            }
            return log;
        }

        public static string CleanAreaName(string name)
        {
            string newName = name.Replace('_', ' ');
            switch (newName)
            {
                case "Kings Pass":
                    newName = "King's Pass";
                    break;
                case "Queens Station":
                    newName = "Queen's Station";
                    break;
                case "Kings Station":
                    newName = "King's Station";
                    break;
                case "Queens Gardens":
                    newName = "Queen's Gardens";
                    break;
                case "Hallownests Crown":
                    newName = "Hallownest's Crown";
                    break;
                case "Kingdoms Edge":
                    newName = "Kingdom's Edge";
                    break;
                case "Weavers Den":
                    newName = "Weaver's Den";
                    break;
                case "Beasts Den":
                    newName = "Beast's Den";
                    break;
                case "Spirits Glade":
                    newName = "Spirit's Glade";
                    break;
                case "Ismas Grove":
                    newName = "Isma's Grove";
                    break;
                case "Teachers Archives":
                    newName = "Teacher's Archives";
                    break;
            }
            return newName;
        }
    }
}
