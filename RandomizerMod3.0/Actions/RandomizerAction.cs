using System;
using System.Collections.Generic;
using System.Linq;
using RandomizerMod.Randomization;
using SereCore;
using UnityEngine;
using static RandomizerMod.LogHelper;
using static RandomizerMod.GiveItemActions;
using Object = UnityEngine.Object;

namespace RandomizerMod.Actions
{
    public abstract class RandomizerAction
    {
        public enum ActionType
        {
            GameObject,
            PlayMakerFSM
        }

        private static readonly List<RandomizerAction> Actions = new List<RandomizerAction>();
        public static Dictionary<string, string> AdditiveBoolNames = new Dictionary<string, string>(); // item name, additive bool name
        public static Dictionary<(string, string), string> ShopItemBoolNames = new Dictionary<(string, string), string>(); // (item name, shop name), shop item bool name

        public abstract ActionType Type { get; }

        public static void ClearActions()
        {
            Actions.Clear();
        }

        public static void CreateActions((string, string)[] items, SaveSettings settings)
        {
            ClearActions();
            
            ShopItemBoolNames = new Dictionary<(string, string), string>();
            AdditiveBoolNames = new Dictionary<string, string>();

            int newShinies = 0;
            int newGrubs = 0;
            int newRocks = 0;
            string[] shopNames = LogicManager.ShopNames;

            // Loop non-shop items
            foreach ((string newItemName, string location) in items.Where(item => !shopNames.Contains(item.Item2)))
            {
                ReqDef oldItem = LogicManager.GetItemDef(location);
                ReqDef newItem = LogicManager.GetItemDef(newItemName);

                // Solution done for MW with different settings compatibility
                if (newItemName == location)
                {
                    if (!settings.RandomizeMaps && newItem.pool == "Map")
                        continue;
                
                    if (!settings.RandomizeStags && newItem.pool == "Stag")
                        continue;
                    
                    if (!settings.RandomizeRocks && newItem.pool == "Rock")
                        continue;
                    
                    if (!settings.RandomizeSoulTotems && newItem.pool == "Soul")
                        continue;
                    
                    if (!settings.RandomizePalaceTotems && newItem.pool == "PalaceSoul")
                        continue;
                    
                    if (!settings.RandomizePalaceTablets && newItem.pool == "PalaceLore")
                        continue;
                    
                    if (!settings.RandomizeLoreTablets && newItem.pool == "Lore")
                        continue;
                }

                if (settings.NPCItemDialogue)
                {
                    if (oldItem.objectName == "NM Sheo NPC" || oldItem.objectName == "NM Mato NPC" || oldItem.objectName == "NM Oro NPC")
                    {
                        Actions.Add(new ChangeNailmasterReward(oldItem.sceneName, oldItem.objectName, oldItem.fsmName, newItem.action, newItemName, location));
                        continue;
                    }
                    else if (oldItem.objectName == "Sly Basement NPC")
                    {
                        Actions.Add(new ChangeSlyReward(oldItem.sceneName, oldItem.objectName, oldItem.fsmName, newItem.action, newItemName, location));
                        continue;
                    }
                    else if (oldItem.objectName == "Crystal Shaman")
                    {
                        Actions.Add(new ChangeCrystalShamanReward(oldItem.sceneName, oldItem.objectName, oldItem.fsmName, newItem.action, newItemName, location));
                        continue;
                    }
                    else if (oldItem.objectName == "Ruins Shaman")
                    {
                        Actions.Add(new ChangeSanctumShamanReward(oldItem.sceneName, oldItem.objectName, oldItem.fsmName, newItem.action, newItemName, location));
                        continue;
                    }
                    else if (oldItem.objectName == "Cornifer" || oldItem.objectName == "Cornifer Deepnest")
                    {
                        Actions.Add(new ChangeCorniferReward(oldItem.sceneName, oldItem.objectName, oldItem.fsmName, newItem.action, newItemName, location));
                        continue;
                    }
                }

                if (!string.IsNullOrEmpty(oldItem.inspectName))
                {
                    // For some reason, in most cases the inspect region is a separate object to the lore tablet sprite, so
                    // we have to disable it separately
                    Actions.Add(new DisableLoreTablet(oldItem.sceneName, oldItem.inspectName, oldItem.inspectFsmName));
                }
                else if ((location == "Focus" || location == "World_Sense") && !settings.RandomizeLoreTablets)
                {
                    // Disable the Focus/World Sense tablets here
                    Actions.Add(new DisableLoreTablet(oldItem.sceneName, "Tut_tablet_top", "Inspection"));
                }

                // Some objects destroy themselves based on a pdbool check via the FSM. This executes before we have
                // a chance to replace with a shiny when coming from a boss scene. Disable that behaviour here;
                // we need to do it here to cover the grub, rock cases.
                if (!string.IsNullOrEmpty(oldItem.selfDestructFsmName) && oldItem.replace)
                {
                    // With NPC Item Dialogue we shouldn't do this for the VS pickup
                    if (!(settings.NPCItemDialogue && location == "Vengeful_Spirit"))
                    {
                        Actions.Add(new PreventSelfDestruct(oldItem.sceneName, oldItem.objectName, oldItem.selfDestructFsmName));
                    }
                }

                bool hasCost = (oldItem.cost != 0 || oldItem.costType != AddYNDialogueToShiny.CostType.Geo) && !(location == "Vessel_Fragment-Basin" && settings.NPCItemDialogue);
                bool canReplaceWithObj = oldItem.elevation != 0 && !(settings.NPCItemDialogue && location == "Vengeful_Spirit") && !hasCost;
                bool replacedWithGrub = newItem.pool == "Grub" && canReplaceWithObj;
                bool replacedWithGeoRock = newItem.pool == "Rock" && canReplaceWithObj;

                if (replacedWithGrub)
                {
                    string jarName = "Randomizer Grub Jar " + newGrubs++;
                    if (oldItem.newShiny)
                    {
                        Actions.Add(new CreateNewGrubJar(oldItem.sceneName, oldItem.x, oldItem.y + CreateNewGrubJar.GRUB_JAR_ELEVATION - oldItem.elevation, jarName, newItemName, location));
                    }
                    else
                    {
                        Actions.Add(new ReplaceObjectWithGrubJar(oldItem.sceneName, oldItem.objectName, oldItem.elevation, jarName, newItemName, location));
                        // Add a PreventSelfDestruct for shiny items not typically replaced.
                        // Add the action only for items which set a bool, because only those will use a
                        // PlayerData rather than a SceneData check for their original Self Destruction.
                        if (!oldItem.replace && oldItem.fsmName == "Shiny Control" && !string.IsNullOrEmpty(oldItem.boolName))
                        {
                            Actions.Add(new PreventSelfDestruct(oldItem.sceneName, oldItem.objectName, "Shiny Control"));
                        }

                    }
                }
                else if (replacedWithGeoRock)
                {
                    string rockName = "Randomizer Geo Rock " + newRocks++;
                    GeoRockSubtype subtype = GetRockSubtype(newItem.objectName);
                    // The 420 geo rock gives 5-geo pieces, so the amount
                    // spawned must be reduced proportionally.
                    int geo = newItem.geo;
                    if (subtype == GeoRockSubtype.Outskirts420) {
                        geo /= 5;
                    }
                    if (oldItem.newShiny)
                    {
                        Actions.Add(new CreateNewGeoRock(oldItem.sceneName, oldItem.x, oldItem.y + CreateNewGeoRock.Elevation[subtype] - oldItem.elevation, rockName, newItemName, location, geo, subtype));
                    }
                    else
                    {
                        Actions.Add(new ReplaceObjectWithGeoRock(oldItem.sceneName, oldItem.objectName, oldItem.elevation, rockName, newItemName, location, geo, subtype));
                        // Add a PreventSelfDestruct for shiny items not typically replaced 
                        // Add the action only for items which set a bool, because only those will use a
                        // PlayerData rather than a SceneData check for their original Self Destruction.
                        if (!oldItem.replace && oldItem.fsmName == "Shiny Control" && !string.IsNullOrEmpty(oldItem.boolName))
                        {
                            Actions.Add(new PreventSelfDestruct(oldItem.sceneName, oldItem.objectName, "Shiny Control"));
                        }
                    }
                }
                else if (oldItem.replace)
                {
                    string replaceShinyName = "Randomizer Shiny " + newShinies++;
                    if (location == "Dream_Nail" || location == "Mask_Shard-Brooding_Mawlek" || location == "Nailmaster's_Glory" || location == "Godtuner")
                    {
                        replaceShinyName = "Randomizer Shiny"; // legacy name for scene edits
                    }
                    oldItem.fsmName = "Shiny Control";
                    oldItem.type = ItemType.Charm;

                    if (settings.NPCItemDialogue && location == "Vengeful_Spirit")
                    {
                        Actions.Add(new ReplaceObjectWithShiny(oldItem.sceneName, "Vengeful Spirit", replaceShinyName));
                        Actions.Add(new ReplaceVengefulSpiritWithShiny(oldItem.sceneName, replaceShinyName, location));
                    }
                    else if (location == "Vessel_Fragment-Basin")
                    {
                        if (settings.NPCItemDialogue)
                        {
                            Actions.Add(new ReplaceObjectWithShiny(oldItem.sceneName, oldItem.objectName, replaceShinyName));
                            Actions.Add(new ReplaceBasinVesselWithShiny(replaceShinyName));
                        }
                        else
                        {
                            Actions.Add(new CreateNewShiny(oldItem.sceneName, oldItem.x, oldItem.y, replaceShinyName));
                            oldItem.fsmName = "Shiny Control";
                        }
                    }
                    else
                    {
                        Actions.Add(new ReplaceObjectWithShiny(oldItem.sceneName, oldItem.objectName, replaceShinyName));
                    }
                    oldItem.objectName = replaceShinyName;
                }

                else if (oldItem.newShiny)
                {
                    string newShinyName = "New Shiny " + newShinies++;
                    if (location == "Simple_Key-Lurker")
                    {
                        newShinyName = "New Shiny"; // legacy name for scene edits
                    }
                    else if (location.StartsWith("Boss_Geo"))
                    {
                        newShinyName = "New Shiny Boss Geo";
                    }
                    else if (location == "Split_Mothwing_Cloak")
                    {
                        newShinyName = "New Shiny Split Cloak";
                    }
                    Actions.Add(new CreateNewShiny(oldItem.sceneName, oldItem.x, oldItem.y, newShinyName));
                    oldItem.objectName = newShinyName;
                    oldItem.fsmName = "Shiny Control";
                    oldItem.type = ItemType.Charm;
                }

                else if (oldItem.type == ItemType.Geo && newItem.type != ItemType.Geo)
                {
                    Actions.Add(new AddShinyToChest(oldItem.sceneName, oldItem.objectName, oldItem.fsmName,
                        "Randomizer Chest Shiny"));
                    oldItem.objectName = "Randomizer Chest Shiny";
                    oldItem.fsmName = "Shiny Control";
                    oldItem.type = ItemType.Charm;
                }
                else if (oldItem.type == ItemType.Flame)
                {
                    // Even if the new item is also a flame, this action should still run in order to
                    // guarantee that the player can't be locked out of getting it by upgrading their
                    // Grimmchild.
                    Actions.Add(new ChangeGrimmkinReward(oldItem.sceneName, oldItem.objectName, oldItem.fsmName, newItem.action, newItemName, location));
                    continue;
                }
                else if (oldItem.pool == "Essence_Boss")
                {
                    Actions.Add(new ChangeBossEssenceReward(oldItem.sceneName, oldItem.objectName, oldItem.fsmName, newItem.action, newItemName, location));
                    continue;
                }

                // Dream nail needs a special case
                if (location == "Dream_Nail")
                {
                    Actions.Add(new ChangeBoolTest("RestingGrounds_04", "Binding Shield Activate", "FSM", "Check",
                        newItemName, playerdata: false, 
                        altTest:() => RandomizerMod.Instance.Settings.CheckLocationFound(location)));
                    Actions.Add(new ChangeBoolTest("RestingGrounds_04", "Dreamer Plaque Inspect",
                        "Conversation Control", "End", newItemName, playerdata: false,
                        altTest:() => RandomizerMod.Instance.Settings.CheckLocationFound(location)));
                    Actions.Add(new ChangeBoolTest("RestingGrounds_04", "Dreamer Scene 2", "Control", "Init",
                        newItemName, playerdata: false,
                        altTest: () => RandomizerMod.Instance.Settings.CheckLocationFound(location)));
                    Actions.Add(new ChangeBoolTest("RestingGrounds_04", "PreDreamnail", "FSM", "Check",
                        newItemName, playerdata: false,
                        altTest: () => RandomizerMod.Instance.Settings.CheckLocationFound(location)));
                    Actions.Add(new ChangeBoolTest("RestingGrounds_04", "PostDreamnail", "FSM", "Check",
                        newItemName, playerdata: false,
                        altTest: () => RandomizerMod.Instance.Settings.CheckLocationFound(location)));
                }

                if (replacedWithGrub || replacedWithGeoRock)
                {
                    continue;
                }

                switch (newItem.type)
                {
                    default:
                        Actions.Add(new ChangeShinyIntoItem(oldItem.sceneName, oldItem.objectName, oldItem.fsmName,
                            newItem.action, newItemName, location, newItem.nameKey, newItem.shopSpriteKey));
                        if (!string.IsNullOrEmpty(oldItem.altObjectName))
                        {
                            Actions.Add(new ChangeShinyIntoItem(oldItem.sceneName, oldItem.altObjectName, oldItem.fsmName,
                                newItem.action, newItemName, location, newItem.nameKey, newItem.shopSpriteKey));
                        }
                        break;

                    case ItemType.Big:
                    case ItemType.Spell:
                        BigItemDef[] newItemsArray = GetBigItemDefArray(newItemName);

                        Actions.Add(new ChangeShinyIntoBigItem(oldItem.sceneName, oldItem.objectName,
                            oldItem.fsmName, newItemsArray, newItem.action, newItemName, location));
                        if (!string.IsNullOrEmpty(oldItem.altObjectName))
                        {
                            Actions.Add(new ChangeShinyIntoBigItem(oldItem.sceneName, oldItem.altObjectName,
                                oldItem.fsmName, newItemsArray, newItem.action, newItemName, location));
                        }

                        break;

                    case ItemType.Geo:
                        if (oldItem.inChest)
                        {
                            Actions.Add(new ChangeChestGeo(oldItem.sceneName, oldItem.chestName,
                                oldItem.chestFsmName, newItem.geo, newItemName, location));
                        }
                        else if (oldItem.type == ItemType.Geo)
                        {
                            Actions.Add(new ChangeChestGeo(oldItem.sceneName, oldItem.objectName, oldItem.fsmName,
                                    newItem.geo, newItemName, location));
                        }
                        else
                        {
                            Actions.Add(new ChangeShinyIntoGeo(oldItem.sceneName, oldItem.objectName,
                                oldItem.fsmName, newItem.geo, newItemName, location));

                            if (!string.IsNullOrEmpty(oldItem.altObjectName))
                            {
                                Actions.Add(new ChangeShinyIntoGeo(oldItem.sceneName, oldItem.altObjectName,
                                    oldItem.fsmName, newItem.geo, newItemName, location));
                            }
                        }
                        break;
                    case ItemType.Lifeblood:
                        Actions.Add(new ChangeShinyIntoLifeblood(oldItem.sceneName, oldItem.objectName, oldItem.fsmName, newItem.lifeblood, newItemName, location));
                        
                        if (!string.IsNullOrEmpty(oldItem.altObjectName))
                        {
                            Actions.Add(new ChangeShinyIntoLifeblood(oldItem.sceneName, oldItem.altObjectName,
                                oldItem.fsmName, newItem.lifeblood, newItemName, location));
                        }
                        break;

                    case ItemType.Soul:
                        Actions.Add(new ChangeShinyIntoSoul(oldItem.sceneName, oldItem.objectName,
                            oldItem.fsmName, newItemName, location));

                        if (!string.IsNullOrEmpty(oldItem.altObjectName))
                        {
                            Actions.Add(new ChangeShinyIntoSoul(oldItem.sceneName, oldItem.altObjectName,
                                oldItem.fsmName, newItemName, location));
                        }
                        break;

                    case ItemType.Lore:
                        newItem.loreSheet = string.IsNullOrEmpty(newItem.loreSheet) ? "Lore Tablets" : newItem.loreSheet;

                        Actions.Add(new ChangeShinyIntoText(oldItem.sceneName, oldItem.objectName, oldItem.fsmName,
                            newItem.loreKey, newItem.loreSheet, newItem.textType, newItemName, location));

                        if (!string.IsNullOrEmpty(oldItem.altObjectName))
                        {
                            Actions.Add(new ChangeShinyIntoText(oldItem.sceneName, oldItem.altObjectName, oldItem.fsmName,
                                newItem.loreKey, newItem.loreSheet, newItem.textType, newItemName, location));
                        }
                        break;
                }

                if (hasCost)
                {
                    int cost = oldItem.cost;
                    if (oldItem.costType == AddYNDialogueToShiny.CostType.Essence || oldItem.costType == AddYNDialogueToShiny.CostType.Grub)
                    {
                        cost = settings.VariableCosts.First(pair => pair.Item1 == location).Item2;
                    }

                    Actions.Add(new AddYNDialogueToShiny(
                        oldItem.sceneName,
                        oldItem.objectName,
                        oldItem.fsmName,
                        newItem.nameKey,
                        cost,
                        oldItem.costType));
                }
            }

            List<ChangeShopContents> shopActions = new List<ChangeShopContents>();

            // TODO: Change to use additiveItems rather than hard coded
            // No point rewriting this before making the shop component
            foreach ((string shopItem, string shopName) in items.Where(item => shopNames.Contains(item.Item2)))
            {
                ReqDef newItem = LogicManager.GetItemDef(shopItem);

                GiveAction giveAction = newItem.action;
                if (giveAction == GiveAction.SpawnGeo)
                {
                    giveAction = GiveAction.AddGeo;
                }

                string boolName = "RandomizerMod." + giveAction.ToString() + "." + shopItem + "." + shopName;

                ShopItemBoolNames[(shopItem, shopName)] = boolName;
                
                ShopItemDef newItemDef = new ShopItemDef
                {
                    PlayerDataBoolName = boolName,
                    NameConvo = newItem.nameKey,
                    DescConvo = newItem.shopDescKey,
                    RequiredPlayerDataBool = LogicManager.GetShopDef(shopName).requiredPlayerDataBool,
                    RemovalPlayerDataBool = string.Empty,
                    DungDiscount = LogicManager.GetShopDef(shopName).dungDiscount,
                    NotchCostBool = newItem.notchCost,
                    Cost = settings.ShopCosts.First(pair => pair.Item1 == shopItem).Item2,
                    SpriteName = newItem.shopSpriteKey
                };

                if (newItemDef.Cost == 0)
                {
                    newItemDef.Cost = 1;
                    LogWarn($"Found item {shopItem} in {shopName} with no saved cost.");
                }

                if (newItemDef.Cost < 5)
                {
                    newItemDef.DungDiscount = false;
                }

                ChangeShopContents existingShopAction = shopActions.FirstOrDefault(action =>
                    action.SceneName == LogicManager.GetShopDef(shopName).sceneName &&
                    action.ObjectName == LogicManager.GetShopDef(shopName).objectName);

                if (existingShopAction == null)
                {
                    shopActions.Add(new ChangeShopContents(LogicManager.GetShopDef(shopName).sceneName,
                        LogicManager.GetShopDef(shopName).objectName, new[] { newItemDef }));
                }
                else
                {
                    existingShopAction.AddItemDefs(new[] { newItemDef });
                }
            }

            shopActions.ForEach(action => Actions.Add(action));

            // Add an action for each shop to allow showing Lore
            if (settings.RandomizeLoreTablets || settings.RandomizePalaceTablets)
            {
                Actions.Add(new ShowLoreTextInShop(SceneNames.Room_shop, "UI List", "Confirm Control"));
                Actions.Add(new ShowLoreTextInShop(SceneNames.Room_mapper, "UI List", "Confirm Control"));
                Actions.Add(new ShowLoreTextInShop(SceneNames.Room_Charm_Shop, "UI List", "Confirm Control"));
                Actions.Add(new ShowLoreTextInShop(SceneNames.Fungus2_26, "UI List", "Confirm Control"));
            }
        }

        private static GeoRockSubtype GetRockSubtype(string objName) {
            GeoRockSubtype subtype = GeoRockSubtype.Default;
            if (objName.Contains("Abyss")) {
                subtype = GeoRockSubtype.Abyss;
            }
            else if (objName.Contains("City")) {
                subtype = GeoRockSubtype.City;
            }
            else if (objName.Contains("Deepnest")) {
                subtype = GeoRockSubtype.Deepnest;
            }
            else if (objName.Contains("Fung 01")) {
                subtype = GeoRockSubtype.Fung01;
            }
            else if (objName.Contains("Fung 02")) {
                subtype = GeoRockSubtype.Fung02;
            }
            else if (objName.Contains("Grave 01")) {
                subtype = GeoRockSubtype.Grave01;
            }
            else if (objName.Contains("Grave 02")) {
                subtype = GeoRockSubtype.Grave02;
            }
            else if (objName.Contains("Green Path 01")) {
                subtype = GeoRockSubtype.GreenPath01;
            }
            else if (objName.Contains("Green Path 02")) {
                subtype = GeoRockSubtype.GreenPath02;
            }
            else if (objName.Contains("Hive")) {
                subtype = GeoRockSubtype.Hive;
            }
            else if (objName.Contains("Mine")) {
                subtype = GeoRockSubtype.Mine;
            }
            else if (objName.Contains("Outskirts")) {
                subtype = GeoRockSubtype.Outskirts;
            }
            else if (objName == "Giant Geo Egg") {
                subtype = GeoRockSubtype.Outskirts420;
            }

            return ObjectCache.GetPreloadedRockType(subtype);
        }

        public static string GetAdditivePrefix(string itemName)
        {
            return LogicManager.AdditiveItemNames.FirstOrDefault(itemSet =>
                LogicManager.GetAdditiveItems(itemSet).Contains(itemName));
        }

        private static BigItemDef[] GetBigItemDefArray(string itemName)
        {
            itemName = LogicManager.RemoveDuplicateSuffix(itemName);
            string prefix = GetAdditivePrefix(itemName);
            if (prefix != null)
            {
                return LogicManager.GetAdditiveItems(prefix)
                    .Select(LogicManager.GetItemDef)
                    .Select(item => new BigItemDef
                    {
                        Name = itemName,
                        BoolName = item.boolName,
                        SpriteKey = item.bigSpriteKey,
                        TakeKey = item.takeKey,
                        NameKey = item.nameKey,
                        ButtonKey = item.buttonKey,
                        DescOneKey = item.descOneKey,
                        DescTwoKey = item.descTwoKey
                    }).ToArray();
            }

            ReqDef item2 = LogicManager.GetItemDef(itemName);
            return new[]
            {
                new BigItemDef
                {
                    Name = itemName,
                    BoolName = item2.boolName,
                    SpriteKey = item2.bigSpriteKey,
                    TakeKey = item2.takeKey,
                    NameKey = item2.nameKey,
                    ButtonKey = item2.buttonKey,
                    DescOneKey = item2.descOneKey,
                    DescTwoKey = item2.descTwoKey
                }
            };
        }

        private static string GetAdditiveBoolName(string boolName, ref Dictionary<string, int> additiveCounts)
        {
            if (additiveCounts == null)
            {
                additiveCounts = LogicManager.AdditiveItemNames.ToDictionary(str => str, str => 0);
            }

            string prefix = GetAdditivePrefix(boolName);
            if (string.IsNullOrEmpty(prefix))
            {
                return null;
            }

            additiveCounts[prefix] = additiveCounts[prefix] + 1;
            AdditiveBoolNames[boolName] = prefix + additiveCounts[prefix];
            return prefix + additiveCounts[prefix];
        }

        public static void Hook()
        {
            UnHook();

            On.PlayMakerFSM.OnEnable += ProcessFSM;
        }

        public static void UnHook()
        {
            On.PlayMakerFSM.OnEnable -= ProcessFSM;
        }

        public static void ProcessFSM(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM fsm)
        {
            orig(fsm);

            string scene = fsm.gameObject.scene.name;

            foreach (RandomizerAction action in Actions)
            {
                if (action.Type != ActionType.PlayMakerFSM)
                {
                    continue;
                }

                try
                {
                    action.Process(scene, fsm);
                }
                catch (Exception e)
                {
                    LogError(
                        $"Error processing action of type {action.GetType()}:\n{JsonUtility.ToJson(action)}\n{e}");
                }
            }
        }

        public static void EditShinies()
        {
            string scene = Ref.GM.GetSceneNameString();

            foreach (RandomizerAction action in Actions)
            {
                if (action.Type != ActionType.GameObject)
                {
                    continue;
                }

                try
                {
                    action.Process(scene, null);
                }
                catch (Exception e)
                {
                    LogError(
                        $"Error processing action of type {action.GetType()}:\n{JsonUtility.ToJson(action)}\n{e}");
                }
            }
        }

        public abstract void Process(string scene, Object changeObj);
    }
}
