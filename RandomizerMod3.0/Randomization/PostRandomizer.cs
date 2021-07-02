using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomizerMod.Actions;
using static RandomizerMod.LogHelper;
using static RandomizerMod.Randomization.Randomizer;

namespace RandomizerMod.Randomization
{
    public static class PostRandomizer
    {
        public static Action PostRandomizationActions;

        public static void RunPostRandomizationTasks()
        {
            PostRandomizationActions.Invoke();
        }

        // Tasks names in this functions are crucial for MW's functionality
        internal static void InitializeTasks()
        {
            PostRandomizationActions = RemovePlaceholders;
            PostRandomizationActions += SaveAllPlacements;
            PostRandomizationActions += CreateSpoilers;
            PostRandomizationActions += CreateActions; 
        }

        internal static void CreateSpoilers()
        {
            if (!RandomizerMod.Instance.Settings.CreateSpoilerLog) return;

            // Locations in the Vanilla manager where the location is a shop count as vanilla; otherwise, if the item and location
            // do not match we should log them. In particular, the split cloak pieces in the vanilla manager should be logged.
            (int, string, string)[] orderedILPairs = getOrderedILPairs();

            // TODO For MW: LogAllToSpoiler - Provide a pre-generated ItemsSpoiler section from server which emits the itemspoiler generation call
            RandoLogger.LogAllToSpoiler(orderedILPairs, RandomizerMod.Instance.Settings._transitionPlacements.Select(kvp => (kvp.Key, kvp.Value)).ToArray());

            // TODO For MW: LogItemsToCondensedSpoiler - Provide allIlPairs.Where(Either Key is player's);
            RandoLogger.LogItemsToCondensedSpoiler(orderedILPairs);
        }

        private static void CreateActions()
        {
            RandomizerAction.CreateActions(RandomizerMod.Instance.Settings.ItemPlacements, RandomizerMod.Instance.Settings);
        }

        public static (int, string, string)[] getOrderedILPairs()
        {
            return RandomizerMod.Instance.Settings.ItemPlacements
                .Except(VanillaManager.Instance.ItemPlacements.Where(pair => (pair.Item1 == pair.Item2) || LogicManager.ShopNames.Contains(pair.Item2)))
                .Select(pair => (ItemManager.locationOrder.TryGetValue(pair.Item2, out int loc) ? loc : 0, pair.Item1, pair.Item2))
                .ToArray();
        }

        private static void RemovePlaceholders()
        {
            if (RandomizerMod.Instance.Settings.DuplicateMajorItems)
            {
                // Duplicate items should not be placed very early in logic
                int minimumDepth = Math.Min(ItemManager.locationOrder.Count / 5, ItemManager.locationOrder.Count - 2 * ItemManager.duplicatedItems.Count);
                int maximumDepth = ItemManager.locationOrder.Count;
                bool ValidIndex(int i)
                {
                    string location = ItemManager.locationOrder.FirstOrDefault(kvp => kvp.Value == i).Key;
                    return !string.IsNullOrEmpty(location) && !LogicManager.ShopNames.Contains(location) && !LogicManager.GetItemDef(ItemManager.nonShopItems[location]).progression;
                }
                List<int> allowedDepths = Enumerable.Range(minimumDepth, maximumDepth).Where(i => ValidIndex(i)).ToList();
                Random rand = new Random(RandomizerMod.Instance.Settings.Seed + 29);

                foreach (string majorItem in ItemManager.duplicatedItems)
                {
                    while (allowedDepths.Any())
                    {
                        int depth = allowedDepths[rand.Next(allowedDepths.Count)];
                        string location = ItemManager.locationOrder.First(kvp => kvp.Value == depth).Key;
                        string swapItem = ItemManager.nonShopItems[location];
                        string toShop = LogicManager.ShopNames.OrderBy(shop => ItemManager.shopItems[shop].Count).First();

                        ItemManager.nonShopItems[location] = majorItem + "_(1)";
                        ItemManager.shopItems[toShop].Add(swapItem);
                        allowedDepths.Remove(depth);
                        break;
                    }
                }
            }
        }

        private static void SaveAllPlacements()
        {
            if (RandomizerMod.Instance.Settings.RandomizeTransitions)
            {
                foreach (KeyValuePair<string, string> kvp in TransitionManager.transitionPlacements)
                {
                    RandomizerMod.Instance.Settings.AddTransitionPlacement(kvp.Key, kvp.Value);
                    // For map tracking
                    //     RandoLogger.LogTransitionToTracker(kvp.Key, kvp.Value);
                }
            }

            foreach (KeyValuePair<string, List<string>> kvp in ItemManager.shopItems)
            {
                foreach (string item in kvp.Value)
                {
                    if (VanillaManager.Instance.ItemPlacements.Contains((item, kvp.Key))) continue;
                    int cost = GetRandomizedShopCost(item);
                    RandomizerMod.Instance.Settings.AddShopCost(item, cost);
                }
            }

            foreach (var (item, shop) in VanillaManager.Instance.ItemPlacements.Where(p => LogicManager.ShopNames.Contains(p.Item2)))
            {
                RandomizerMod.Instance.Settings.AddShopCost(item, LogicManager.GetItemDef(item).shopCost);
            }

            foreach ((string, string) pair in GetPlacedItemPairs())
            {
                RandomizerMod.Instance.Settings.AddItemPlacement(pair.Item1, pair.Item2);
            }

            for (int i = 0; i < startItems.Count; i++)
            {
                RandomizerMod.Instance.Settings.AddItemPlacement(startItems[i], "Equipped_(" + i + ")");
            }

            foreach (var kvp in ItemManager.locationOrder)
            {
                RandomizerMod.Instance.Settings.AddOrderedLocation(kvp.Key, kvp.Value);
            }

            RandomizerMod.Instance.Settings.StartName = StartName;
            StartDef startDef = LogicManager.GetStartLocation(StartName);
            RandomizerMod.Instance.Settings.StartSceneName = startDef.sceneName;
            RandomizerMod.Instance.Settings.StartRespawnMarkerName = StartSaveChanges.RESPAWN_MARKER_NAME;
            RandomizerMod.Instance.Settings.StartRespawnType = 0;
            RandomizerMod.Instance.Settings.StartMapZone = (int)startDef.zone;
        }

        public static int GetRandomizedShopCost(string item)
        {
            int cost;
            ReqDef def = LogicManager.GetItemDef(item);

            /*
            if (!RandomizerMod.Instance.Settings.GetRandomizeByPool(def.pool))
            {
                // This probably isn't ever called
                cost = def.cost;
            }
            else
            */
            {
                Random rand = new Random(RandomizerMod.Instance.Settings.Seed + item.GetHashCode()); // make shop item cost independent from prior randomization

                int baseCost = 100;
                int increment = 10;
                int maxCost = 500;

                int priceFactor = 1;
                if (def.geo > 0) priceFactor = 0;
                if (item.StartsWith("Soul_Totem")) priceFactor = 0;
                if (item.StartsWith("Lore_Tablet")) priceFactor = 0;
                if (item.StartsWith("Rancid") || item.StartsWith("Mask")) priceFactor = 2;
                if (item.StartsWith("Pale_Ore") || item.StartsWith("Charm_Notch")) priceFactor = 3;
                if (item == "Focus") priceFactor = 10;
                if (item.StartsWith("Godtuner") || item.StartsWith("Collector") || item.StartsWith("World_Sense")) priceFactor = 0;
                cost = baseCost + increment * rand.Next(1 + (maxCost - baseCost)/increment); // random from 100 to 500 inclusive, multiples of 10
                cost *= priceFactor;
            }

            return Math.Max(cost, 1);
        }

        public static List<(string, string)> GetPlacedItemPairs()
        {
            List<(string, string)> pairs = new List<(string, string)>();
            foreach (KeyValuePair<string, List<string>> kvp in ItemManager.shopItems)
            {
                foreach (string item in kvp.Value)
                {
                    pairs.Add((item, kvp.Key));
                }
            }
            foreach (KeyValuePair<string, string> kvp in ItemManager.nonShopItems)
            {
                pairs.Add((kvp.Value, kvp.Key));
            }

            //Vanilla Item Placements (for RandomizerActions, Hints, Logs, etc)
            foreach ((string, string) pair in vm.ItemPlacements)
            {
                pairs.Add((pair.Item1, pair.Item2));
            }

            return pairs;
        }

        public static void LogItemPlacements(ProgressionManager pm)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("All Item Placements:");
            foreach ((string, string) pair in GetPlacedItemPairs())
            {
                ReqDef def = LogicManager.GetItemDef(pair.Item1);
                if (def.progression) sb.AppendLine($"--{pm.CanGet(pair.Item2)} - {pair.Item1} -at- {pair.Item2}");
            }

            Log(sb.ToString());
        }
    }
}
