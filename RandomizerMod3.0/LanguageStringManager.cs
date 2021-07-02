using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Linq;
using SereCore;
using Language;
using static RandomizerMod.LogHelper;
using RandomizerMod.Randomization;

namespace RandomizerMod
{
    public static class LanguageStringManager
    {
        private static readonly Dictionary<string, Dictionary<string, string>> LanguageStrings =
            new Dictionary<string, Dictionary<string, string>>();

        public static void LoadLanguageXML(Stream xmlStream)
        {
            // Load XmlDocument from resource stream
            XmlDocument xml = new XmlDocument();
            xml.Load(xmlStream);
            xmlStream.Dispose();

            XmlNodeList nodes = xml.SelectNodes("Language/entry");
            if (nodes == null)
            {
                LogWarn("Malformatted language xml, no nodes that match Language/entry");
                return;
            }

            foreach (XmlNode node in nodes)
            {
                string sheet = node.Attributes?["sheet"]?.Value;
                string key = node.Attributes?["key"]?.Value;

                if (sheet == null || key == null)
                {
                    LogWarn("Malformatted language xml, missing sheet or key on node");
                    continue;
                }

                SetString(sheet, key, node.InnerText.Replace("\\n", "\n"));
            }

            Log("Language xml processed");
        }

        public static void SetString(string sheetName, string key, string text)
        {
            if (string.IsNullOrEmpty(sheetName) || string.IsNullOrEmpty(key) || text == null)
            {
                return;
            }

            if (!LanguageStrings.TryGetValue(sheetName, out Dictionary<string, string> sheet))
            {
                sheet = new Dictionary<string, string>();
                LanguageStrings.Add(sheetName, sheet);
            }

            sheet[key] = text;
        }

        public static void ResetString(string sheetName, string key)
        {
            if (string.IsNullOrEmpty(sheetName) || string.IsNullOrEmpty(key))
            {
                return;
            }

            if (LanguageStrings.TryGetValue(sheetName, out Dictionary<string, string> sheet) && sheet.ContainsKey(key))
            {
                sheet.Remove(key);
            }
        }

        private static string NameOfItemPlacedAt(string location)
        {
            ReqDef item = LogicManager.GetItemDef(RandomizerMod.Instance.Settings.GetItemPlacedAt(location));
            return GetLanguageString(item.nameKey, "UI");
        }

        public static string GetLanguageString(string key, string sheetTitle)
        {
            if (sheetTitle == "Jiji" && key == "HIVE" && RandomizerMod.Instance.Settings.Jiji)
            {
                return NextJijiHint();
            }

            if (sheetTitle == "Quirrel" && RandomizerMod.Instance.Settings.Quirrel && RandomizerMod.Instance.Settings.QuirrerHintCounter < 3 &&
                new List<string> { "QUIRREL_MEET_TEMPLE_C", "QUIRREL_GREENPATH_1", "QUIRREL_QUEENSTATION_01", "QUIRREL_MANTIS_01", "QUIRREL_RUINS_1", "QUIRREL_SPA", "QUIRREL_MINES_2", "QUIRREL_FOGCANYON_A", "QUIRREL_EPILOGUE_A" }.Contains(key))
            {
                return GetQuirrelHint(key, sheetTitle);
            }

            if (RandomizerMod.Instance.Settings.ItemDepthHints && SheetsAllowedForOrdinalHints.Contains(sheetTitle))
            {
                return GetItemDepthHint();
            }

            if (key.StartsWith("RANDOMIZER_NAME_ESSENCE_"))
            {
                return key.Split('_').Last() + " Essence";
            }

            if (key.StartsWith("RANDOMIZER_NAME_GEO_"))
            {
                return key.Split('_').Last() + " Geo";
            }

            if (key.StartsWith("RANDOMIZER_NAME_GRUB"))
            {
                return $"A grub! ({PlayerData.instance.grubsCollected + 1}/46)";
            }
            if (key.StartsWith("RANDOMIZER_NAME_GRIMMKIN_FLAME"))
            {
                return $"Grimmkin Flame ({RandomizerMod.Instance.Settings.TotalFlamesCollected + 1}/10)";
            }

            if (key == "BRUMM_DEEPNEST_3" && sheetTitle == "CP2" && RandomizerMod.Instance.Settings.RandomizeGrimmkinFlames)
            {
                return Language.Language.GetInternal(key, sheetTitle).Replace("flame", NameOfItemPlacedAt("Grimmkin_Flame-Brumm"));
            }

            if (RandomizerMod.Instance.Settings.RandomizeBossEssence && sheetTitle == "Minor NPC" && key.StartsWith("BRETTA_DIARY_"))
            {
                return Language.Language.GetInternal(key, sheetTitle) + $"<page>The Maiden's Treasure<br>Pondering what to gift her saviour, the damsel thought of the precious {NameOfItemPlacedAt("Boss_Essence-Grey_Prince_Zote")} under her room. Though difficult to part with, she had nothing better with which to thank them.";
            }

            if (RandomizerMod.Instance.Settings.RandomizeSkills && sheetTitle == "Prompts" && key == "NAILMASTER_FREE")
            {
                // The Nailmasters' prompts all use the same key, so we need to distinguish them by
                // where they appear.
                switch (GameManager.instance.sceneName)
                {
                    case SceneNames.Room_nailmaster:
                        return NameOfItemPlacedAt("Cyclone_Slash");
                    case SceneNames.Room_nailmaster_02:
                        return NameOfItemPlacedAt("Great_Slash");
                    case SceneNames.Room_nailmaster_03:
                        return NameOfItemPlacedAt("Dash_Slash");
                }
            }

            if (RandomizerMod.Instance.Settings.RandomizeMaps && sheetTitle == "Cornifer" && key == "CORNIFER_PROMPT")
            {
                switch (GameManager.instance.sceneName)
                {
                    case SceneNames.Crossroads_33:
                        return NameOfItemPlacedAt("Crossroads_Map");
                    case SceneNames.Fungus1_06:
                        return NameOfItemPlacedAt("Greenpath_Map");
                    case SceneNames.Fungus3_25:
                        return NameOfItemPlacedAt("Fog_Canyon_Map");
                    case SceneNames.Fungus2_18:
                        return NameOfItemPlacedAt("Fungal_Wastes_Map");
                    case SceneNames.Deepnest_01 + "b":
                        return NameOfItemPlacedAt("Deepnest_Map-Upper");
                    case SceneNames.Fungus2_25:
                        return NameOfItemPlacedAt("Deepnest_Map-Right_[Gives_Quill]");
                    case SceneNames.Abyss_04:
                        return NameOfItemPlacedAt("Ancient_Basin_Map");
                    case SceneNames.Deepnest_East_03:
                        return NameOfItemPlacedAt("Kingdom's_Edge_Map");
                    case SceneNames.Ruins1_31:
                        return NameOfItemPlacedAt("City_of_Tears_Map");
                    case SceneNames.Waterways_09:
                        return NameOfItemPlacedAt("Royal_Waterways_Map");
                    case SceneNames.Cliffs_01:
                        return NameOfItemPlacedAt("Howling_Cliffs_Map");
                    case SceneNames.Mines_30:
                        return NameOfItemPlacedAt("Crystal_Peak_Map");
                    case SceneNames.Fungus1_24:
                        return NameOfItemPlacedAt("Queen's_Gardens_Map");
                }
            }

            if (RandomizerMod.Instance.Settings.RandomizeVesselFragments && sheetTitle == "Prompts" && key == "GEO_RELIEVE")
            {
                return Language.Language.GetInternal(key, sheetTitle).Replace("?", $" for a {NameOfItemPlacedAt("Vessel_Fragment-Basin")}?");
            }

            // Used to show which mantis claw piece we have in inventory. Changed the Mantis Claw shop name/description to
            // use a different entry, for the unlikely event that Mantis Claw and claw pieces can appear in the same seed in the future.
            // Bypass this check if they have claw (so show the usual text)

            // We don't need to check whether the player has Claw Pieces randomized; we only change the output if they have
            // exactly one claw piece
            if (!PlayerData.instance.GetBool("hasWalljump"))
            {
                if (key == "INV_NAME_WALLJUMP" && sheetTitle == "UI")
                {
                    if (RandomizerMod.Instance.Settings.GetBool(name: "hasWalljumpLeft")
                        && !RandomizerMod.Instance.Settings.GetBool(name: "hasWalljumpRight"))
                    {
                        return "Left Mantis Claw";
                    }
                    else if (!RandomizerMod.Instance.Settings.GetBool(name: "hasWalljumpLeft")
                        && RandomizerMod.Instance.Settings.GetBool(name: "hasWalljumpRight"))
                    {
                        return "Right Mantis Claw";
                    }
                }
                else if (key == "INV_DESC_WALLJUMP" && sheetTitle == "UI")
                {
                    if (RandomizerMod.Instance.Settings.GetBool(name: "hasWalljumpLeft")
                        && !RandomizerMod.Instance.Settings.GetBool(name: "hasWalljumpRight"))
                    {
                        return "Part of a claw carved from bone. Allows the wearer to cling to walls on the left and leap off of them.";
                    }
                    else if (!RandomizerMod.Instance.Settings.GetBool(name: "hasWalljumpLeft")
                        && RandomizerMod.Instance.Settings.GetBool(name: "hasWalljumpRight"))
                    {
                        return "Part of a claw carved from bone. Allows the wearer to cling to walls on the right and leap off of them.";
                    }
                }
            }

            // Same, for cloak
            if (!PlayerData.instance.GetBool("hasDash"))
            {
                if (key == "INV_NAME_DASH" && sheetTitle == "UI")
                {
                    if (RandomizerMod.Instance.Settings.GetBool(name: "canDashLeft")
                        && !RandomizerMod.Instance.Settings.GetBool(name: "canDashRight"))
                    {
                        return "Left Mothwing Cloak";
                    }
                    else if (!RandomizerMod.Instance.Settings.GetBool(name: "canDashLeft")
                        && RandomizerMod.Instance.Settings.GetBool(name: "canDashRight"))
                    {
                        return "Right Mothwing Cloak";
                    }
                }
                else if (key == "INV_DESC_DASH" && sheetTitle == "UI")
                {
                    if (RandomizerMod.Instance.Settings.GetBool(name: "canDashLeft")
                        && !RandomizerMod.Instance.Settings.GetBool(name: "canDashRight"))
                    {
                        return "Cloak threaded with mothwing strands. Allows the wearer to dash to the left along the ground or through the air.";
                    }
                    else if (!RandomizerMod.Instance.Settings.GetBool(name: "canDashLeft")
                        && RandomizerMod.Instance.Settings.GetBool(name: "canDashRight"))
                    {
                        return "Cloak threaded with mothwing strands. Allows the wearer to dash to the right along the ground or through the air.";
                    }
                }
                else if (key == "INV_NAME_SHADOWDASH" && sheetTitle == "UI")
                {
                    if (RandomizerMod.Instance.Settings.GetBool(name: "canDashLeft")
                        && !RandomizerMod.Instance.Settings.GetBool(name: "canDashRight"))
                    {
                        return "Left Shade Cloak";
                    }
                    else if (!RandomizerMod.Instance.Settings.GetBool(name: "canDashLeft")
                        && RandomizerMod.Instance.Settings.GetBool(name: "canDashRight"))
                    {
                        return "Right Shade Cloak";
                    }
                }
                else if (key == "INV_DESC_SHADOWDASH" && sheetTitle == "UI")
                {
                    if (RandomizerMod.Instance.Settings.GetBool(name: "canDashLeft")
                        && !RandomizerMod.Instance.Settings.GetBool(name: "canDashRight"))
                    {
                        return "Cloak formed from the substance of the Abyss. Allows the wearer to dash to the left through enemies and their attacks without taking damage.";
                    }
                    else if (!RandomizerMod.Instance.Settings.GetBool(name: "canDashLeft")
                        && RandomizerMod.Instance.Settings.GetBool(name: "canDashRight"))
                    {
                        return "Cloak formed from the substance of the Abyss. Allows the wearer to dash to the right through enemies and their attacks without taking damage.";
                    }
                }
            }

            // Add in some text to show which nail directions the player has
            if (key.StartsWith("INV_DESC_NAIL") && sheetTitle.StartsWith("UI"))
            {
                string nailblockstring = string.Empty;
                if (RandomizerMod.Instance.Settings.CursedNail)
                {
                    nailblockstring = "<br><br>Can be swung down";
                    if (RandomizerMod.Instance.Settings.GetBool(name: "canUpslash")) nailblockstring += ", up";
                    if (RandomizerMod.Instance.Settings.GetBool(name: "canSideslashLeft")) nailblockstring += ", left";
                    if (RandomizerMod.Instance.Settings.GetBool(name: "canSideslashRight")) nailblockstring += ", right";
                    nailblockstring += ".";
                }
                return Language.Language.GetInternal(key, sheetTitle) + nailblockstring;
            }


            if (key == "ELDERBUG_FLOWER" && sheetTitle == "Prompts")
            {
                switch (GameManager.instance.sceneName)
                {
                    // Having a switch/case because I like leaving the possibility open for adding more funny text but am too lazy to it right now
                    case SceneNames.Town:
                        return "Give Elderbug-chan the flower?";
                    default:
                        break;
                }    
            }

            if ((key == "JIJI_DOOR_NOKEY" || key == "BATH_HOUSE_NOKEY") && (sheetTitle == "Prompts") 
                && !PlayerData.instance.openedWaterwaysManhole & PlayerData.instance.simpleKeys > 0 && PlayerData.instance.simpleKeys < 2)
            {
                return "Elderbug's words echoed... There's a time and place for everything, but not now.";
            }

            if (key == "INV_NAME_SPELL_FOCUS" && sheetTitle == "UI") return "Tracker";

            if (key == "INV_DESC_SPELL_FOCUS" && sheetTitle == "UI")
            {
                StringBuilder sb = new StringBuilder();
                if (RandomizerMod.Instance.Settings.RandomizeFocus)
                {
                    if (RandomizerMod.Instance.Settings.GetBool(name: "canFocus")) sb.AppendLine("You can focus.");
                    else sb.AppendLine("You cannot focus.");
                }

                if (RandomizerMod.Instance.Settings.RandomizeSwim)
                {
                    if (RandomizerMod.Instance.Settings.GetBool(name: "canSwim")) sb.AppendLine("You can swim.");
                    else sb.AppendLine("You cannot swim.");
                }

                if (!Ref.PD.GetBool(nameof(Ref.PD.hasDreamNail)))
                {
                    int essence = Ref.PD.GetInt(nameof(Ref.PD.dreamOrbs));
                    if (essence > 0) sb.AppendLine($"You have {essence} essence.");
                }

                if (RandomizerMod.Instance.Settings.RandomizeGrimmkinFlames && PlayerData.instance.GetInt(nameof(PlayerData.grimmChildLevel)) <= 3)
                {
                    sb.AppendLine($"You have {Ref.PD.flamesCollected} unspent Flames.");
                }

                sb.AppendLine($"You've rescued {PlayerData.instance.grubsCollected} grub(s) so far!");
                int dreamers = Ref.PD.GetInt(nameof(PlayerData.guardiansDefeated));
                sb.Append($"You've found {dreamers} dreamer(s)");
                if (dreamers > 0)
                {
                    sb.AppendLine(", including:");
                    bool lurien = Ref.PD.GetBool(nameof(PlayerData.lurienDefeated));
                    bool monomon = Ref.PD.GetBool(nameof(PlayerData.monomonDefeated));
                    bool herrah = Ref.PD.GetBool(nameof(PlayerData.hegemolDefeated));

                    if (lurien)
                    {
                        sb.Append("Lurien, ");
                        dreamers--;
                    }
                    if (monomon)
                    {
                        sb.Append("Monomon, ");
                        dreamers--;
                    }
                    if (herrah)
                    {
                        sb.Append("Herrah, ");
                        dreamers--;
                    }
                    if (dreamers > 0)
                    {
                        sb.Append("Duplicate Dreamer(s)");
                    }
                }

                return sb.ToString();
            }

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(sheetTitle))
            {
                return string.Empty;
            }

            if (LanguageStrings.ContainsKey(sheetTitle) && LanguageStrings[sheetTitle].ContainsKey(key))
            {
                return LanguageStrings[sheetTitle][key];
            }

            return Language.Language.GetInternal(key, sheetTitle);
        }
        public static string NextJijiHint()
        {
            string hint = string.Empty;

            bool ValidHintItem(string item)
            {
                ReqDef def = LogicManager.GetItemDef(item);
                if (def.majorItem) return true;
                else if (def.action == GiveItemActions.GiveAction.Kingsoul) return true;
                else if (def.action == GiveItemActions.GiveAction.Dreamer) return true;
                else if (item == "Focus") return true;

                return false;
            }

            while (RandomizerMod.Instance.Settings.JijiHintCounter < RandomizerMod.Instance.Settings.MaxOrder)
            {
                string location = RandomizerMod.Instance.Settings.GetNthLocation(RandomizerMod.Instance.Settings.JijiHintCounter);
                string item = RandomizerMod.Instance.Settings.GetNthLocationItems(RandomizerMod.Instance.Settings.JijiHintCounter).FirstOrDefault(i => ValidHintItem(i));
                if (string.IsNullOrEmpty(item) || string.IsNullOrEmpty(location))
                {
                    RandomizerMod.Instance.Settings.JijiHintCounter++;
                    continue;
                }
                else if (RandomizerMod.Instance.Settings.CheckItemFound(item))
                {
                    hint = CreateJijiHint(item, location);
                    RandoLogger.LogHintToTracker(hint);
                    RandomizerMod.Instance.Settings.JijiHintCounter++;
                    continue;
                }
                else
                {
                    hint = CreateJijiHint(item, location);
                    RandoLogger.LogHintToTracker(hint);
                    RandomizerMod.Instance.Settings.JijiHintCounter++;
                    break;
                }
            }
            if (string.IsNullOrEmpty(hint)) return "Oh! I guess I couldn't find any items you left behind. Since you're doing so well, though, I think I'll be keeping this meal.";
            return hint;
        }

        public static string CreateJijiHint(string hintItemName, string hintItemSpot)
        {
            ReqDef hintItem = LogicManager.GetItemDef(hintItemName);
            string areaName;
            if (LogicManager.TryGetItemDef(hintItemSpot, out ReqDef hintSpot))
            {
                areaName = hintSpot.areaName;
            }
            else
            {
                areaName = "Shop";
            }

            bool good = false;
            int useful = 0;
            foreach ((string, string) p in RandomizerMod.Instance.Settings.ItemPlacements)
            {
                ReqDef item = LogicManager.GetItemDef(p.Item1);
                if (LogicManager.TryGetItemDef(p.Item2, out ReqDef location))
                {
                    if (location.areaName == areaName)
                    {
                        if (item.majorItem || item.pool == "Dreamer") good = true;
                        if (item.progression) useful++;
                    }
                }
                else // shop
                {
                    if (p.Item2 == hintItemSpot)
                    {
                        if (item.majorItem || item.pool == "Dreamer") good = true;
                        if (item.progression) useful++;
                    }
                }
            }
            string secondMessage;
            if (good) secondMessage = " The items there... just thinking about them is getting me excited.";
            else if (useful >= 2) secondMessage = " There are a few useful things waiting for you there.";
            else if (useful == 1) secondMessage = " I can't say whether it would be worth your time though.";
            else secondMessage = " Although it does seem awfully out of the way...";

            if (!PoolText.TryGetValue(hintItem.pool, out string hintPool))
            {
                hintPool = "A mysterious upgrade";
            }

            if (!JijiHintText.TryGetValue(areaName, out string firstMessage))
            {
                firstMessage = $"***, somewhere beyond my vision in {areaName}";
            }

            firstMessage = firstMessage.Replace("***", hintPool);

            return firstMessage + secondMessage;
        }

        public static Dictionary<string, string> PoolText = new Dictionary<string, string>
        {
            { "Dreamer", "A dreamer" },
            { "Charm", "A charm" },
            { "Skill", "A new ability" },
            { "SplitClaw", "A new ability" },
            { "SplitCloak", "A new ability" },
            { "CursedNail", "A new ability" },
            { "Key", "A useful item" },
            { "Root", "A hoard of essence" },
            { "Grub", "A helpless grub" },
            { "Stag", "A stag" },
            { "Map", "A mapping tool" },
            { "Fake", "A forgery" },
            { "Cursed", "A healing ability" }
        };

        private static Dictionary<string, string> JijiHintText = new Dictionary<string, string>
        {
            { "Kings_Pass", "Yes, I can see the items you've left behind. *** nearby, right at the entrance to this kingdom." },
            { "Dirtmouth", "Yes, I can see the items you've left behind. *** just outside, in a town quietly fading away." },
            { "Forgotten_Crossroads", "Yes, I can see the items you've left behind. *** just below us, lost amongst the kingdom's twisting roads and highways." },
            { "Black_Egg_Temple", "Yes, I can see the items you've left behind. ***, sealed by the might of three distant dreamers..." },
            { "Ancestral_Mound", "Yes, I can see the items you've left behind. *** in an ancestral mound... a place of strange worships." },
            { "Greenpath", "Yes, I can see the items you've left behind. *** in a lush, green land." },
            { "Lake_of_Unn", "Yes, I can see the items you've left behind. ***, hidden past a deadly acid lake." },
            { "Stone_Sanctuary", "Yes, I can see the items you've left behind. ***, in a dark quiet sanctuary... home only to ghosts now." },
            { "Fog_Canyon", "Yes, I can see the items you've left behind. *** lost in the fog of a strange land." },
            { "Overgrown_Mound", "Yes, I can see the items you've left behind. *** deep in a mossy cave, humming with power." },
            { "Teachers_Archives", "Yes, I can see the items you've left behind. *** in a lost library, swallowed by an acid lake." },
            { "Queens_Station", "Yes, I can see the items you've left behind. ***, sitting alone in a misty station, where the echoes of past travellers can be heard..." },
            { "Fungal_Wastes", "Yes, I can see the items you've left behind. *** nestled amongst strange fungus and bubbling lakes." },
            { "Mantis_Village", "Yes, I can see the items you've left behind. ***, guarded by a tribe of fierce warriors." },
            { "Fungal_Core", "Yes, I can see the items you've left behind. ***, in the deepest hold of the mushroom people." },
            { "Deepnest", "Yes, I can see the items you've left behind. ***, barely visible in the tunnels of a nest deep below this kingdom." },
            { "Failed_Tramway", "Yes, I can see the items you've left behind. ***, abandoned where the king's tramway came to ruin." },
            { "Weavers_Den", "Yes, I can see the items you've left behind. ***, left in the home of weavers of great skill." },
            { "Distant_Village", "Yes, I can see the items you've left behind. ***, in an empty village far, far below." },
            { "Beasts_Den", "Yes, I can see the items you've left behind. ***, protected by devoted followers of the beast." },
            { "Ancient_Basin", "Yes, I can see the items you've left behind. ***, nestled in a basin deep below the city." },
            { "Palace_Grounds", "Yes, I can see the items you've left behind. ***, lying just outside the ruins of the king's palace." },
            { "Abyss", "Yes, I can see the items you've left behind. Only faintly though... *** deep below the world, surrounded by darkness. Almost a part of it..." },
            { "Kingdoms_Edge", "Yes, I can see the items you've left behind. *** far away at the very edge of the world." },
            { "Hive", "Yes, I can see the items you've left behind. *** surrounded by golden light, in a hive far away from here." },
            { "Cast Off Shell", "Yes, I can see the items you've left behind. ***, in the king's old shell, guarded by a nimble sentinel." },
            { "Tower_of_Love", "Yes, I can see the items you've left behind. ***, in a plush tower, filled with the sound of hideous laughter." },
            { "Colosseum", "Yes, I can see the items you've left behind. ***, surrounded by warriors and fools alike." },
            { "Kings_Station", "Yes, I can see the items you've left behind. ***, above the king's flooded stagway." },
            { "City_of_Tears", "Yes, I can see the items you've left behind. *** in the heart of the kingdom's capital. Rain can not wash it away, though..." },
            { "Pleasure_House", "Yes, I can see the items you've left behind. *** in a house of pleasure, where one may rest weary bones and hear the song of lost spirits." },
            { "Soul_Sanctum", "Yes, I can see the items you've left behind. ***, in a sanctum filled with of follies and mistakes. Best not to linger there lest you face it's master..." },
            { "Royal_Waterways", "Yes, I can see the items you've left behind. ***, surrounded by pipes and running water. It can not be washed away, though..." },
            { "Junk_Pit", "Yes, I can see the items you've left behind. ***, on a heap of trash and refuse. Whether it's worth the effort to sift through the junk is up to you..." },
            { "Ismas_Grove", "Yes, I can see the items you've left behind. ***, in a secret grove, defended by a loyal knight." },
            { "Resting_Grounds", "Yes, I can see the items you've left behind. ***, in a holy place of repose." },
            { "Spirits_Glade", "Yes, I can see the items you've left behind. ***, in the secret glade of the moth tribe, guarded by a vigilant ghost." },
            { "Blue_Lake", "Yes, I can see the items you've left behind. ***, above the endless placid blue lake." },
            { "Crystal_Peak", "Yes, I can see the items you've left behind. *** almost hidden by the glow of shimmering crystals around it." },
            { "Hallownests_Crown", "Yes, I can see the items you've left behind. ***, atop the crown of a lonely mountain, looking down at the light of shimmering crystals." },
            { "Crystallized_Mound", "Yes, I can see the items you've left behind. ***, in a shaman's cave, vibrating with crystalline power." },
            { "Queens_Gardens", "Yes, I can see the items you've left behind. ***, marring a garden's beauty." },
            { "Howling_Cliffs", "Yes, I can see the items you've left behind. ***, high above us, surrounded by howling winds." },
            { "Stag_Nest", "Yes, I can see the items you've left behind. ***, in the lost home of the stags. If only one could remember where to find it..." },
            { "Shop", "Yes, I can see the items you've left behind. ***, on the shelf of a greedy merchant. But is it worth the price?" }
        };

        public static string GetQuirrelHint(string key, string sheetTitle)
        {
            RandomizerMod.Instance.Settings.QuirrerHintCounter++;
            string hint = string.Empty;
            List<string> areas = new List<string>();
            List<string> locations = new List<string>();

            switch (key)
            {
                case "QUIRREL_MEET_TEMPLE_C":
                    locations = RandomizerMod.Instance.Settings.ItemPlacements.Where(pair => new List<string> { "Dream_Nail", "Dream_Gate", "Awoken_Dream_Nail" }.Contains(pair.Item1)).Select(pair => pair.Item2).ToList();
                    foreach (string location in locations)
                    {
                        if (LogicManager.ShopNames.Contains(location))
                        {
                            areas.Add("a shop");
                        }
                        else
                        {
                            areas.Add(LogicManager.GetItemDef(location).areaName.Replace('_',' '));
                        }
                    }
                    hint = "A great stone egg, lying in the corpse of an ancient kingdom. And this egg...Is it warm? It certainly gives off a unique air." +
                        "<page>" + "Can it be opened? There are strange marks all over it..." + 
                        "<page>" + "Perhaps...I've heard rumors of artifacts in this kingdom which can open the mind and recover even the darkest of secrets." +
                        "<page>" + "The first was said to be stowed away in " + areas[0] + "." +
                        "<page>" + "The second kept safe in " + areas[1] + "." +
                        "<page>" + "And the last... somewhere in " + areas[2] + "." +
                        "<page>" + "Perhaps with one of those you might solve the riddle of this temple...";
                    break;
                case "QUIRREL_GREENPATH_1":
                    locations.Add(RandomizerMod.Instance.Settings.ItemPlacements.First(pair => pair.Item1 == "Mantis_Claw" || pair.Item1 == "Monarch_Wings").Item2);
                    foreach (string location in locations)
                    {
                        if (LogicManager.ShopNames.Contains(location))
                        {
                            areas.Add("a shop");
                        }
                        else
                        {
                            areas.Add(LogicManager.GetItemDef(location).areaName.Replace('_', ' '));
                        }
                    }
                    hint = "Oh, hello there! Seems we both tread far from the path." +
                        "<page>" + "I can hardly believe those dusty old highways led to such a lush and lively place!" +
                        "<page>" + "This building suggests some form of worship, though its idol has clearly been long forgotten. Doubles equally well for a moment's respite."
                        +"<page>" + "Oh, I picked up a good tip for you too. If you're looking to get to higher places, try checking " + areas[0] + "."
                        +"<page>" + "You should be able to find something useful there.";
                    break;
                case "QUIRREL_QUEENSTATION_01":
                    locations.Add(RandomizerMod.Instance.Settings.ItemPlacements.First(pair => pair.Item1 == "Mothwing_Cloak" || pair.Item1 == "Shade_Cloak").Item2);
                    foreach (string location in locations)
                    {
                        if (LogicManager.ShopNames.Contains(location))
                        {
                            areas.Add("a shop");
                        }
                        else
                        {
                            areas.Add(LogicManager.GetItemDef(location).areaName.Replace('_', ' '));
                        }
                    }
                    hint = "Isn't this something. I'd not expected to discover so huge a Stag Station after that foggy descent." +
                        "<page>" + "The bugs of Hallownest must've been an impressive lot, building such grand structures so far into these wilds." +
                        "<page>" + "Seems the dangerous creatures about haven't yet made their way in here. It's the perfect place for a quick rest." +
                        "<page>" + "From this point on, you're going to need to move a bit faster. I've heard tell of a special cloak that confers such a power." +
                        "<page>" + "I couldn't find it myself, but you might. Try looking in " + areas[0] + ".";
                    break;
                case "QUIRREL_MANTIS_01":
                    locations.Add(RandomizerMod.Instance.Settings.ItemPlacements.First(pair => pair.Item1 == "Lumafly_Lantern").Item2);
                    foreach (string location in locations)
                    {
                        if (LogicManager.ShopNames.Contains(location))
                        {
                            areas.Add("a shop");
                        }
                        else
                        {
                            areas.Add(LogicManager.GetItemDef(location).areaName.Replace('_', ' '));
                        }
                    }
                    hint = "Hello again! I suppose you've already met with the tribe of this village, hmm? They seem a little distrustful of strangers... to put it lightly."+
                        "<page>"+"They're not brutes though, no. The sickness in the air that clouds the mind of lesser beasts... they resist it. They retain their intellect and their honour, though also their lethal traditions."+
                        "<page>"+"I've some words of advice, my friend. If you plan to head down below, past the lords of this village, you'll find it far more difficult without a tool to light your way."+
                        "<page>"+"I saw an extra one while I was passing through " + areas[0] + ".";
                    break;
                case "QUIRREL_RUINS_1":
                    locations = RandomizerMod.Instance.Settings.ItemPlacements.Where(pair => new List<string> { "Desolate_Dive", "Descending_Dark" }.Contains(pair.Item1)).Select(pair => pair.Item2).ToList();
                    foreach (string location in locations)
                    {
                        if (LogicManager.ShopNames.Contains(location))
                        {
                            areas.Add("a shop");
                        }
                        else
                        {
                            areas.Add(LogicManager.GetItemDef(location).areaName.Replace('_', ' '));
                        }
                    }
                    hint = "The capital lies before us my friend. What a sombre place it seems and one that holds the answers to many a mystery."+
                        "<page>"+"I too have felt the pull of this place, though now I sit before it I find myself hesitant to descend."+
                        "<page>"+"Is it fear I wonder, or something else that holds me back?"+
                        "<page>"+"The stories of the terrible research done in the sanctum before us are too terrible to repeat. Supposedly they developed a spell to smash through the ground, but the art was lost when the city fell."+
                        "<page>"+"If you need that kind of power, try searching " + areas[0] + ". That seems like the most likely place to find it. You might also find it in " + areas[1] + " but that will take some digging.";
                    break;
                case "QUIRREL_SPA":
                    locations.Add(RandomizerMod.Instance.Settings.ItemPlacements.Last(pair => pair.Item1 == "Mothwing_Cloak" || pair.Item1 == "Shade_Cloak").Item2);
                    foreach (string location in locations)
                    {
                        if (LogicManager.ShopNames.Contains(location))
                        {
                            areas.Add("a shop");
                        }
                        else
                        {
                            areas.Add(LogicManager.GetItemDef(location).areaName.Replace('_', ' '));
                        }
                    }
                    hint = "Hello, hello! What a thrill this is, to find such warm comfort amidst the den of beasts."+
                        "<page>"+"This is a ferocious place no doubt. Supposedly, there's a village deep in the warren. Its inhabitants never accepted Hallownest's King."+
                        "<page>"+"By the way, have you noticed those strange black barriers strewn about?"+
                        "<page>"+"Supposedly the tool to get past them was sealed away in " + areas[0] +", but who can say for sure?";
                    break;
                case "QUIRREL_MINES_2":
                    locations.Add(RandomizerMod.Instance.Settings.ItemPlacements.Last(pair => pair.Item1 == "Crystal_Heart").Item2);
                    foreach (string location in locations)
                    {
                        if (LogicManager.ShopNames.Contains(location))
                        {
                            areas.Add("a shop");
                        }
                        else
                        {
                            areas.Add(LogicManager.GetItemDef(location).areaName.Replace('_', ' '));
                        }
                    }
                    hint = "Did it sadden you to see those miners below, still labouring at their endless task?" +
                        "<page>" + "Even overcome, strong purpose has been imprinted upon their husks." +
                        "<page>" + "The crystal ore is said to contain a sort of energy, not as powerful as the soul the city dwellers harnessed but far less lethal." +
                        "<page>" + "Now, the cores they use to power their tunneling machines must've had incredible power, but not many remain. The only one I know of was moved to " + areas[0] + ".";
                    break;
                case "QUIRREL_FOGCANYON_A":
                    locations.Add(RandomizerMod.Instance.Settings.ItemPlacements.Last(pair => pair.Item1 == "Isma's_Tear").Item2);
                    foreach (string location in locations)
                    {
                        if (LogicManager.ShopNames.Contains(location))
                        {
                            areas.Add("a shop");
                        }
                        else
                        {
                            areas.Add(LogicManager.GetItemDef(location).areaName.Replace('_', ' '));
                        }
                    }
                    hint = "Doesn't this kingdom just abound with surprises? A building atop an acid lake." +
                        "<page>" + "Speaking of, I'm impressed you were able to make it this far. The acid is quite threatening to our kind, without the proper protection." +
                        "<page>" + "If you still need that ability, I'd suggest searching around " + areas[0] +
                        "<page>"+"Despite the sight, I can't help but feel... familiarity? Something stirs in my mind, though I can't yet tell what.."+
                        "<page>"+"I'd thought it my lust for discovery that led me here but now there seems something else."+
                        "<page>"+"This building beckons me.";
                    break;
                case "QUIRREL_EPILOGUE_A":
                    locations.Add(RandomizerMod.Instance.Settings.ItemPlacements.First(pair => pair.Item1 == "Mantis_Claw" || pair.Item1 == "Monarch_Wings").Item2);
                    foreach (string location in locations)
                    {
                        if (LogicManager.ShopNames.Contains(location))
                        {
                            areas.Add("a shop");
                        }
                        else
                        {
                            areas.Add(LogicManager.GetItemDef(location).areaName.Replace('_', ' '));
                        }
                    }
                    hint = "Again we meet, my short friend. Here at last, I feel at peace."+
                        "<page>"+"I suppose your quest is still far from over though. If you still seek greater heights, go to " + areas[0] + ". You should find it there, I hope..."+
                        "<page>"+"Twice I've seen this world and though my service may have stripped the first experience from me, I'm thankful I could witness its beauty again."+
                        "<page>"+"Hallownest is a vast and wondrous thing, but with as many wonders as it holds, I've seen none quite so intriguing as you."+
                        "<page>"+"Ha. My flattery returns only silent stoicism. I like that."+
                        "<page>"+"I like that very much.";
                    break;
                default:
                    LogWarn("Unknown key passed to GetQuirrelHint");
                    break;
            }
            if (hint == string.Empty)
                return Language.Language.GetInternal(key, sheetTitle);

            RandoLogger.LogHintToTracker(hint, jiji: false, quirrel: true);
            return hint;
        }

        private static string[] SheetsAllowedForOrdinalHints =
        {
            //"Charm Slug",
            //"Nailmasters",
            "Hunter",
            "Elderbug",
            //"CP2",
            "Quirrel",
            //"Iselda",
            //"Zote",
            //"Cornifer",
            "Nailsmith",
            //"Minor NPC",
            //"Ghosts", 
            //"Dreamers",
            //"Sly",
            "Dream Witch",
            "Shaman",
            "Hornet",
            //"Relic Dealer",
            "Banker",
            //"Stag"
        };

        public static string GetItemDepthHint()
        {
            int IGTMinute =(int)Math.Floor(TimeSpan.FromSeconds(GameManager.instance.PlayTime + PlayerData.instance.playTime).TotalMinutes);
            Random rand = new Random(RandomizerMod.Instance.Settings.Seed + IGTMinute);
            bool giveItemHint = rand.Next(1) == 0;
            if (giveItemHint)
            {
                int order = rand.Next(RandomizerMod.Instance.Settings.MaxOrder);
                string[] itemList = RandomizerMod.Instance.Settings.GetNthLocationItems(order);
                string item = itemList[rand.Next(itemList.Length)];
                item = GetLanguageString(LogicManager.GetItemDef(item).nameKey, "UI");
                if (item.StartsWith("A grub")) item = "grub";
                int difficulty = Math.Min((itemHintSecondPart.Count * order) / RandomizerMod.Instance.Settings.MaxOrder, itemHintSecondPart.Count);

                return $"{itemHintFirstPart[rand.Next(itemHintFirstPart.Count)].Replace("***", item)}<page>{itemHintSecondPart[difficulty]}";
            }
            else
            {
                int order = rand.Next(RandomizerMod.Instance.Settings.MaxOrder);
                string location = RandomizerMod.Instance.Settings.GetNthLocation(order).Replace('_',' ');
                int difficulty = Math.Min((itemHintSecondPart.Count * order) / RandomizerMod.Instance.Settings.MaxOrder, itemHintSecondPart.Count);

                return $"{locationHintFirstPart[rand.Next(locationHintFirstPart.Count)].Replace("***", location)}<page>{locationHintSecondPart[difficulty]}";
            }
        }

        public static List<string> itemHintFirstPart = new List<string>
        {
            "So listen to this. This morning I went for a walk and found a ***!",
            "Keep this a secret, but last week I tried digging up the graveyard in Dirtmouth and came up with a ***.",
            "Ugh, I'm embarrassed to even say this, but someone tried to give me a *** as a present the other day.",
        };

        private static List<string> itemHintSecondPart = new List<string>
        {
            "I got ambushed by a horde of crawlids, or they might have been tiktiks, I can never tell the difference. Anyways, I ended up dropping the item I mentioned, but it should still be pretty easy to find.<page>I'd rate the location a 1 out of 5 in time investment.",
            "I kind of panicked after getting it though, and ended up dropping it somewhere. It really wouldn't be all that hard to get back there, but I couldn't be bothered to make the trip.<page>I'd rate the location a 2 out of 5 as far as time investment goes.",
            "I was looking down to open the toll though when someone nabbed it from me. I didn't see where they ran off to, but I'm pretty sure you could pick up the trail with standard equipment.<page>I'd rate the location a 3 out of 5 in time investment.",
            "I have too many of those anyways, so I tied it to a vengefly and let the little guy fly away. You'll definitely need to prepare if you want to find it now.<page>I'd rate the location a 4 out of 5 just looking at time investment.",
            "But after thinking about it, I decided that's just too much power for one person to have. I ended up putting it back somewhere you won't be able to get to for a long, long time.<page>Definitely a maximum time investment location."
        };

        private static List<string> locationHintFirstPart = new List<string>
        {
            "I've been keeping this to myself, but I went for a stroll earlier and saw an item over at ***!",
            "Take a look at this map I found in my attic. It says there's treasure hidden at ***.",
            "I'm planning a scavenger hunt next week. What do you think about putting something by ***?"
        };

        private static List<string> locationHintSecondPart = new List<string>
        {
            "It's a pretty easy place to get to, wouldn't you say?<page>I'd give it a 1 out of 5 for time investment.",
            "I know it isn't that bad to get there, but I just can't be bothered to make the trip.<page>If you compare to the rest of my options, that location has to be pushing 2 out of 5 for time investment.",
            "I'll probably have to pick up the usual gear before I head over there.<page>I'd guess the total trip will be about a 3 out of 5 in time investment.",
            "I don't know, though, it definitely take preparation if you're going the whole way there.<page>Probably around a 4 out of 5 in time investment, so maybe go for the low hanging fruit first.",
            "That's a long, long ways away though. I really can't imagine committing to that unless I knew something incredible had to be there.<page>Talk about a maximum time investment location."
        };

        // very critical
        private static string OrdinalSuffix(int n)
        {
            n %= 10;
            switch (n)
            {
                case 1:
                    return "st";
                case 2:
                    return "nd";
                case 3:
                    return "rd";
                default:
                    return "th";
            }
        }
    }
}
