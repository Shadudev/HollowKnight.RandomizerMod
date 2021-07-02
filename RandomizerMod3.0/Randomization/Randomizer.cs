using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using static RandomizerMod.LogHelper;
using static RandomizerMod.Randomization.PreRandomizer;
using static RandomizerMod.Randomization.PostRandomizer;
using static RandomizerMod.Randomization.SpanningTree;
using RandomizerMod.Actions;

namespace RandomizerMod.Randomization
{
    public enum RandomizerState
    {
        None,
        InProgress,
        Validating,
        Completed,
        HelperLog
    }

    internal static class Randomizer
    {
        public const int MAX_GRUB_COST = 23;
        public static int MAX_ESSENCE_COST => RandomizerMod.Instance.Settings.RandomizeBossEssence ? 1800 : 900;

        public static ItemManager im;
        public static TransitionManager tm;
        public static VanillaManager vm { get { return VanillaManager.Instance; } }

        private static bool overflow;
        public static bool randomizationError;
        public static Random rand = null;

        public static List<string> startProgression;
        public static List<string> startItems;

        public static string StartName;
        public static StartDef startDef => LogicManager.GetStartLocation(StartName);        
        public static string startTransition => RandomizerMod.Instance.Settings.RandomizeRooms ? startDef.roomTransition : startDef.areaTransition;

        

        public static void Randomize()
        {
            rand = new Random(RandomizerMod.Instance.Settings.Seed);

            while (true)
            {
                randomizationError = false;
                overflow = false;
                startProgression = null;
                startItems = null;
                StartName = null;

                RandomizerMod.Instance.Settings.ResetPlacements();
                RandomizeNonShopCosts();
                RandomizeStartingItems();
                RandomizeStartingLocation();
                if (RandomizerMod.Instance.Settings.RandomizeTransitions) RandomizeTransitions();
                RandomizeItems();
                if (!randomizationError) break;
            }

            RunPostRandomizationTasks();
        }

        private static void RandomizeTransitions()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();


            while (true)
            {
                Log("\n" +
                    "Beginning transition randomization...");
                SetupTransitionVariables();

                PlaceOneWayTransitions();
                if (RandomizerMod.Instance.Settings.RandomizeAreas) BuildAreaSpanningTree();
                else if (RandomizerMod.Instance.Settings.RandomizeRooms && RandomizerMod.Instance.Settings.ConnectAreas) BuildCARSpanningTree();
                else if (RandomizerMod.Instance.Settings.RandomizeRooms && !RandomizerMod.Instance.Settings.ConnectAreas) BuildRoomSpanningTree();
                else
                {
                    LogError("Ambiguous settings passed to transition randomizer.");
                    throw new NotSupportedException();
                }

                PlaceIsolatedTransitions();
                ConnectStartToGraph();
                CompleteTransitionGraph();

                if (ValidateTransitionRandomization()) break;
                if (randomizationError)
                {
                    LogWarn("Error encountered while randomizing transitions, attempting again...");
                }
            }
            watch.Stop();
            RandomizerMod.Instance.Log("Transition randomization finished in " + watch.Elapsed.TotalSeconds + " seconds.");
        }
        private static void RandomizeItems()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            Log("");
            Log("Beginning item randomization...");
            SetupItemVariables();

            FirstPass();
            SecondPass();
            if (!ValidateItemRandomization())
            {
                randomizationError = true;
                return;
            }

            RandomizerMod.Instance.Log("Item randomization finished in " + watch.Elapsed.TotalSeconds + " seconds.");
        }

        private static void SetupTransitionVariables()
        {
            randomizationError = false;

            tm = new TransitionManager(rand);
            im = new ItemManager(rand);
            // add start progression in ConnectStartToGraph
        }
        private static void SetupItemVariables()
        {
            im = new ItemManager(rand);
            im.pm.Add(startProgression);

            overflow = false;
        }

        private static void PlaceOneWayTransitions()
        {
            if (randomizationError) return;
            List<string> oneWayEntrances = LogicManager.TransitionNames().Where(transition => LogicManager.GetTransitionDef(transition).oneWay == 1).ToList();
            List<string> oneWayExits = LogicManager.TransitionNames().Where(transition => LogicManager.GetTransitionDef(transition).oneWay == 2).ToList();
            List<string> horizontalOneWays = oneWayEntrances.Where(t => !LogicManager.GetTransitionDef(t).doorName.StartsWith("b")).ToList();

            while (horizontalOneWays.Any())
            {
                string horizontalEntrance = horizontalOneWays.First();
                string downExit = oneWayExits[rand.Next(oneWayExits.Count)];

                tm.PlaceOneWayPair(horizontalEntrance, downExit);
                oneWayEntrances.Remove(horizontalEntrance);
                horizontalOneWays.Remove(horizontalEntrance);
                oneWayExits.Remove(downExit);
            }

            DirectedTransitions directed = new DirectedTransitions(rand);
            directed.Add(oneWayExits);
            while (oneWayEntrances.Any())
            {
                string entrance = oneWayEntrances[rand.Next(oneWayEntrances.Count)];
                string exit = directed.GetNextTransition(entrance);

                tm.PlaceOneWayPair(entrance, exit);
                oneWayEntrances.Remove(entrance);
                oneWayExits.Remove(exit);
                directed.Remove(exit);
            }
        }

        private static void PlaceIsolatedTransitions()
        {
            if (randomizationError) return;

            List<string> isolatedTransitions = tm.unplacedTransitions.Where(transition => LogicManager.GetTransitionDef(transition).isolated).ToList();
            List<string> nonisolatedTransitions = tm.unplacedTransitions.Where(transition => !LogicManager.GetTransitionDef(transition).isolated).ToList();
            DirectedTransitions directed = new DirectedTransitions(rand);
            isolatedTransitions.Remove(startTransition);
            nonisolatedTransitions.Remove(startTransition);
            directed.Add(nonisolatedTransitions);

            bool connectAreas = RandomizerMod.Instance.Settings.ConnectAreas;
            while (isolatedTransitions.Any())
            {
                string transition1 = isolatedTransitions[rand.Next(isolatedTransitions.Count)];
                string transition2 = directed.GetNextTransition(transition1, favorSameArea: connectAreas);
                if (transition2 is null)
                {
                    Log("Ran out of nonisolated transitions during preplacement!");
                    randomizationError = true;
                    return;
                }
                tm.PlaceStandbyPair(transition1, transition2);
                isolatedTransitions.Remove(transition1);
                directed.Remove(transition2);
            }
        }

        private static void ConnectStartToGraph()
        {
            if (randomizationError) return;
            Log("Attaching start to graph...");

            tm.pm = new ProgressionManager(
                RandomizerState.InProgress
                );
            im.ResetReachableLocations();
            vm.ResetReachableLocations();
            tm.ResetReachableTransitions();

            tm.pm.Add(startProgression);

            {   // keeping local variables out of the way
                DirectedTransitions d = new DirectedTransitions(rand);
                d.Add(startTransition);
                string transition2 = tm.ForceTransition(d);
                if (transition2 is null) // this should happen extremely rarely, but it has to be handled
                {
                    Log("No way out of start?!?");
                    Log("Was the start transition already placed? " + TransitionManager.transitionPlacements.ContainsKey(startTransition));
                    randomizationError = true;
                    return;
                }
                tm.PlaceTransitionPair(startTransition, transition2);
            }

            while (true)
            {
                if (!RandomizerMod.Instance.Settings.RandomizeSkills)
                {
                    // it is essentially impossible to generate a transition randomizer without one of these accessible
                    if (tm.pm.CanGet("Mantis_Claw") || tm.pm.CanGet("Mothwing_Cloak") || tm.pm.CanGet("Shade_Cloak")) 
                    {
                        return;
                    }
                        
                }
                else if (im.FindNextLocation(tm.pm) != null) return;

                tm.UnloadReachableStandby();
                List<string> placeableTransitions = tm.reachableTransitions.Intersect(tm.unplacedTransitions.Union(tm.standbyTransitions.Keys)).ToList();
                if (!placeableTransitions.Any())
                {
                    Log("Could not connect start to map--ran out of placeable transitions.");
                    foreach (string t in tm.reachableTransitions) Log(t);
                    randomizationError = true;
                    return;
                }

                DirectedTransitions directed = new DirectedTransitions(rand);
                directed.Add(placeableTransitions);

                if (tm.ForceTransition(directed) is string transition1)
                {
                    string transition2 = directed.GetNextTransition(transition1);
                    tm.PlaceTransitionPair(transition1, transition2);
                }
                else
                {
                    Log("Could not connect start to map--ran out of progression transitions.");
                    randomizationError = true;
                    return;
                }
            }
        }

        private static void CompleteTransitionGraph()
        {
            if (randomizationError) return;
            
            // IDK if this is the right fix but the line is correct and the algo is being rewritten so w/e
            foreach (string item in startProgression) im.UpdateReachableLocations(item);

            int failsafe = 0;
            Log("Beginning full placement of transitions...");

            while (tm.unplacedTransitions.Any())
            {
                failsafe++;
                if (failsafe > 120)
                {
                    Log("Aborted randomization on too many passes. At the time, there were:");
                    Log("Unplaced transitions: " + tm.unplacedTransitions.Count);
                    Log("Reachable transitions: " + tm.reachableTransitions.Count);
                    Log("Reachable unplaced transitions, directionally compatible: " + tm.placeableCount);
                    Log("Reachable item locations: " + im.availableCount);
                    foreach (string t in tm.unplacedTransitions) Log(t + ", in reachable: " + tm.reachableTransitions.Contains(t) + ", is reachable: " + tm.pm.CanGet(t));
                    randomizationError = true;
                    return;
                }

                if (im.canGuess && im.availableCount > 1) // give randomized progression as locations are available
                {
                    if (im.FindNextLocation(tm.pm) is string placeLocation)
                    {
                        string placeItem = im.GuessItem();
                        im.PlaceItem(placeItem, placeLocation);
                        tm.UpdateReachableTransitions(placeItem, true);
                    }
                }

                int placeableCount = tm.placeableCount;
                if (placeableCount < 4) tm.UpdateReachableTransitions();
                if (placeableCount == 0 && im.availableCount == 0)
                {
                    Log("Ran out of locations?!?");
                    randomizationError = true;
                    return;
                }
                else if (placeableCount > 2)
                {
                    tm.UnloadReachableStandby();
                    string transition1 = tm.NextTransition();
                    string transition2 = tm.dt.GetNextTransition(transition1);
                    tm.PlaceTransitionPair(transition1, transition2);
                    //Log($">2 place: {transition1}, {transition2}");
                    continue;
                }
                else if (tm.unplacedTransitions.Count == 2)
                {
                    string transition1 = tm.unplacedTransitions[0];
                    string transition2 = tm.unplacedTransitions[1];
                    tm.PlaceTransitionPair(transition1, transition2);
                    //Log($"last place: {transition1}, {transition2}");
                    continue;
                }
                else if (placeableCount != 0)
                {
                    if (tm.ForceTransition() is string transition1)
                    {
                        string transition2 = tm.dt.GetNextTransition(transition1);
                        tm.PlaceTransitionPair(transition1, transition2);
                        //Log($"force place: {transition1}, {transition2}");
                        continue;
                    }
                }
                // Last ditch effort to save the seed. The list is ordered by which items are heuristically likely to unlock transitions at this point.
                if (im.FindNextLocation(tm.pm) is string lastLocation)
                {
                    IEnumerable<string> progressionCandidates = new List<string> {
                        "Mantis_Claw", "Monarch_Wings", "Left_Mantis_Claw", "Right_Mantis_Claw",
                        "Desolate_Dive", "Isma's_Tear", "Crystal_Heart",
                        "Mothwing_Cloak", "Shade_Cloak", "Right_Mothwing_Cloak", "Right_Shade_Cloak", "Left_Mothwing_Cloak", "Left_Shade_Cloak" }
                        .Where(item => im.randomizedItems.Contains(item));
                    
                    foreach (string item in progressionCandidates)
                    {
                        if (!tm.pm.Has(item))
                        {
                            im.PlaceItem(item, lastLocation);
                            tm.UpdateReachableTransitions(item, true);
                            break;
                        }
                    }
                    continue;
                }
            }
            Log("Placing last reserved transitions...");
            tm.UnloadStandby();
            Log("All transitions placed? " + (TransitionManager.transitionPlacements.Count == LogicManager.TransitionNames().Count(t => LogicManager.GetTransitionDef(t).oneWay != 2)));
        }

        private static void FirstPass()
        {
            Log("Beginning first pass of item placement...");

            {
                im.ResetReachableLocations();
                vm.ResetReachableLocations();

                foreach (string item in startProgression) im.UpdateReachableLocations(item);
                Log("Finished first update");
            }

            while (true)
            {
                string placeItem;
                string placeLocation;

                switch (im.availableCount)
                {
                    case 0:
                        if (im.anyLocations)
                        {
                            if (im.canGuess)
                            {
                                if (!overflow) Log("Entered overflow state with 0 reachable locations after placing " + ItemManager.nonShopItems.Count + " locations");
                                overflow = true;
                                placeItem = im.GuessItem();
                                im.PlaceProgressionToStandby(placeItem);
                                continue;
                            }
                        }
                        return;
                    case 1:
                        placeItem = im.ForceItem();
                        if (placeItem is null)
                        {
                            if (im.canGuess)
                            {
                                if (!overflow) Log("Entered overflow state with 1 reachable location after placing " + ItemManager.nonShopItems.Count + " locations");
                                overflow = true;
                                placeItem = im.GuessItem();
                                im.PlaceProgressionToStandby(placeItem);
                                continue;
                            }
                            else placeItem = im.NextItem();
                        }
                        else
                        {
                            im.Delinearize(rand);
                        }
                        placeLocation = im.NextLocation();
                        break;
                    default:
                        placeItem = im.NextItem();
                        placeLocation = im.NextLocation();
                        break;
                }

                //Log($"i: {placeItem}, l: {placeLocation}, o: {overflow}, p: {LogicManager.GetItemDef(placeItem).progression}");

                if (!overflow && !LogicManager.GetItemDef(placeItem).progression)
                {
                    im.PlaceJunkItemToStandby(placeItem, placeLocation);
                }
                else
                {
                    im.PlaceItem(placeItem, placeLocation);
                }
            }
        }

        private static void SecondPass()
        {
            Log("Beginning second pass of item placement...");
            im.TransferStandby();

            // We fill the remaining locations and shops with the leftover junk
            while (im.anyItems)
            {
                string placeItem = im.NextItem(checkFlag: false);
                string placeLocation;

                if (im.anyLocations) placeLocation = im.NextLocation(checkLogic: false);
                else placeLocation = LogicManager.ShopNames[rand.Next(5)];

                im.PlaceItemFromStandby(placeItem, placeLocation);
            }

            // try to guarantee no empty shops
            if (im.normalFillShops && ItemManager.shopItems.Any(kvp => !kvp.Value.Any()))
            {
                Log("Exited randomizer with empty shop. Attempting repair...");
                Dictionary<string, List<string>> nonprogressionShopItems = ItemManager.shopItems.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Where(i => !LogicManager.GetItemDef(i).progression).ToList());
                if (nonprogressionShopItems.Select(kvp => kvp.Value.Count).Aggregate(0, (total,next) => total + next) >= 5)
                {
                    int i = 0;
                    while(ItemManager.shopItems.FirstOrDefault(kvp => !kvp.Value.Any()).Key is string emptyShop && nonprogressionShopItems.FirstOrDefault(kvp => kvp.Value.Count > 1).Key is string fullShop)
                    {
                        string item = ItemManager.shopItems[fullShop].First();
                        ItemManager.shopItems[emptyShop].Add(item);
                        ItemManager.shopItems[fullShop].Remove(item);
                        nonprogressionShopItems[emptyShop].Add(item);
                        nonprogressionShopItems[fullShop].Remove(item);
                        i++;
                        if (i > 5)
                        {
                            LogError("Emergency exit from shop repair.");
                            break;
                        }
                    }
                }
                Log("Successfully repaired shops.");
            }

            if (im.anyLocations) LogError("Exited item randomizer with unfilled locations.");
        }

        private static bool ValidateTransitionRandomization()
        {
            if (randomizationError) return false;
            Log("Beginning transition placement validation...");

            ProgressionManager pm = new ProgressionManager(
                RandomizerState.Validating
                );
            pm.Add(startProgression);
            pm.Add(LogicManager.ItemNames.Where(i => LogicManager.GetItemDef(i).progression));

            tm.ResetReachableTransitions();
            tm.UpdateReachableTransitions(_pm: pm);
            
            bool validated = tm.reachableTransitions.SetEquals(LogicManager.TransitionNames());

            if (!validated)
            {
                Log("Transition placements failed to validate!");
                foreach (string t in LogicManager.TransitionNames().Except(tm.reachableTransitions))
                {
                    Log($"{t} --> {(TransitionManager.transitionPlacements.TryGetValue(t, out string target) ? target : "???")}");
                }
            }
            else Log("Validation successful.");
            return validated;
        }

        private static bool ValidateItemRandomization()
        {
            RandomizerMod.Instance.Log("Beginning item placement validation...");

            List<string> unfilledLocations;
            if (im.normalFillShops) unfilledLocations = im.randomizedLocations.Except(ItemManager.nonShopItems.Keys).Except(ItemManager.shopItems.Keys).ToList();
            else unfilledLocations = im.randomizedLocations.Except(ItemManager.nonShopItems.Keys).Except(LogicManager.ShopNames).ToList();
            
            if (unfilledLocations.Any())
            {
                Log("Unable to validate!");
                string m = "The following locations were not filled: ";
                foreach (string l in unfilledLocations) m += l + ", ";
                Log(m);
                return false;
            }

            HashSet<(string,string)> LIpairs = new HashSet<(string,string)>(ItemManager.nonShopItems.Select(kvp => (kvp.Key, kvp.Value)));
            foreach (var kvp in ItemManager.shopItems)
            {
                LIpairs.UnionWith(kvp.Value.Select(i => (kvp.Key, i)));
            }

            var lookup = LIpairs.ToLookup(pair => pair.Item2, pair => pair.Item1).Where(x => x.Count() > 1);
            if (lookup.Any())
            {
                Log("Unable to validate!");
                string m = "The following items were placed multiple times: ";
                foreach (var x in lookup) m += x.Key + ", ";
                Log(m);
                string l = "The following locations were filled by these items: ";
                foreach (var x in lookup) foreach (string k in x) l += k + ", ";
                Log(l);
                return false;
            }

            /*
            // Potentially useful debug logs
            foreach (string item in ItemManager.GetRandomizedItems())
            {
                if (ItemManager.nonShopItems.Any(kvp => kvp.Value == item))
                {
                    Log($"Placed {item} at {ItemManager.nonShopItems.First(kvp => kvp.Value == item).Key}");
                }
                else if (ItemManager.shopItems.Any(kvp => kvp.Value.Contains(item)))
                {
                    Log($"Placed {item} at {ItemManager.shopItems.First(kvp => kvp.Value.Contains(item)).Key}");
                }
                else LogError($"Unable to find where {item} was placed.");
            }
            foreach (string location in ItemManager.GetRandomizedLocations())
            {
                if (ItemManager.nonShopItems.TryGetValue(location, out string item))
                {
                    Log($"Filled {location} with {item}");
                }
                else if (ItemManager.shopItems.ContainsKey(location))
                {
                    Log($"Filled {location}");
                }
                else LogError($"{location} was not filled.");
            }
            */

            ProgressionManager pm = new ProgressionManager(
                RandomizerState.Validating
                );
            pm.Add(startProgression);

            HashSet<string> locations = new HashSet<string>(im.randomizedLocations.Union(vm.progressionLocations));
            HashSet<string> transitions = new HashSet<string>();
            HashSet<string> items = im.randomizedItems;
            items.ExceptWith(startItems);

            if (RandomizerMod.Instance.Settings.RandomizeTransitions)
            {
                transitions.UnionWith(LogicManager.TransitionNames());
                tm.ResetReachableTransitions();
                tm.UpdateReachableTransitions(_pm: pm);
            }

            vm.ResetReachableLocations(false, pm);

            int passes = 0;
            while (locations.Any() || items.Any() || transitions.Any())
            {
                if (RandomizerMod.Instance.Settings.RandomizeTransitions) transitions.ExceptWith(tm.reachableTransitions);

                foreach (string location in locations.Where(loc => pm.CanGet(loc)).ToList())
                {
                    locations.Remove(location);

                    if (vm.progressionLocations.Contains(location))
                    {
                        vm.UpdateVanillaLocations(location, false, pm);
                        if (RandomizerMod.Instance.Settings.RandomizeTransitions && !LogicManager.ShopNames.Contains(location)) tm.UpdateReachableTransitions(location, true, pm);
                        else if (RandomizerMod.Instance.Settings.RandomizeTransitions)
                        {
                            foreach (string i in vm.progressionShopItems[location])
                            {
                                tm.UpdateReachableTransitions(i, true, pm);
                            }
                        }
                        // It is possible for a shop to be both a vanilla progression location and contain randomized items, if
                        // Charms or Keys are unrandomized
                        if (ItemManager.shopItems.TryGetValue(location, out List<string> shopItems))
                        {
                            foreach (string newItem in shopItems)
                            {
                                items.Remove(newItem);
                                if (LogicManager.GetItemDef(newItem).progression)
                                {
                                    pm.Add(newItem);
                                    if (RandomizerMod.Instance.Settings.RandomizeTransitions) tm.UpdateReachableTransitions(newItem, true, pm);
                                }
                            }
                        }
                    }

                    else if (ItemManager.nonShopItems.TryGetValue(location, out string item))
                    {
                        items.Remove(item);
                        
                        if (LogicManager.GetItemDef(item).progression)
                        {
                            pm.Add(item);
                            if (RandomizerMod.Instance.Settings.RandomizeTransitions) tm.UpdateReachableTransitions(item, true, pm);
                        }
                    }

                    else if (ItemManager.shopItems.TryGetValue(location, out List<string> shopItems))
                    {
                        foreach (string newItem in shopItems)
                        {
                            items.Remove(newItem);
                            if (LogicManager.GetItemDef(newItem).progression)
                            {
                                pm.Add(newItem);
                                if (RandomizerMod.Instance.Settings.RandomizeTransitions) tm.UpdateReachableTransitions(newItem, true, pm);
                            }
                        }
                    }
                    
                    else
                    {
                        Log("Unable to validate!");
                        Log($"Location {location} did not correspond to any known placement.");
                        return false;
                    }
                }

                passes++;
                if (passes > 400)
                {
                    Log("Unable to validate!");
                    Log("Progression: " + pm.ListObtainedProgression() + Environment.NewLine + "Grubs: " + pm.obtained[LogicManager.grubIndex] + Environment.NewLine + "Essence: " + pm.obtained[LogicManager.essenceIndex] + Environment.NewLine + "Flames: " + pm.obtained[LogicManager.flameIndex]);
                    string m = string.Empty;
                    foreach (string s in items) m += s + ", ";
                    Log("Unable to get items: " + m);
                    m = string.Empty;
                    foreach (string s in locations) m += s + ", ";
                    Log("Unable to get locations: " + m);
                    m = string.Empty;
                    foreach (string s in transitions) m += s + ",";
                    Log("Unable to get transitions: " + m);
                    LogItemPlacements(pm);
                    return false;
                }
            }
            //LogItemPlacements(pm);
            Log("Validation successful.");
            return true;
        }
    }
}
