using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Modding;
using RandomizerMod.Actions;
using RandomizerMod.Randomization;
using SereCore;
using UnityEngine;
using UnityEngine.SceneManagement;
using HutongGames.PlayMaker;
using RandomizerMod.FsmStateActions;
using static RandomizerMod.LogHelper;
using static RandomizerMod.GiveItemActions;
using RandomizerMod.SceneChanges;
using System.Security.Cryptography;

using Object = UnityEngine.Object;
using RandomizerMod.MultiWorld;

namespace RandomizerMod
{
    public class RandomizerMod : Mod, IMultiWorldCompatibleRandomizer
    {
        private static Dictionary<string, Sprite> _sprites;
        private static Dictionary<string, string> _secondaryBools;

        private static Thread _logicParseThread;

        public static RandomizerMod Instance { get; private set; }

        public GlobalSettings globalSettings { get; set; } = new GlobalSettings();
        public SaveSettings Settings { get; set; } = new SaveSettings();

        public override ModSettings SaveSettings
        {
            get => Settings = Settings ?? new SaveSettings();
            set => Settings = value is SaveSettings saveSettings ? saveSettings : Settings;
        }

        public override ModSettings GlobalSettings
        {
            get => globalSettings = globalSettings ?? new GlobalSettings();
            set => globalSettings = value is GlobalSettings gSettings ? gSettings : globalSettings;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloaded)
        {
            if (Instance != null)
            {
                LogWarn("Attempting to make multiple instances of mod, ignoring");
                return;
            }

            // Set instance for outside use
            Instance = this;

            // Make sure the play mode screen is always unlocked
            Ref.GM.EnablePermadeathMode();

            // Unlock godseeker too because idk why not
            Ref.GM.SetStatusRecordInt("RecBossRushMode", 1);

            Assembly randoDLL = GetType().Assembly;

            // Load embedded resources
            _sprites = ResourceHelper.GetSprites("RandomizerMod.Resources.");
            
            try
            {
                LanguageStringManager.LoadLanguageXML(
                    randoDLL.GetManifestResourceStream("RandomizerMod.Resources.language.xml"));
            }
            catch (Exception e)
            {
                LogError("Could not process language xml:\n" + e);
            }

            _logicParseThread = new Thread(() =>
            LogicManager.ParseXML(randoDLL));
            _logicParseThread.Start();

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnMainMenu;

            // Setup preloaded objects
            ObjectCache.GetPrefabs(preloaded);

            // Some items have two bools for no reason, gotta deal with that
            _secondaryBools = new Dictionary<string, string>
            {
                {nameof(PlayerData.hasDash), nameof(PlayerData.canDash)},
                {nameof(PlayerData.hasShadowDash), nameof(PlayerData.canShadowDash)},
                {nameof(PlayerData.hasSuperDash), nameof(PlayerData.canSuperDash)},
                {nameof(PlayerData.hasWalljump), nameof(PlayerData.canWallJump)},
                {nameof(PlayerData.gotCharm_23), nameof(PlayerData.fragileHealth_unbreakable)},
                {nameof(PlayerData.gotCharm_24), nameof(PlayerData.fragileGreed_unbreakable)},
                {nameof(PlayerData.gotCharm_25), nameof(PlayerData.fragileStrength_unbreakable)}
            };

            _logicParseThread.Join(); // new update -- logic manager is needed to supply start locations to menu
            MenuChanger.EditUI();
        }

        public override List<(string, string)> GetPreloadNames()
        {
            var preloads = new List<(string, string)>
            {
                (SceneNames.Tutorial_01, "_Props/Chest/Item/Shiny Item (1)"),
                (SceneNames.Tutorial_01, "_Enemies/Crawler 1"),
                (SceneNames.Tutorial_01, "_Props/Cave Spikes (1)"),
                (SceneNames.Tutorial_01, "_Markers/Death Respawn Marker"),
                (SceneNames.Tutorial_01, "_Scenery/plat_float_17"),
                (SceneNames.Tutorial_01, "_Props/Tut_tablet_top"),
                (SceneNames.Tutorial_01, "_Props/Geo Rock 1"),
                (SceneNames.Cliffs_02, "Soul Totem 5"),
                //(SceneNames.Room_Jinn, "Jinn NPC"),
                (SceneNames.Abyss_19, "Grub Bottle/Grub"),
                (SceneNames.Abyss_19, "Grub Bottle")
            };
            if (!globalSettings.ReducePreloads)
            {
                preloads.AddRange(new List<(string, string)>
                {
                    (SceneNames.Abyss_19, "Geo Rock Abyss"),
                    (SceneNames.Ruins2_05, "Geo Rock City 1"),
                    (SceneNames.Deepnest_02, "Geo Rock Deepnest"),
                    (SceneNames.Fungus2_11, "Geo Rock Fung 01"),
                    (SceneNames.Fungus2_11, "Geo Rock Fung 02"),
                    (SceneNames.RestingGrounds_10, "Geo Rock Grave 01"),
                    (SceneNames.RestingGrounds_10, "Geo Rock Grave 02"),
                    (SceneNames.Fungus1_12, "Geo Rock Green Path 01"),
                    (SceneNames.Fungus1_12, "Geo Rock Green Path 02"),
                    (SceneNames.Hive_01, "Geo Rock Hive"),
                    (SceneNames.Mines_20, "Geo Rock Mine (4)"),
                    (SceneNames.Deepnest_East_17, "Geo Rock Outskirts"),
                    (SceneNames.Deepnest_East_17, "Giant Geo Egg")
                });
            }
            return preloads;
        }

        public void HookRandomizer()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += HandleSceneChanges;
            ModHooks.Instance.LanguageGetHook += LanguageStringManager.GetLanguageString;
            ModHooks.Instance.GetPlayerIntHook += IntOverride;
            ModHooks.Instance.GetPlayerBoolHook += BoolGetOverride;
            ModHooks.Instance.SetPlayerBoolHook += BoolSetOverride;
            On.PlayMakerFSM.OnEnable += FixVoidHeart;
            On.PlayMakerFSM.OnEnable += FixFury;
            On.GameManager.BeginSceneTransition += EditTransition;
            On.PlayerData.CountGameCompletion += RandomizerCompletion;
            On.PlayerData.SetInt += FixGrimmkinUpgradeCost;

            RecentItems.Hook();

            CustomSkills.Hook();
            RandomizerAction.Hook();
            SceneEditor.Hook();

            HookBenchwarp();
        }

        public void UnhookRandomizer()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= HandleSceneChanges;
            ModHooks.Instance.LanguageGetHook -= LanguageStringManager.GetLanguageString;
            ModHooks.Instance.GetPlayerIntHook -= IntOverride;
            ModHooks.Instance.GetPlayerBoolHook -= BoolGetOverride;
            ModHooks.Instance.SetPlayerBoolHook -= BoolSetOverride;
            On.PlayMakerFSM.OnEnable -= FixVoidHeart;
            On.PlayMakerFSM.OnEnable -= FixFury;
            On.GameManager.BeginSceneTransition -= EditTransition;
            On.PlayerData.CountGameCompletion -= RandomizerCompletion;
            On.PlayerData.SetInt -= FixGrimmkinUpgradeCost;

            RecentItems.UnHook();
            CustomSkills.UnHook();
            RandomizerAction.UnHook();
            SceneEditor.UnHook();

            UnHookBenchwarp();
        }

        private static Func<
                    (string respawnScene, string respawnMarkerName, int respawnType, int mapZone),
                    (string respawnScene, string respawnMarkerName, int respawnType, int mapZone)
                    >
                    BenchwarpGetStartDef = def => Instance == null || Instance.Settings == null ? def :
                    (Instance.Settings.StartSceneName, Instance.Settings.StartRespawnMarkerName, Instance.Settings.StartRespawnType, Instance.Settings.StartMapZone);

        private void HookBenchwarp()
        {
            try
            {
                FieldInfo field = Type.GetType("Benchwarp.Events, Benchwarp")
                    .GetField("OnGetStartDef", BindingFlags.Public | BindingFlags.Static);

                field.FieldType
                    .GetEvent("Event", BindingFlags.Public | BindingFlags.Instance)
                    .AddEventHandler(field.GetValue(null), BenchwarpGetStartDef);
            }
            catch
            {
                LogWarn("Randomizer was unable to access Benchwarp. Installing the latest version of Benchwarp is strongly advised.");
                return;
            }
        }

        private void UnHookBenchwarp()
        {
            try
            {
                FieldInfo field = Type.GetType("Benchwarp.Events, Benchwarp")
                    .GetField("OnGetStartDef", BindingFlags.Public | BindingFlags.Static);

                field.FieldType
                    .GetEvent("Event", BindingFlags.Public | BindingFlags.Instance)
                    .RemoveEventHandler(field.GetValue(null), BenchwarpGetStartDef);
            }
            catch
            {
                return;
            }
        }


        public static Sprite GetSprite(string name)
        {
            if (_sprites != null && _sprites.TryGetValue(name, out Sprite sprite))
            {
                return sprite;
            }
            return null;
        }

        public static bool LoadComplete()
        {
            return _logicParseThread == null || !_logicParseThread.IsAlive;
        }

        public MultiWorldMenu CreateMultiWorldMenu()
        {
            return MenuChanger.CreateMultiWorldMenu();
        }

        public void StartNewGame()
        {
            if (!Settings.Randomizer)
            {
                return;
            }

            HookRandomizer();

            if (!LoadComplete())
            {
                _logicParseThread.Join();
            }

            RandoLogger.InitializeSpoiler();
            RandoLogger.InitializeCondensedSpoiler();

            try
            {
                Randomizer.Randomize();
        
                RandoLogger.UpdateHelperLog();
            }
            catch (Exception e)
            {
                LogError("Error in randomization:\n" + e);
            }

            RandoLogger.InitializeTracker();
        }

        public int MakeAssemblyHash()
        {
            SHA1 sha1 = SHA1.Create();
            FileStream stream = File.OpenRead(Assembly.GetExecutingAssembly().Location);
            byte[] hash = sha1.ComputeHash(stream).ToArray();
            stream.Dispose();
            sha1.Clear();

            unchecked
            {
                int val = 0;
                for (int i = 0; i < hash.Length - 1; i += 4)
                {
                    val = 17 * val + 31 * BitConverter.ToInt32(hash, i);
                }
                return val;
            }
        }

        public override string GetVersion()
        {
            string ver = "3.12.5MW";

            ver += $"({Math.Abs(MakeAssemblyHash() % 997)})";

            int minAPI = 53;

            bool apiTooLow = Convert.ToInt32(ModHooks.Instance.ModVersion.Split('-')[1]) < minAPI;
            if (apiTooLow)
            {
                return ver + " (Update API)";
            }

            return ver;
        }

        private void RandomizerCompletion(On.PlayerData.orig_CountGameCompletion orig, PlayerData self)
        {
            if (!RandomizerMod.Instance.Settings.Randomizer)
            {
                orig(self);
                return;
            }

            float placedItems = (float)RandomizerMod.Instance.Settings.GetNumLocations();
            float foundItems = (float)RandomizerMod.Instance.Settings.NumItemsFound;

            // Count a pair (in, out) as a single transition check
            float randomizedTransitions = RandomizerMod.Instance.Settings.RandomizeRooms ? 445f :
                                            RandomizerMod.Instance.Settings.RandomizeAreas ? 80f : 0f;
            float foundTransitions = (float)RandomizerMod.Instance.Settings.GetTransitionsFound().Length / 2f;
            if (placedItems == 0 && randomizedTransitions == 0)
            {
                PlayerData.instance.completionPercentage = 0;
                return;
            }

            float rawPercent = (foundItems + foundTransitions) / (placedItems + randomizedTransitions) * 100f;

            PlayerData.instance.completionPercentage = (float)Math.Floor(rawPercent);
        }

        private void UpdateCharmNotches(PlayerData pd)
        {
            // Update charm notches
            if (Settings.CharmNotch)
            {
                if (pd == null)
                {
                    return;
                }

                pd.CountCharms();
                int charms = pd.charmsOwned;
                int notches = pd.charmSlots;

                if (!pd.salubraNotch1 && charms >= 5)
                {
                    pd.SetBool(nameof(PlayerData.salubraNotch1), true);
                    notches++;
                }

                if (!pd.salubraNotch2 && charms >= 10)
                {
                    pd.SetBool(nameof(PlayerData.salubraNotch2), true);
                    notches++;
                }

                if (!pd.salubraNotch3 && charms >= 18)
                {
                    pd.SetBool(nameof(PlayerData.salubraNotch3), true);
                    notches++;
                }

                if (!pd.salubraNotch4 && charms >= 25)
                {
                    pd.SetBool(nameof(PlayerData.salubraNotch4), true);
                    notches++;
                }

                pd.SetInt(nameof(PlayerData.charmSlots), notches);
                Ref.GM.RefreshOvercharm();
            }
        }

        private bool BoolGetOverride(string boolName)
        {
            // Fake spell bools
            if (boolName == "hasVengefulSpirit")
            {
                return Ref.PD.fireballLevel > 0;
            }

            if (boolName == "hasShadeSoul")
            {
                return Ref.PD.fireballLevel > 1;
            }

            if (boolName == "hasDesolateDive")
            {
                return Ref.PD.quakeLevel > 0;
            }

            if (boolName == "hasDescendingDark")
            {
                return Ref.PD.quakeLevel > 1;
            }

            if (boolName == "hasHowlingWraiths")
            {
                return Ref.PD.screamLevel > 0;
            }

            if (boolName == "hasAbyssShriek")
            {
                return Ref.PD.screamLevel > 1;
            }

            // This variable is incredibly stubborn, not worth the effort to make it cooperate
            // Just override it completely
            if (boolName == nameof(PlayerData.gotSlyCharm) && Settings.Randomizer)
            {
                return Settings.SlyCharm;
            }

            if (boolName == nameof(PlayerData.spiderCapture))
            {
                return false;
            }

            // Make Happy Couple require obtaining whatever item Sheo gives, instead of Great Slash
            if (boolName == nameof(PlayerData.nailsmithSheo) && Settings.RandomizeSkills)
            {
                return Settings.NPCItemDialogue && PlayerData.instance.GetBoolInternal(nameof(PlayerData.nailsmithSpared)) && Settings.CheckLocationFound("Great_Slash");
            }

            if (boolName == nameof(PlayerData.corniferAtHome))
            {
                if (!Settings.RandomizeMaps)
                {
                    return PlayerData.instance.GetBoolInternal(boolName);
                }
                return !Settings.NPCItemDialogue || (
                       Settings.CheckLocationFound("Greenpath_Map") &&
                       Settings.CheckLocationFound("Fog_Canyon_Map") &&
                       Settings.CheckLocationFound("Fungal_Wastes_Map") &&
                       Settings.CheckLocationFound("Deepnest_Map-Upper") &&
                       Settings.CheckLocationFound("Deepnest_Map-Right_[Gives_Quill]") &&
                       Settings.CheckLocationFound("Ancient_Basin_Map") &&
                       Settings.CheckLocationFound("Kingdom's_Edge_Map") &&
                       Settings.CheckLocationFound("City_of_Tears_Map") &&
                       Settings.CheckLocationFound("Royal_Waterways_Map") &&
                       Settings.CheckLocationFound("Howling_Cliffs_Map") &&
                       Settings.CheckLocationFound("Crystal_Peak_Map") &&
                       Settings.CheckLocationFound("Queen's_Gardens_Map"));
            }

            if (boolName == nameof(PlayerData.instance.openedMapperShop))
            {
                // Iselda is now always unlocked
                return true || PlayerData.instance.GetBoolInternal(boolName) ||
                    (!RandomizerMod.Instance.Settings.RandomizeMaps &&
                    (
                    PlayerData.instance.GetBoolInternal(nameof(PlayerData.corn_cityLeft)) ||
                    PlayerData.instance.GetBoolInternal(nameof(PlayerData.corn_abyssLeft)) ||
                    PlayerData.instance.GetBoolInternal(nameof(PlayerData.corn_cliffsLeft)) ||
                    PlayerData.instance.GetBoolInternal(nameof(PlayerData.corn_crossroadsLeft)) ||
                    PlayerData.instance.GetBoolInternal(nameof(PlayerData.corn_deepnestLeft)) ||
                    PlayerData.instance.GetBoolInternal(nameof(PlayerData.corn_fogCanyonLeft)) ||
                    PlayerData.instance.GetBoolInternal(nameof(PlayerData.corn_fungalWastesLeft)) ||
                    PlayerData.instance.GetBoolInternal(nameof(PlayerData.corn_greenpathLeft)) ||
                    PlayerData.instance.GetBoolInternal(nameof(PlayerData.corn_minesLeft)) ||
                    PlayerData.instance.GetBoolInternal(nameof(PlayerData.corn_outskirtsLeft)) ||
                    PlayerData.instance.GetBoolInternal(nameof(PlayerData.corn_royalGardensLeft)) ||
                    PlayerData.instance.GetBoolInternal(nameof(PlayerData.corn_waterwaysLeft)) ||
                    PlayerData.instance.GetBoolInternal(nameof(PlayerData.openedRestingGrounds))
                    ));
            }

            if (boolName.StartsWith("RandomizerMod."))
            {
                // format is RandomizerMod.GiveAction.ItemName.LocationName for shop bools. Only the item name is used for savesettings bools
                return Settings.CheckItemFound(boolName.Split('.')[2]);
            }
            
            if (RandomizerMod.Instance.Settings.RandomizeRooms && (boolName == "troupeInTown" || boolName == "divineInTown")) return false;
            //if (boolName == "crossroadsInfected" && RandomizerMod.Instance.Settings.RandomizeRooms
            //    && new List<string> { SceneNames.Crossroads_03, SceneNames.Crossroads_06, SceneNames.Crossroads_10, SceneNames.Crossroads_19 }.Contains(GameManager.instance.sceneName)) return false;

            return Ref.PD.GetBoolInternal(boolName);
        }

        private void BoolSetOverride(string boolName, bool value)
        {
            PlayerData pd = Ref.PD;

            // It's just way easier if I can treat spells as bools
            if (boolName == "hasVengefulSpirit" && value && pd.fireballLevel <= 0)
            {
                pd.SetInt("fireballLevel", 1);
            }
            else if (boolName == "hasVengefulSpirit" && !value)
            {
                pd.SetInt("fireballLevel", 0);
            }
            else if (boolName == "hasShadeSoul" && value)
            {
                pd.SetInt("fireballLevel", 2);
            }
            else if (boolName == "hasShadeSoul" && !value && pd.fireballLevel >= 2)
            {
                pd.SetInt("fireballLevel", 1);
            }
            else if (boolName == "hasDesolateDive" && value && pd.quakeLevel <= 0)
            {
                pd.SetInt("quakeLevel", 1);
            }
            else if (boolName == "hasDesolateDive" && !value)
            {
                pd.SetInt("quakeLevel", 0);
            }
            else if (boolName == "hasDescendingDark" && value)
            {
                pd.SetInt("quakeLevel", 2);
            }
            else if (boolName == "hasDescendingDark" && !value && pd.quakeLevel >= 2)
            {
                pd.SetInt("quakeLevel", 1);
            }
            else if (boolName == "hasHowlingWraiths" && value && pd.screamLevel <= 0)
            {
                pd.SetInt("screamLevel", 1);
            }
            else if (boolName == "hasHowlingWraiths" && !value)
            {
                pd.SetInt("screamLevel", 0);
            }
            else if (boolName == "hasAbyssShriek" && value)
            {
                pd.SetInt("screamLevel", 2);
            }
            else if (boolName == "hasAbyssShriek" && !value && pd.screamLevel >= 2)
            {
                pd.SetInt("screamLevel", 1);
            }

            else if (boolName.StartsWith("RandomizerMod."))
            {
                // format is RandomizerMod.GiveAction.ItemName.LocationName for shop bools. Only the item name is used for savesettings bools

                string[] pieces = boolName.Split('.');
                pieces[1].TryToEnum(out GiveAction giveAction);
                string item = pieces[2];
                string location = pieces[3];

                GiveItem(giveAction, item, location);
                return;
            }
            // Send the set through to the actual set
            pd.SetBoolInternal(boolName, value);

            // Check if there is a secondary bool for this item
            if (_secondaryBools.TryGetValue(boolName, out string secondaryBoolName))
            {
                pd.SetBool(secondaryBoolName, value);
            }

            if (boolName == nameof(PlayerData.hasCyclone) || boolName == nameof(PlayerData.hasUpwardSlash) ||
                boolName == nameof(PlayerData.hasDashSlash))
            {
                // Make nail arts work
                bool hasCyclone = pd.GetBool(nameof(PlayerData.hasCyclone));
                bool hasUpwardSlash = pd.GetBool(nameof(PlayerData.hasUpwardSlash));
                bool hasDashSlash = pd.GetBool(nameof(PlayerData.hasDashSlash));

                pd.SetBool(nameof(PlayerData.hasNailArt), hasCyclone || hasUpwardSlash || hasDashSlash);
                pd.SetBool(nameof(PlayerData.hasAllNailArts), hasCyclone && hasUpwardSlash && hasDashSlash);
            }
            else if (boolName == nameof(PlayerData.hasDreamGate) && value)
            {
                // Make sure the player can actually use dream gate after getting it
                FSMUtility.LocateFSM(Ref.Hero.gameObject, "Dream Nail").FsmVariables
                    .GetFsmBool("Dream Warp Allowed").Value = true;
            }
            else if (boolName == nameof(PlayerData.hasAcidArmour) && value)
            {
                // Gotta update the acid pools after getting this
                PlayMakerFSM.BroadcastEvent("GET ACID ARMOUR");
            }
            else if (boolName == nameof(PlayerData.hasShadowDash) && value)
            {
                // Apparently this is enough to disable the shade gate walls
                EventRegister.SendEvent("GOT SHADOW DASH");
            }
            else if (boolName.StartsWith("gotCharm_"))
            {
                // Check for Salubra notches if it's a charm
                UpdateCharmNotches(pd);
            }
        }

        private int IntOverride(string intName)
        {
            if (intName == "RandomizerMod.Zero")
            {
                return 0;
            }
            // Grimm only appears in his tent if the player has exactly 3 flames. Hide any excess
            // flames (which can only happen when flames are randomized) from the game.
            // Increments of the variable (collecting flames) will still increment the real value.
            if (Settings.RandomizeGrimmkinFlames && intName == "flamesCollected")
            {
                int n = Ref.PD.GetIntInternal(intName);
                return n > 3 ? 3 : n;
            }

            return Ref.PD.GetIntInternal(intName);
        }

        // When upgrading Grimmchild, Grimm sets the flame counter to 0. If there are excess flames,
        // this is wrong; we want those flames to carry over to the next level.
        // To avoid conflicts with other mods, we hook PlayerData.SetInt directly rather than
        // use SetPlayerIntHook; when using the latter, other mods using that hook, such as
        // PlayerDataTracker, will inadvertently overwrite our changes if their hook runs after ours,
        // since they only see the value the game originally tried to set and SetPlayerIntHook
        // requires the hook to write the new value itself even if it doesn't want to override it.
        private void FixGrimmkinUpgradeCost(On.PlayerData.orig_SetInt orig, PlayerData pd, string intName, int newValue)
        {
            if (Settings.RandomizeGrimmkinFlames && intName == "flamesCollected" && newValue == 0)
            {
                // We can still get the original value here, since we haven't called orig yet.
                newValue = pd.GetIntInternal(intName) - 3;
            }
            orig(pd, intName, newValue);
        }

        private void FixVoidHeart(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            // Normal shade and sibling AI
            if ((self.FsmName == "Control" && self.gameObject.name.StartsWith("Shade Sibling")) || (self.FsmName == "Shade Control" && self.gameObject.name.StartsWith("Hollow Shade")))
            {
                self.FsmVariables.FindFsmBool("Friendly").Value = false;
                self.GetState("Pause").ClearTransitions();
                self.GetState("Pause").AddTransition("FINISHED", "Init");
            }
            // Make Void Heart equippable
            else if (self.FsmName == "UI Charms" && self.gameObject.name == "Charms")
            {
                self.GetState("Equipped?").RemoveTransitionsTo("Black Charm? 2");
                self.GetState("Equipped?").AddTransition("EQUIPPED", "Return Points");
                self.GetState("Set Current Item Num").RemoveTransitionsTo("Black Charm?");
                self.GetState("Set Current Item Num").AddTransition("FINISHED", "Return Points");
            }
        }

        // Make Fury work properly when the player only has 1 max mask due to
        // Cursed Masks.
        // The game doesn't really expect the player to ever have Fury active when
        // loading in or sitting at a bench.
        private void FixFury(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
        {
            orig(self);
            if (self.FsmName == "Fury" && self.gameObject.name == "Charm Effects")
            {
                var init = self.GetState("Init");
                init.ClearTransitions();
                init.AddTransition("FINISHED", "Check HP");

                // Make Fury activate when equipped at 1 health
                // (The game broadcasts an UPDATE BLUE HEALTH event whenever the player
                // equips or unequips a charm)
                self.GetState("Idle").AddTransition("UPDATE BLUE HEALTH", "Check HP");

                // Make Fury deactivate when unequipped at 1 health
                var checkEquipped = new FsmState(init) { Name = "Check Equipped" };
                checkEquipped.ClearTransitions();
                checkEquipped.AddTransition("EQUIPPED", "Stay Furied");
                checkEquipped.AddTransition("NOT EQUIPPED", "Deactivate");
                checkEquipped.Actions = new FsmStateAction[] {
                    new RandomizerExecuteLambda(() => self.SendEvent(Ref.PD.equippedCharm_6 ? "EQUIPPED" : "NOT EQUIPPED"))
                };
                self.AddState(checkEquipped);

                void PatchRecheck(FsmState s)
                {
                    s.RemoveTransitionsTo("Deactivate");
                    s.AddTransition("HERO HEALED FULL", "Recheck");
                    // This is an original transition that we don't want to change
                    s.AddTransition("ALL CHARMS END", "Deactivate");
                    s.AddTransition("UPDATE BLUE HEALTH", checkEquipped.Name);
                }

                PatchRecheck(self.GetState("Activate"));
                PatchRecheck(self.GetState("Stay Furied"));
            }
        }

        // Will be moved out of RandomizerMod in the future

        public string LastRandomizedEntrance = null;
        public string LastRandomizedExit = null;

        private static void EditTransition(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
        {
            if (PlayerData.instance.bossRushMode && info.SceneName == "GG_Entrance_Cutscene")
            {
                StartSaveChanges.StartDataChanges();
                info.SceneName = PlayerData.instance.respawnScene;
                SceneEditor.ApplySaveDataChanges(info.SceneName, info.EntryGateName ?? string.Empty);
                orig(self, info);
                return;
            }
            if (string.IsNullOrEmpty(info.EntryGateName) || string.IsNullOrEmpty(info.SceneName))
            {
                orig(self, info);
                return;
            }

            // Reroute Jiji to Jinn
            if (self.sceneName == SceneNames.Room_Ouiji && info.SceneName == SceneNames.Town)
            {
                info.SceneName = SceneNames.Room_Jinn;
                info.EntryGateName = "left1";
                orig(self, info);
                return;
            }

            if (RandomizerMod.Instance.Settings.RandomizeTransitions)
            {
                TransitionPoint tp = Object.FindObjectsOfType<TransitionPoint>().FirstOrDefault(x => x.entryPoint == info.EntryGateName && x.targetScene == info.SceneName);
                string transitionName = string.Empty;

                if (tp == null)
                {
                    if (self.sceneName == SceneNames.Fungus3_44 && info.EntryGateName == "left1") transitionName = self.sceneName + "[door1]";
                    else if (self.sceneName == SceneNames.Crossroads_02 && info.EntryGateName == "left1") transitionName = self.sceneName + "[door1]";
                    else if (self.sceneName == SceneNames.Crossroads_06 && info.EntryGateName == "left1") transitionName = self.sceneName + "[door1]";
                    else if (self.sceneName == SceneNames.Deepnest_10 && info.EntryGateName == "left1") transitionName = self.sceneName + "[door1]";
                    else if (self.sceneName == SceneNames.Town && info.SceneName == SceneNames.Room_shop) transitionName = self.sceneName + "[door_sly]";
                    else if (self.sceneName == SceneNames.Town && info.SceneName == SceneNames.Room_Town_Stag_Station) transitionName = self.sceneName + "[door_station]";
                    else if (self.sceneName == SceneNames.Town && info.SceneName == SceneNames.Room_Bretta) transitionName = self.sceneName + "[door_bretta]";
                    else if (self.sceneName == SceneNames.Crossroads_04 && info.SceneName == SceneNames.Room_Charm_Shop) transitionName = self.sceneName + "[door_charmshop]";
                    else if (self.sceneName == SceneNames.Crossroads_04 && info.SceneName == SceneNames.Room_Mender_House) transitionName = self.sceneName + "[door_Mender_House]";
                    else if (self.sceneName == SceneNames.Ruins1_04 && info.SceneName == SceneNames.Room_nailsmith) transitionName = self.sceneName + "[door1]";
                    else if (self.sceneName == SceneNames.Fungus3_48 && info.SceneName == SceneNames.Room_Queen) transitionName = self.sceneName + "[door1]";
                    else
                    {
                        orig(self, info);
                        return;
                    }
                }
                else
                {
                    string name = tp.name.Split(null).First(); // some transitions have duplicates named left1 (1) and so on

                    if (RandomizerMod.Instance.Settings.RandomizeRooms)
                    {
                        // It's simplest to treat the three transitions connecting Mantis Lords and Mantis Village as one
                        if (self.sceneName == SceneNames.Fungus2_14 && name.StartsWith("bot")) name = "bot3";
                        else if (self.sceneName == SceneNames.Fungus2_15 && name.StartsWith("top")) name = "top3";
                    }

                    transitionName = self.sceneName + "[" + name + "]";
                }

                // Use Jinn in place of Jiji for the randomized exit
                if (transitionName == "Room_Jinn[left1]") transitionName = "Room_Ouiji[left1]";

                if (Instance.Settings._transitionPlacements.TryGetValue(transitionName, out string destination))
                {
                    Instance.LastRandomizedEntrance = transitionName;
                    Instance.LastRandomizedExit = destination;

                    try
                    {
                        if (!RandomizerMod.Instance.Settings.CheckTransitionFound(transitionName))
                        {
                            RandomizerMod.Instance.Settings.MarkTransitionFound(transitionName);
                            RandomizerMod.Instance.Settings.MarkTransitionFound(destination);
                            RandoLogger.LogTransitionToTracker(transitionName, destination);
                            // moved UpdateHelperLog to SceneEditor, so it accesses new scene name
                        }
                    }
                    catch (Exception e)
                    {
                        RandomizerMod.Instance.LogError("Error in logging new transition: " + transitionName + "\n" + e);
                    }
                    info.SceneName = LogicManager.GetTransitionDef(destination).sceneName.Split('-').First();
                    info.EntryGateName = LogicManager.GetTransitionDef(destination).doorName;
                }
            }
            SceneEditor.ApplySaveDataChanges(info.SceneName, info.EntryGateName);
            orig(self, info);
        }


        private void OnMainMenu(Scene from, Scene to)
        {
            if (Ref.GM.GetSceneNameString() != SceneNames.Menu_Title) return;
            // Reset on menu load
            Settings = new SaveSettings();
            RandomizerAction.ClearActions();
            UnhookRandomizer();

            try
            {
                MenuChanger.EditUI();
            }
            catch (Exception e)
            {
                LogError("Error editing menu:\n" + e);
            }
        }

        
        private void HandleSceneChanges(Scene from, Scene to)
        {
            if (Ref.GM.IsGameplayScene())
            {
                try
                {
                    // In rare cases, this is called before the previous scene has unloaded
                    // Deleting old randomizer shinies to prevent issues
                    foreach (GameObject g in GameObject.FindObjectsOfType<GameObject>())
                    {
                        if (g.name.Contains("Randomizer Shiny"))
                        {
                            Object.DestroyImmediate(g);
                        }
                    }

                    RandomizerAction.EditShinies();
                }
                catch (Exception e)
                {
                    LogError($"Error applying RandomizerActions to scene {to.name}:\n" + e);
                }
            }

            try
            {
                RestrictionManager.SceneChanged(to);
                SceneEditor.SceneChanged(to);
                StartSaveChanges.StartSceneChanges(to);
            }
            catch (Exception e)
            {
                LogError($"Error applying changes to scene {to.name}:\n" + e);
            }
        }
    }
}
