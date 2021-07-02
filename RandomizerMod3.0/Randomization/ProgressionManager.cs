using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using static RandomizerMod.LogHelper;

namespace RandomizerMod.Randomization
{
    public class ProgressionManager
    {
        public int[] obtained;
        private Dictionary<string, int> grubLocations;
        private Dictionary<string, int> essenceLocations;
        private Dictionary<string, int> flameLocations;
        private bool temp;
        private bool share = true;
        private bool concealRandom;
        private int randomEssence = 0;
        private int randomFlames = 0;
        public HashSet<string> tempItems;

        public ProgressionManager(RandomizerState state, int[] progression = null, bool addSettings = true)
        {
            concealRandom = state == RandomizerState.HelperLog;

            obtained = new int[LogicManager.bitMaskMax + 1];
            if (progression != null) progression.CopyTo(obtained, 0);

            FetchEssenceLocations(state);
            FetchGrubLocations(state);
            FetchFlameLocations(state);

            if (addSettings) ApplyDifficultySettings();
            RecalculateEssence();
            RecalculateGrubs();
            RecalculateFlames();
        }

        public bool CanGet(string item)
        {
            return LogicManager.ParseProcessedLogic(item, obtained);
        }

        public void Add(string item)
        {
            item = LogicManager.RemoveDuplicateSuffix(item);
            if (!LogicManager.progressionBitMask.TryGetValue(item, out (int, int) a))
            {
                // RandomizerMod.Instance.LogWarn("Could not find progression value corresponding to: " + item);
                return;
            }
            obtained[a.Item2] |= a.Item1;
            if (temp)
            {
                tempItems.Add(item);
            }
            if (share)
            {
                Share(item);
            }

            // Take into account root essence found; this should only ever happen during helper log generation
            if (RandomizerMod.Instance.Settings.RandomizeWhisperingRoots && concealRandom)
            {
                if (LogicManager.TryGetItemDef(item, out ReqDef itemDef))
                {
                    if (itemDef.pool == "Root")
                    {
                        randomEssence += itemDef.geo;
                    }
                }
            }
            if (RandomizerMod.Instance.Settings.RandomizeBossEssence && concealRandom)
            {
                if (LogicManager.TryGetItemDef(item, out ReqDef itemDef))
                {
                    if (itemDef.pool == "Essence_Boss")
                    {
                        randomEssence += itemDef.geo;
                    }
                }
            }
            if (RandomizerMod.Instance.Settings.RandomizeGrimmkinFlames && concealRandom)
            {
                if (LogicManager.TryGetItemDef(item, out ReqDef itemDef))
                {
                    if (itemDef.pool == "Flame")
                    {
                        randomFlames += 1;
                    }
                }
            }

            RecalculateGrubs();
            RecalculateEssence();
            RecalculateFlames();
            UpdateWaypoints();
        }

        public void Add(IEnumerable<string> items)
        {
            foreach (string item in items.Select(i => LogicManager.RemoveDuplicateSuffix(i)))
            {
                if (!LogicManager.progressionBitMask.TryGetValue(item, out (int, int) a))
                {
                    RandomizerMod.Instance.LogWarn("Could not find progression value corresponding to: " + item);
                    return;
                }
                obtained[a.Item2] |= a.Item1;
                if (temp)
                {
                    tempItems.Add(item);
                }
                if (share)
                {
                    Share(item);
                }
            }
            RecalculateGrubs();
            RecalculateEssence();
            RecalculateFlames();
            UpdateWaypoints();
        }

        public void AddTemp(string item)
        {
            temp = true;
            if (tempItems == null)
            {
                tempItems = new HashSet<string>();
            }
            Add(item);
        }

        private void Share(string item)
        {
            if (ItemManager.recentProgression != null)
            {
                ItemManager.recentProgression.Add(item);
            }

            if (TransitionManager.recentProgression != null)
            {
                TransitionManager.recentProgression.Add(item);
            }
        }

        private void ToggleShare(bool value)
        {
            share = value;
        }

        public void Remove(string item)
        {
            item = LogicManager.RemoveDuplicateSuffix(item);
            if (!LogicManager.progressionBitMask.TryGetValue(item, out (int, int) a))
            {
                RandomizerMod.Instance.LogWarn("Could not find progression value corresponding to: " + item);
                return;
            }
            obtained[a.Item2] &= ~a.Item1;
            if (LogicManager.grubProgression.Contains(item)) RecalculateGrubs();
            if (LogicManager.essenceProgression.Contains(item)) RecalculateEssence();
            if (LogicManager.flameProgression.Contains(item)) RecalculateFlames();
        }

        public void RemoveTempItems()
        {
            temp = false;
            foreach (string item in tempItems)
            {
                Remove(item);
            }
            tempItems = new HashSet<string>();
        }

        public void SaveTempItems()
        {
            temp = false;
            if (share)
            {
                foreach (string item in tempItems)
                {
                    Share(item);
                }
            }
            
            tempItems = new HashSet<string>();
        }

        public bool Has(string item)
        {
            item = LogicManager.RemoveDuplicateSuffix(item);
            if (!LogicManager.progressionBitMask.TryGetValue(item, out (int, int) a))
            {
                RandomizerMod.Instance.LogWarn("Could not find progression value corresponding to: " + item);
                return false;
            }
            return (obtained[a.Item2] & a.Item1) == a.Item1;
        }

        public void UpdateWaypoints()
        {
            if (RandomizerMod.Instance.Settings.RandomizeRooms) return;

            foreach(string waypoint in LogicManager.Waypoints)
            {
                if (!Has(waypoint) && CanGet(waypoint))
                {
                    Add(waypoint);
                }
            }
        }

        private void ApplyDifficultySettings()
        {
            bool tempshare = share;
            share = false;

            if (RandomizerMod.Instance.Settings.ShadeSkips) Add("SHADESKIPS");
            if (RandomizerMod.Instance.Settings.AcidSkips) Add("ACIDSKIPS");
            if (RandomizerMod.Instance.Settings.SpikeTunnels) Add("SPIKETUNNELS");
            if (RandomizerMod.Instance.Settings.SpicySkips) Add("SPICYSKIPS");
            if (RandomizerMod.Instance.Settings.FireballSkips) Add("FIREBALLSKIPS");
            if (RandomizerMod.Instance.Settings.DarkRooms) Add("DARKROOMS");
            if (RandomizerMod.Instance.Settings.MildSkips) Add("MILDSKIPS");
            if (RandomizerMod.Instance.Settings.Cursed) Add("CURSED");
            if (!RandomizerMod.Instance.Settings.RandomizeFocus) Add("NONRANDOMFOCUS");
            if (!RandomizerMod.Instance.Settings.CursedNail) Add("NONRANDOMNAIL");
            if (!RandomizerMod.Instance.Settings.CursedMasks) Add("NONCURSEDMASKS");
            if (!RandomizerMod.Instance.Settings.CursedNotches) Add("NONCURSEDNOTCHES");
            if (!RandomizerMod.Instance.Settings.RandomizeSwim) Add("Swim");

            share = tempshare;
        }

        private Dictionary<string, int> FetchLocationsByPool(RandomizerState state, string pool)
        {
            Dictionary<string, int> locations;
            switch (state)
            {
                case RandomizerState.InProgress:
                    return new Dictionary<string, int>();
                case RandomizerState.Validating:
                    locations = ItemManager.nonShopItems.Where(kvp => LogicManager.GetItemDef(kvp.Value).pool == pool).ToDictionary(kvp => kvp.Key, kvp => 1);
                    foreach (var kvp in ItemManager.shopItems)
                    {
                        if (kvp.Value.Any(item => LogicManager.GetItemDef(item).pool == pool))
                        {
                            locations.Add(kvp.Key, kvp.Value.Count(item => LogicManager.GetItemDef(item).pool == pool));
                        }
                    }
                    return locations;
                case RandomizerState.Completed:
                case RandomizerState.HelperLog:
                    locations = RandomizerMod.Instance.Settings.ItemPlacements
                        .Where(pair => LogicManager.GetItemDef(pair.Item1).pool == pool && !LogicManager.ShopNames.Contains(pair.Item2))
                        .ToDictionary(pair => pair.Item2, kvp => 1);
                    foreach (string shop in LogicManager.ShopNames)
                    {
                        if (RandomizerMod.Instance.Settings.ItemPlacements.Any(pair => pair.Item2 == shop && LogicManager.GetItemDef(pair.Item1).pool == pool))
                        {
                            locations.Add(shop, RandomizerMod.Instance.Settings.ItemPlacements.Count(pair => pair.Item2 == shop && LogicManager.GetItemDef(pair.Item1).pool == pool));
                        }
                    }
                    return locations;
                default:
                    Log("FetchLocationsByPool: unexpected RandomizerState");
                    return new Dictionary<string, int>();
            }
        }

        private void FetchGrubLocations(RandomizerState state)
        {
            if (RandomizerMod.Instance.Settings.RandomizeGrubs)
            {
                grubLocations = FetchLocationsByPool(state, "Grub");
            }
            else
            {
                grubLocations = LogicManager.GetItemsByPool("Grub").ToDictionary(grub => grub, grub => 1);
            }
        }

        private void FetchEssenceLocationsFromPool(RandomizerState state, string pool, bool poolRandomized)
        {
            switch (state)
            {
                default:
                    foreach (string root in LogicManager.GetItemsByPool(pool))
                    {
                        essenceLocations.Add(root, LogicManager.GetItemDef(root).geo);
                    }
                    break;
                case RandomizerState.InProgress when poolRandomized:
                case RandomizerState.HelperLog when poolRandomized:
                    break;
                case RandomizerState.Validating when poolRandomized:
                    foreach (var kvp in ItemManager.nonShopItems)
                    {
                        if (LogicManager.GetItemDef(kvp.Value).pool == pool)
                        {
                            essenceLocations.Add(kvp.Key, LogicManager.GetItemDef(kvp.Value).geo);
                        }
                    }
                    foreach (var kvp in ItemManager.shopItems)
                    {
                        foreach (string item in kvp.Value)
                        {
                            if (LogicManager.GetItemDef(item).pool == pool)
                            {
                                if (!essenceLocations.ContainsKey(kvp.Key))
                                {
                                    essenceLocations.Add(kvp.Key, 0);
                                }
                                essenceLocations[kvp.Key] += LogicManager.GetItemDef(item).geo;
                            }
                        }
                    }
                    break;
                case RandomizerState.Completed when poolRandomized:
                    foreach (var pair in RandomizerMod.Instance.Settings.ItemPlacements)
                    {
                        if (LogicManager.GetItemDef(pair.Item1).pool == pool && !LogicManager.ShopNames.Contains(pair.Item2))
                        {
                            essenceLocations.Add(pair.Item2, LogicManager.GetItemDef(pair.Item1).geo);
                        }
                    }
                    foreach (string shop in LogicManager.ShopNames)
                    {
                        if (RandomizerMod.Instance.Settings.ItemPlacements.Any(pair => pair.Item2 == shop && LogicManager.GetItemDef(pair.Item1).pool == pool))
                        {
                            essenceLocations.Add(shop, 0);
                            foreach (var pair in RandomizerMod.Instance.Settings.ItemPlacements)
                            {
                                if (pair.Item2 == shop && LogicManager.GetItemDef(pair.Item1).pool == pool)
                                {
                                    essenceLocations[shop] += LogicManager.GetItemDef(pair.Item1).geo;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private void FetchEssenceLocations(RandomizerState state)
        {
            essenceLocations = new Dictionary<string, int>();
            FetchEssenceLocationsFromPool(state, "Root", RandomizerMod.Instance.Settings.RandomizeWhisperingRoots);
            FetchEssenceLocationsFromPool(state, "Essence_Boss", RandomizerMod.Instance.Settings.RandomizeBossEssence);
        }

        private void FetchFlameLocations(RandomizerState state)
        {
            if (state == RandomizerState.HelperLog || !RandomizerMod.Instance.Settings.RandomizeGrimmkinFlames)
            {
                // Flames are not relevant for logic when they're not randomized, as the player starts with 6 of them already given.
                // When in Helper Log mode, we do not want to add vanilla flame locations!
                flameLocations = new Dictionary<string, int>();
            }
            else
            {
                flameLocations = FetchLocationsByPool(state, "Flame");
            }
        }

        public void RecalculateEssence()
        {
            int essence = randomEssence;

            foreach (string location in essenceLocations.Keys)
            {
                if (CanGet(location))
                {
                    essence += essenceLocations[location];
                }
                if (essence >= Randomizer.MAX_ESSENCE_COST + LogicManager.essenceTolerance) break;
            }
            obtained[LogicManager.essenceIndex] = essence;
        }

        public void RecalculateGrubs()
        {
            int grubs = 0;

            foreach (string location in grubLocations.Keys)
            {
                if (CanGet(location))
                {
                    grubs += grubLocations[location];
                }
                if (grubs >= Randomizer.MAX_GRUB_COST + LogicManager.grubTolerance) break;
            }

            obtained[LogicManager.grubIndex] = grubs;
        }

        public void RecalculateFlames()
        {
            int flames = randomFlames;

            foreach (string location in flameLocations.Keys)
            {
                if (CanGet(location))
                {
                    flames += flameLocations[location];
                }
            }

            obtained[LogicManager.flameIndex] = flames;
        }

        public void AddGrubLocation(string location)
        {
            if (!grubLocations.ContainsKey(location))
            {
                grubLocations.Add(location, 1);
            }
            else
            {
                grubLocations[location]++;
            }
        }

        public void AddEssenceLocation(string location, int essence)
        {
            if (!essenceLocations.ContainsKey(location))
            {
                essenceLocations.Add(location, essence);
            }
            else
            {
                essenceLocations[location] += essence;
            }
        }

        public void AddFlameLocation(string location)
        {
            if (!flameLocations.ContainsKey(location))
            {
                flameLocations.Add(location, 1);
            }
            else
            {
                flameLocations[location]++;
            }
        }

        // useful for debugging
        public string ListObtainedProgression()
        {
            string progression = string.Empty;
            foreach (string item in LogicManager.ItemNames)
            {
                if (LogicManager.GetItemDef(item).progression && Has(item)) progression += item + ", ";
            }

            if (RandomizerMod.Instance.Settings.RandomizeTransitions)
            {
                foreach (string transition in LogicManager.TransitionNames())
                {
                    if (Has(transition)) progression += transition + ", ";
                }
            }
            
            return progression;
        }
        public void SpeedTest()
        {
            Stopwatch watch = new Stopwatch();
            foreach (string item in LogicManager.ItemNames)
            {
                watch.Reset();
                watch.Start();
                string result = CanGet(item).ToString();
                double elapsed = watch.Elapsed.TotalSeconds;
                Log("Parsed logic for " + item + " with result " + result + " in " + watch.Elapsed.TotalSeconds);
            }
        }
    }
}
