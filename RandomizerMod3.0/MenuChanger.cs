using RandomizerMod.Extensions;
using SereCore;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static RandomizerMod.LogHelper;
using Object = UnityEngine.Object;
using Random = System.Random;
using RandomizerMod.Randomization;
using RandomizerMod.MultiWorld;

namespace RandomizerMod
{
    internal static class MenuChanger
    {
        private static MenuButton startRandoBtn;
        private static MenuButton back;

        public static void EditUI()
        {
            // Reset settings
            RandomizerMod.Instance.Settings = new SaveSettings();
            PostRandomizer.InitializeTasks();

            // Fetch data from vanilla screen
            MenuScreen playScreen = Ref.UI.playModeMenuScreen;

            playScreen.title.gameObject.transform.localPosition = new Vector3(0, 520.56f);

            Object.Destroy(playScreen.topFleur.gameObject);

            MenuButton classic = (MenuButton)playScreen.defaultHighlight;
            MenuButton steel = (MenuButton)classic.FindSelectableOnDown();
            back = (MenuButton)steel.FindSelectableOnDown();

            GameObject parent = steel.transform.parent.gameObject;

            Object.Destroy(parent.GetComponent<VerticalLayoutGroup>());

            // Create new buttons
            startRandoBtn = classic.Clone("StartRando", MenuButton.MenuButtonType.Proceed,
                new Vector2(0, 0), "Start Game", "Randomizer", RandomizerMod.GetSprite("UI.logo"));

            /*
            MenuButton startNormalBtn = classic.Clone("StartNormal", MenuButton.MenuButtonType.Proceed,
                new Vector2(0, -200), "Start Game", "Non-Randomizer");
            MenuButton startSteelRandoBtn = steel.Clone("StartSteelRando", MenuButton.MenuButtonType.Proceed,
                new Vector2(10000, 10000), "Steel Soul", "Randomizer", RandomizerMod.GetSprite("UI.logo2"));
            MenuButton startSteelNormalBtn = steel.Clone("StartSteelNormal", MenuButton.MenuButtonType.Proceed,
                new Vector2(10000, 10000), "Steel Soul", "Non-Randomizer");
                
            startNormalBtn.transform.localScale = 
                startSteelNormalBtn.transform.localScale =
                    startSteelRandoBtn.transform.localScale = */
            startRandoBtn.transform.localScale = new Vector2(0.75f, 0.75f);
            startRandoBtn.GetComponent<StartGameEventTrigger>().bossRush = true;
            MenuButton backBtn = back.Clone("Back", MenuButton.MenuButtonType.Proceed, new Vector2(0, -100), "Back");


            //RandoMenuItem<string> gameTypeBtn = new RandoMenuItem<string>(back, new Vector2(0, 600), "Game Type", "Normal", "Steel Soul");

            float leftColumn = 700f;
            float rightColumn = 1100f;
            float centerColumn = (leftColumn + rightColumn) / 2;
            float vspace = 60;

            float y = 1290;
            CreateLabel(back, new Vector2(900, 1290), "Item Randomization");
            y -= 90f;
            RandoMenuItem<string> presetPoolsBtn = new RandoMenuItem<string>(back, new Vector2(centerColumn, y), "Preset", "Standard", "Super", "LifeTotems", "Spoiler DAB", "EVERYTHING", "Vanilla", "Custom");
            y -= vspace;
            RandoMenuItem<bool> RandoDreamersBtn = new RandoMenuItem<bool>(back, new Vector2(leftColumn, y), "Dreamers", true, false);
            RandoMenuItem<bool> RandoSkillsBtn = new RandoMenuItem<bool>(back, new Vector2(rightColumn, y), "Skills", true, false);
            y -= vspace;
            RandoMenuItem<bool> RandoCharmsBtn = new RandoMenuItem<bool>(back, new Vector2(leftColumn, y), "Charms", true, false);
            RandoMenuItem<bool> RandoKeysBtn = new RandoMenuItem<bool>(back, new Vector2(rightColumn, y), "Keys", true, false);
            y -= vspace;
            RandoMenuItem<bool> DuplicateBtn = new RandoMenuItem<bool>(back, new Vector2(centerColumn, y), "Duplicate Major Items", true, false);
            y -= vspace;
            RandoMenuItem<bool> RandoMaskBtn = new RandoMenuItem<bool>(back, new Vector2(leftColumn, y), "Mask Shards", true, false);
            RandoMenuItem<bool> RandoVesselBtn = new RandoMenuItem<bool>(back, new Vector2(rightColumn, y), "Vessel Fragments", true, false);
            y -= vspace;
            RandoMenuItem<bool> RandoOreBtn = new RandoMenuItem<bool>(back, new Vector2(leftColumn, y), "Pale Ore", true, false);
            RandoMenuItem<bool> RandoNotchBtn = new RandoMenuItem<bool>(back, new Vector2(rightColumn, y), "Charm Notches", true, false);
            y -= vspace;
            RandoMenuItem<bool> RandoGeoChestsBtn = new RandoMenuItem<bool>(back, new Vector2(leftColumn, y), "Geo Chests", true, false);
            RandoMenuItem<bool> RandoRelicsBtn = new RandoMenuItem<bool>(back, new Vector2(rightColumn, y), "Relics", true, false);
            y -= vspace;
            RandoMenuItem<bool> RandoEggBtn = new RandoMenuItem<bool>(back, new Vector2(leftColumn, y), "Rancid Eggs", true, false);
            RandoMenuItem<bool> RandoStagBtn = new RandoMenuItem<bool>(back, new Vector2(rightColumn, y), "Stags", true, false);
            y -= vspace;
            RandoMenuItem<bool> RandoMapBtn = new RandoMenuItem<bool>(back, new Vector2(leftColumn, y), "Maps", false, true);
            RandoMenuItem<bool> RandoRootsBtn = new RandoMenuItem<bool>(back, new Vector2(rightColumn, y), "Whispering Roots", false, true);
            y -= vspace;
            RandoMenuItem<bool> RandoGrubBtn = new RandoMenuItem<bool>(back, new Vector2(leftColumn, y), "Grubs", false, true);
            RandoMenuItem<bool> RandoCocoonsBtn = new RandoMenuItem<bool>(back, new Vector2(rightColumn, y), "Lifeblood Cocoons", false, true);
            y -= vspace;
            RandoMenuItem<bool> RandoSoulTotemsBtn = new RandoMenuItem<bool>(back, new Vector2(leftColumn, y), "Soul Totems", false, true);
            RandoMenuItem<bool> RandoLoreTabletsBtn = new RandoMenuItem<bool>(back, new Vector2(rightColumn, y), "Lore Tablets", false, true);
            y -= vspace;
            RandoMenuItem<bool> RandoPalaceBtn = new RandoMenuItem<bool>(back, new Vector2(centerColumn, y), "Palace Totems/Tablets", false, true);
            y -= vspace;
            RandoMenuItem<bool> RandoFlamesBtn = new RandoMenuItem<bool>(back, new Vector2(leftColumn, y), "Grimmkin Flames", false, true);
            RandoMenuItem<bool> RandoGeoRocksBtn = new RandoMenuItem<bool>(back, new Vector2(rightColumn, y), "Geo Rocks", false, true);
            y -= vspace;
            RandoMenuItem<bool> RandoBossEssenceBtn = new RandoMenuItem<bool>(back, new Vector2(leftColumn, y), "Boss Essence", false, true);
            RandoMenuItem<bool> RandoBossGeoBtn = new RandoMenuItem<bool>(back, new Vector2(rightColumn, y), "Boss Geo", false, true);
            y -= vspace;
            RandoMenuItem<bool> splitCloakBtn = new RandoMenuItem<bool>(back, new Vector2(leftColumn, y), "Split Cloak", false, true);
            RandoMenuItem<bool> splitClawBtn = new RandoMenuItem<bool>(back, new Vector2(rightColumn, y), "Split Claw", false, true);
            y -= 90;
            RandoMenuItem<bool> RandoNotchCostBtn = new RandoMenuItem<bool>(back, new Vector2(centerColumn, y), "Randomize Notch Costs", true, false);

            RandoMenuItem<bool> RandoStartItemsBtn = new RandoMenuItem<bool>(back, new Vector2(900, 80), "Randomize Start Items", false, true);
            RandoMenuItem<string> RandoStartLocationsModeBtn = new RandoMenuItem<string>(back, new Vector2(900, 0), "Start Location Setting", "Select", "Random");
            RandoMenuItem<string> StartLocationsListBtn = new RandoMenuItem<string>(back, new Vector2(900, -80), "Start Location", LogicManager.StartLocations);

            y = 1290f;
            CreateLabel(back, new Vector2(-900, y), "Required Skips");
            y -= 90f;
            RandoMenuItem<string> presetSkipsBtn = new RandoMenuItem<string>(back, new Vector2(-900, y), "Preset", "Easy", "Medium", "Hard", "Custom");
            y -= vspace;
            RandoMenuItem<bool> mildSkipsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Mild Skips", false, true);
            y -= vspace;
            RandoMenuItem<bool> shadeSkipsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Shade Skips", false, true);
            y -= vspace;
            RandoMenuItem<bool> fireballSkipsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Fireball Skips", false, true);
            y -= vspace;
            RandoMenuItem<bool> acidSkipsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Acid Skips", false, true);
            y -= vspace;
            RandoMenuItem<bool> spikeTunnelsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Spike Tunnels", false, true);
            y -= vspace;
            RandoMenuItem<bool> darkRoomsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Dark Rooms", false, true);
            y -= vspace;
            RandoMenuItem<bool> spicySkipsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Spicy Skips", false, true);
            y -= 90f;
            CreateLabel(back, new Vector2(-900, y), "Quality of Life");
            y -= 90f;
            /*
            RandoMenuItem<bool> charmNotchBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Salubra Notches", true, false);
            y -= vspace;
            */
            RandoMenuItem<bool> preloadsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Reduce Preloads", true, false);
            preloadsBtn.SetSelection(RandomizerMod.Instance.globalSettings.ReducePreloads);
            y -= vspace;
            RandoMenuItem<bool> EarlyGeoBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Early Geo", true, false);
            y -= vspace;
            RandoMenuItem<bool> softlockBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Extra Platforms", true, false);
            y -= vspace;
            RandoMenuItem<bool> recentItemsBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Show Recent Items", true, false);
            recentItemsBtn.SetSelection(RandomizerMod.Instance.globalSettings.RecentItems);
            y -= vspace;
            RandoMenuItem<bool> npcBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "NPC Item Dialogue", true, false);
            npcBtn.SetSelection(RandomizerMod.Instance.globalSettings.NPCItemDialogue);
            /*
            y -= vspace;
            RandoMenuItem<bool> jijiBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Jiji Hints", false, true);
            */
            y -= 90f;
            CreateLabel(back, new Vector2(-900, y), "Restrictions");
            y -= 90f;
            RandoMenuItem<bool> RandoFocusBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Randomize Focus", false, true);
            y -= vspace;
            RandoMenuItem<bool> RandoSwimBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Randomize Swim", true, false);
            y -= vspace;
            RandoMenuItem<bool> cursedNailBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Cursed Nail", false, true);
            y -= vspace;
            RandoMenuItem<bool> cursedNotchesBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Cursed Notches", false, true);
            y -= vspace;
            RandoMenuItem<bool> cursedMasksBtn = new RandoMenuItem<bool>(back, new Vector2(-900, y), "Cursed Masks", false, true);

            RandoMenuItem<string> modeBtn = new RandoMenuItem<string>(back, new Vector2(0, 1040), "Mode", "Item Randomizer", "Item + Area Randomizer", "Item + Connected-Area Room Randomizer", "Item + Room Randomizer");
            RandoMenuItem<string> cursedBtn = new RandoMenuItem<string>(back, new Vector2(0, 960), "Cursed", "no", "noo", "noooo", "noooooooo", "noooooooooooooooo", "Oh yeah");
            
            RandoMenuItem<bool> RandoSpoilerBtn = new RandoMenuItem<bool>(back, new Vector2(0, 0), "Create Spoiler Log", true, false);

            // Create seed entry field
            GameObject seedGameObject = back.Clone("Seed", MenuButton.MenuButtonType.Activate, new Vector2(0, 1130),
                "Click to type a custom seed").gameObject;
            Object.DestroyImmediate(seedGameObject.GetComponent<MenuButton>());
            Object.DestroyImmediate(seedGameObject.GetComponent<EventTrigger>());
            Object.DestroyImmediate(seedGameObject.transform.Find("Text").GetComponent<AutoLocalizeTextUI>());
            Object.DestroyImmediate(seedGameObject.transform.Find("Text").GetComponent<FixVerticalAlign>());
            Object.DestroyImmediate(seedGameObject.transform.Find("Text").GetComponent<ContentSizeFitter>());

            RectTransform seedRect = seedGameObject.transform.Find("Text").GetComponent<RectTransform>();
            seedRect.anchorMin = seedRect.anchorMax = new Vector2(0.5f, 0.5f);
            seedRect.sizeDelta = new Vector2(337, 63.2f);

            InputField customSeedInput = seedGameObject.AddComponent<InputField>();
            customSeedInput.transform.localPosition = new Vector3(0, 1240);
            customSeedInput.textComponent = seedGameObject.transform.Find("Text").GetComponent<Text>();

            RandomizerMod.Instance.Settings.Seed = new Random().Next(999999999);
            customSeedInput.text = RandomizerMod.Instance.Settings.Seed.ToString();

            customSeedInput.caretColor = Color.white;
            customSeedInput.contentType = InputField.ContentType.IntegerNumber;
            customSeedInput.onEndEdit.AddListener(ParseSeedInput);
            customSeedInput.navigation = Navigation.defaultNavigation;
            customSeedInput.caretWidth = 8;
            customSeedInput.characterLimit = 9;

            customSeedInput.colors = new ColorBlock
            {
                highlightedColor = Color.yellow,
                pressedColor = Color.red,
                disabledColor = Color.black,
                normalColor = Color.white,
                colorMultiplier = 2f
            };

            // Create some labels
            CreateLabel(back, new Vector2(900, 160), "Start Settings");
            CreateLabel(back, new Vector2(0, 200), "Use of Benchwarp mod may be required");
            CreateLabel(back, new Vector2(0, 1300), "Seed:");
            
            // We don't need these old buttons anymore
            Object.Destroy(classic.gameObject);
            Object.Destroy(steel.gameObject);
            Object.Destroy(parent.FindGameObjectInChildren("GGButton"));
            back.gameObject.SetActive(false);

            // Gotta put something here, we destroyed the old default
            playScreen.defaultHighlight = startRandoBtn;

            // Apply navigation info (up, right, down, left)
            //startNormalBtn.SetNavigation(gameTypeBtn.Button, presetPoolsBtn.Button, backBtn, presetSkipsBtn.Button);
            startRandoBtn.SetNavigation(modeBtn.Button, presetPoolsBtn.Button, backBtn, presetSkipsBtn.Button);
            //startSteelNormalBtn.SetNavigation(gameTypeBtn.Button, presetPoolsBtn.Button, backBtn, presetSkipsBtn.Button);
            //startSteelRandoBtn.SetNavigation(modeBtn.Button, presetPoolsBtn.Button, gameTypeBtn.Button, presetSkipsBtn.Button);
            modeBtn.Button.SetNavigation(backBtn, modeBtn.Button, startRandoBtn, modeBtn.Button);
            //gameTypeBtn.Button.SetNavigation(startRandoBtn, presetPoolsBtn.Button, startRandoBtn, presetSkipsBtn.Button);
            backBtn.SetNavigation(startRandoBtn, backBtn, modeBtn.Button, backBtn);
            RandoSpoilerBtn.Button.SetNavigation(RandoRelicsBtn.Button, RandoSpoilerBtn.Button, presetPoolsBtn.Button, startRandoBtn);

            presetSkipsBtn.Button.SetNavigation(cursedMasksBtn.Button, startRandoBtn, shadeSkipsBtn.Button, presetSkipsBtn.Button);
            mildSkipsBtn.Button.SetNavigation(presetSkipsBtn.Button, startRandoBtn, mildSkipsBtn.Button, mildSkipsBtn.Button);
            shadeSkipsBtn.Button.SetNavigation(mildSkipsBtn.Button, startRandoBtn, fireballSkipsBtn.Button, shadeSkipsBtn.Button);
            fireballSkipsBtn.Button.SetNavigation(shadeSkipsBtn.Button, startRandoBtn, acidSkipsBtn.Button, fireballSkipsBtn.Button);
            acidSkipsBtn.Button.SetNavigation(fireballSkipsBtn.Button, startRandoBtn, spikeTunnelsBtn.Button, acidSkipsBtn.Button);
            spikeTunnelsBtn.Button.SetNavigation(acidSkipsBtn.Button, startRandoBtn, darkRoomsBtn.Button, spikeTunnelsBtn.Button);
            darkRoomsBtn.Button.SetNavigation(spikeTunnelsBtn.Button, startRandoBtn, spicySkipsBtn.Button, darkRoomsBtn.Button);
            spicySkipsBtn.Button.SetNavigation(darkRoomsBtn.Button, startRandoBtn, preloadsBtn.Button, spicySkipsBtn.Button);

            //charmNotchBtn.Button.SetNavigation(spicySkipsBtn.Button, startRandoBtn, preloadsBtn.Button, charmNotchBtn.Button);
            preloadsBtn.Button.SetNavigation(spicySkipsBtn.Button, startRandoBtn, EarlyGeoBtn.Button, preloadsBtn.Button);
            EarlyGeoBtn.Button.SetNavigation(preloadsBtn.Button, startRandoBtn, softlockBtn.Button, EarlyGeoBtn.Button);
            softlockBtn.Button.SetNavigation(EarlyGeoBtn.Button, startRandoBtn, npcBtn.Button, softlockBtn.Button);
            npcBtn.Button.SetNavigation(softlockBtn.Button, startRandoBtn, RandoFocusBtn.Button, npcBtn.Button);
            //jijiBtn.Button.SetNavigation(npcBtn.Button, startRandoBtn, presetSkipsBtn.Button, jijiBtn.Button);

            RandoFocusBtn.Button.SetNavigation(npcBtn.Button, startRandoBtn, RandoSwimBtn.Button, RandoFocusBtn.Button);
            RandoSwimBtn.Button.SetNavigation(RandoFocusBtn.Button, startRandoBtn, cursedNailBtn.Button, RandoSwimBtn.Button);
            cursedNailBtn.Button.SetNavigation(RandoSwimBtn.Button, startRandoBtn, cursedNotchesBtn.Button, cursedNailBtn.Button);
            cursedNotchesBtn.Button.SetNavigation(cursedNailBtn.Button, startRandoBtn, cursedMasksBtn.Button, cursedNotchesBtn.Button);
            cursedMasksBtn.Button.SetNavigation(cursedNotchesBtn.Button, startRandoBtn, presetSkipsBtn.Button, cursedMasksBtn.Button);


            presetPoolsBtn.Button.SetNavigation(RandoSpoilerBtn.Button, presetPoolsBtn.Button, RandoDreamersBtn.Button, startRandoBtn);
            RandoDreamersBtn.Button.SetNavigation(presetPoolsBtn.Button, RandoSkillsBtn.Button, RandoCharmsBtn.Button, startRandoBtn);
            RandoSkillsBtn.Button.SetNavigation(presetPoolsBtn.Button, RandoSkillsBtn.Button, RandoKeysBtn.Button, RandoDreamersBtn.Button);
            RandoCharmsBtn.Button.SetNavigation(RandoDreamersBtn.Button, RandoKeysBtn.Button, DuplicateBtn.Button, startRandoBtn);
            RandoKeysBtn.Button.SetNavigation(RandoSkillsBtn.Button, RandoKeysBtn.Button, DuplicateBtn.Button, RandoCharmsBtn.Button);
            DuplicateBtn.Button.SetNavigation(RandoCharmsBtn.Button, DuplicateBtn.Button, RandoMaskBtn.Button, startRandoBtn);
            RandoMaskBtn.Button.SetNavigation(DuplicateBtn.Button, RandoVesselBtn.Button, RandoOreBtn.Button, startRandoBtn);
            RandoVesselBtn.Button.SetNavigation(DuplicateBtn.Button, RandoVesselBtn.Button, RandoNotchBtn.Button, RandoMaskBtn.Button);
            RandoOreBtn.Button.SetNavigation(RandoMaskBtn.Button, RandoNotchBtn.Button, RandoGeoChestsBtn.Button, startRandoBtn);
            RandoNotchBtn.Button.SetNavigation(RandoVesselBtn.Button, RandoNotchBtn.Button, RandoRelicsBtn.Button, RandoOreBtn.Button);
            RandoGeoChestsBtn.Button.SetNavigation(RandoOreBtn.Button, RandoRelicsBtn.Button, RandoEggBtn.Button, startRandoBtn);
            RandoRelicsBtn.Button.SetNavigation(RandoNotchBtn.Button, RandoRelicsBtn.Button, RandoStagBtn.Button, RandoGeoChestsBtn.Button);
            RandoEggBtn.Button.SetNavigation(RandoGeoChestsBtn.Button, RandoStagBtn.Button, RandoMapBtn.Button, startRandoBtn);
            RandoStagBtn.Button.SetNavigation(RandoRelicsBtn.Button, RandoStagBtn.Button, RandoRootsBtn.Button, RandoEggBtn.Button);
            RandoMapBtn.Button.SetNavigation(RandoEggBtn.Button, RandoRootsBtn.Button, RandoGrubBtn.Button, startRandoBtn);
            RandoRootsBtn.Button.SetNavigation(RandoStagBtn.Button, RandoRootsBtn.Button, RandoCocoonsBtn.Button, RandoMapBtn.Button);
            RandoGrubBtn.Button.SetNavigation(RandoMapBtn.Button, RandoCocoonsBtn.Button, RandoSoulTotemsBtn.Button, startRandoBtn);
            RandoCocoonsBtn.Button.SetNavigation(RandoRootsBtn.Button, RandoCocoonsBtn.Button, RandoLoreTabletsBtn.Button, RandoGrubBtn.Button);
            RandoSoulTotemsBtn.Button.SetNavigation(RandoGrubBtn.Button, RandoLoreTabletsBtn.Button, RandoFlamesBtn.Button, startRandoBtn);
            RandoLoreTabletsBtn.Button.SetNavigation(RandoCocoonsBtn.Button, RandoLoreTabletsBtn.Button, RandoGeoRocksBtn.Button, RandoSoulTotemsBtn.Button);
            RandoFlamesBtn.Button.SetNavigation(RandoSoulTotemsBtn.Button, RandoGeoRocksBtn.Button, RandoBossEssenceBtn.Button, startRandoBtn);
            RandoGeoRocksBtn.Button.SetNavigation(RandoLoreTabletsBtn.Button, RandoGeoRocksBtn.Button, RandoBossGeoBtn.Button, RandoFlamesBtn.Button);
            RandoBossEssenceBtn.Button.SetNavigation(RandoFlamesBtn.Button, RandoBossGeoBtn.Button, RandoStartItemsBtn.Button, RandoBossEssenceBtn.Button);
            RandoBossGeoBtn.Button.SetNavigation(RandoGeoRocksBtn.Button, RandoBossGeoBtn.Button, RandoStartItemsBtn.Button, RandoBossEssenceBtn.Button);
            
            RandoStartItemsBtn.Button.SetNavigation(RandoBossEssenceBtn.Button, RandoStartItemsBtn.Button, RandoStartLocationsModeBtn.Button, startRandoBtn);
            RandoStartLocationsModeBtn.Button.SetNavigation(RandoStartItemsBtn.Button, RandoStartLocationsModeBtn.Button, StartLocationsListBtn.Button, startRandoBtn);
            StartLocationsListBtn.Button.SetNavigation(RandoStartLocationsModeBtn.Button, RandoStartLocationsModeBtn.Button, StartLocationsListBtn.Button, startRandoBtn);

            void UpdateSkipsButtons(RandoMenuItem<string> item)
            {
                switch (item.CurrentSelection)
                {
                    case "Easy":
                        SetShadeSkips(false);
                        mildSkipsBtn.SetSelection(false);
                        acidSkipsBtn.SetSelection(false);
                        spikeTunnelsBtn.SetSelection(false);
                        spicySkipsBtn.SetSelection(false);
                        fireballSkipsBtn.SetSelection(false);
                        darkRoomsBtn.SetSelection(false);
                        break;
                    case "Medium":
                        SetShadeSkips(true);
                        mildSkipsBtn.SetSelection(true);
                        acidSkipsBtn.SetSelection(false);
                        spikeTunnelsBtn.SetSelection(false);
                        spicySkipsBtn.SetSelection(false);
                        fireballSkipsBtn.SetSelection(false);
                        darkRoomsBtn.SetSelection(false);
                        break;
                    case "Hard":
                        SetShadeSkips(true);
                        mildSkipsBtn.SetSelection(true);
                        acidSkipsBtn.SetSelection(true);
                        spikeTunnelsBtn.SetSelection(true);
                        spicySkipsBtn.SetSelection(true);
                        fireballSkipsBtn.SetSelection(true);
                        darkRoomsBtn.SetSelection(true);
                        break;
                    case "Custom":
                        item.SetSelection("Easy");
                        goto case "Easy";

                    default:
                        LogWarn("Unknown value in preset button: " + item.CurrentSelection);
                        break;
                }
            }
            void UpdatePoolPreset(RandoMenuItem<string> item)
            {
                switch (item.CurrentSelection)
                {
                    case "Standard":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(true);
                        RandoMaskBtn.SetSelection(true);
                        RandoVesselBtn.SetSelection(true);
                        RandoOreBtn.SetSelection(true);
                        RandoNotchBtn.SetSelection(true);
                        RandoEggBtn.SetSelection(true);
                        RandoRelicsBtn.SetSelection(true);
                        RandoMapBtn.SetSelection(false);
                        RandoStagBtn.SetSelection(true);
                        RandoGrubBtn.SetSelection(false);
                        RandoRootsBtn.SetSelection(false);
                        RandoGeoRocksBtn.SetSelection(false);
                        RandoCocoonsBtn.SetSelection(false);
                        RandoSoulTotemsBtn.SetSelection(false);
                        RandoPalaceBtn.SetSelection(false);
                        RandoLoreTabletsBtn.SetSelection(false);
                        RandoFlamesBtn.SetSelection(false);
                        RandoBossEssenceBtn.SetSelection(false);
                        RandoBossGeoBtn.SetSelection(false);
                        break;
                    case "Super":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(true);
                        RandoMaskBtn.SetSelection(true);
                        RandoVesselBtn.SetSelection(true);
                        RandoOreBtn.SetSelection(true);
                        RandoNotchBtn.SetSelection(true);
                        RandoEggBtn.SetSelection(true);
                        RandoRelicsBtn.SetSelection(true);
                        RandoMapBtn.SetSelection(true);
                        RandoStagBtn.SetSelection(true);
                        RandoGrubBtn.SetSelection(true);
                        RandoRootsBtn.SetSelection(true);
                        RandoGeoRocksBtn.SetSelection(false);
                        RandoCocoonsBtn.SetSelection(false);
                        RandoSoulTotemsBtn.SetSelection(false);
                        RandoPalaceBtn.SetSelection(false);
                        RandoLoreTabletsBtn.SetSelection(false);
                        RandoFlamesBtn.SetSelection(false);
                        RandoBossEssenceBtn.SetSelection(false);
                        RandoBossGeoBtn.SetSelection(false);
                        break;
                    case "LifeTotems":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(true);
                        RandoMaskBtn.SetSelection(true);
                        RandoVesselBtn.SetSelection(true);
                        RandoOreBtn.SetSelection(true);
                        RandoNotchBtn.SetSelection(true);
                        RandoEggBtn.SetSelection(true);
                        RandoRelicsBtn.SetSelection(true);
                        RandoMapBtn.SetSelection(false);
                        RandoStagBtn.SetSelection(true);
                        RandoGrubBtn.SetSelection(false);
                        RandoRootsBtn.SetSelection(false);
                        RandoGeoRocksBtn.SetSelection(false);
                        RandoCocoonsBtn.SetSelection(true);
                        RandoSoulTotemsBtn.SetSelection(true);
                        RandoPalaceBtn.SetSelection(true);
                        RandoLoreTabletsBtn.SetSelection(false);
                        RandoFlamesBtn.SetSelection(false);
                        RandoBossEssenceBtn.SetSelection(false);
                        RandoBossGeoBtn.SetSelection(true);
                        break;
                    case "Spoiler DAB":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(true);
                        RandoMaskBtn.SetSelection(true);
                        RandoVesselBtn.SetSelection(true);
                        RandoOreBtn.SetSelection(true);
                        RandoNotchBtn.SetSelection(true);
                        RandoEggBtn.SetSelection(true);
                        RandoRelicsBtn.SetSelection(true);
                        RandoMapBtn.SetSelection(true);
                        RandoStagBtn.SetSelection(true);
                        RandoGrubBtn.SetSelection(false);
                        RandoRootsBtn.SetSelection(true);
                        RandoGeoRocksBtn.SetSelection(false);
                        RandoCocoonsBtn.SetSelection(true);
                        RandoSoulTotemsBtn.SetSelection(true);
                        RandoPalaceBtn.SetSelection(false);
                        RandoLoreTabletsBtn.SetSelection(false);
                        RandoFlamesBtn.SetSelection(false);
                        RandoBossEssenceBtn.SetSelection(false);
                        RandoBossGeoBtn.SetSelection(false);
                        break;
                    case "EVERYTHING":
                        RandoDreamersBtn.SetSelection(true);
                        RandoSkillsBtn.SetSelection(true);
                        RandoCharmsBtn.SetSelection(true);
                        RandoKeysBtn.SetSelection(true);
                        RandoGeoChestsBtn.SetSelection(true);
                        RandoMaskBtn.SetSelection(true);
                        RandoVesselBtn.SetSelection(true);
                        RandoOreBtn.SetSelection(true);
                        RandoNotchBtn.SetSelection(true);
                        RandoEggBtn.SetSelection(true);
                        RandoRelicsBtn.SetSelection(true);
                        RandoMapBtn.SetSelection(true);
                        RandoStagBtn.SetSelection(true);
                        RandoGrubBtn.SetSelection(true);
                        RandoRootsBtn.SetSelection(true);
                        RandoGeoRocksBtn.SetSelection(true);
                        RandoCocoonsBtn.SetSelection(true);
                        RandoSoulTotemsBtn.SetSelection(true);
                        RandoPalaceBtn.SetSelection(true);
                        RandoLoreTabletsBtn.SetSelection(true);
                        RandoFlamesBtn.SetSelection(true);
                        RandoBossEssenceBtn.SetSelection(true);
                        RandoBossGeoBtn.SetSelection(true);
                        break;
                    case "Vanilla":
                        RandoDreamersBtn.SetSelection(false);
                        RandoSkillsBtn.SetSelection(false);
                        RandoCharmsBtn.SetSelection(false);
                        RandoKeysBtn.SetSelection(false);
                        RandoGeoChestsBtn.SetSelection(false);
                        RandoMaskBtn.SetSelection(false);
                        RandoVesselBtn.SetSelection(false);
                        RandoOreBtn.SetSelection(false);
                        RandoNotchBtn.SetSelection(false);
                        RandoEggBtn.SetSelection(false);
                        RandoRelicsBtn.SetSelection(false);
                        RandoMapBtn.SetSelection(false);
                        RandoStagBtn.SetSelection(false);
                        RandoGrubBtn.SetSelection(false);
                        RandoRootsBtn.SetSelection(false);
                        RandoGeoRocksBtn.SetSelection(false);
                        RandoCocoonsBtn.SetSelection(false);
                        RandoSoulTotemsBtn.SetSelection(false);
                        RandoPalaceBtn.SetSelection(false);
                        RandoLoreTabletsBtn.SetSelection(false);
                        RandoFlamesBtn.SetSelection(false);
                        RandoBossEssenceBtn.SetSelection(false);
                        RandoBossGeoBtn.SetSelection(false);
                        break;
                    case "Custom":
                        item.SetSelection("Standard");
                        goto case "Standard";
                }
            }

            void HandleProgressionLock()
            {
                if (RandoStartItemsBtn.CurrentSelection)
                {
                    RandoDreamersBtn.SetSelection(true);
                    RandoSkillsBtn.SetSelection(true);
                    RandoCharmsBtn.SetSelection(true);
                    RandoKeysBtn.SetSelection(true);
                    RandoDreamersBtn.Lock();
                    RandoSkillsBtn.Lock();
                    RandoCharmsBtn.Lock();
                    RandoKeysBtn.Lock();
                }
                else
                {
                    RandoDreamersBtn.Unlock();
                    RandoSkillsBtn.Unlock();
                    RandoCharmsBtn.Unlock();
                    RandoKeysBtn.Unlock();
                }
            }
            HandleProgressionLock(); // call it because duplicates are on by default

            void SetShadeSkips(bool enabled)
            {
                if (enabled)
                {
                    //gameTypeBtn.SetSelection("Normal");
                    //SwitchGameType(false);
                }

                shadeSkipsBtn.SetSelection(enabled);
            }

            void SkipsSettingChanged(RandoMenuItem<bool> item)
            {
                presetSkipsBtn.SetSelection("Custom");
            }

            void PoolSettingChanged(RandoMenuItem<bool> item)
            {
                presetPoolsBtn.SetSelection("Custom");
            }

            void RecentItemsSettingChanged(RandoMenuItem<bool> item)
            {
                RandomizerMod.Instance.globalSettings.RecentItems = recentItemsBtn.CurrentSelection;
            }
            void NPCSettingChanged(RandoMenuItem<bool> item)
            {
                RandomizerMod.Instance.globalSettings.NPCItemDialogue = npcBtn.CurrentSelection;
            }

            void PreloadsSettingChanged(RandoMenuItem<bool> item)
            {
                RandomizerMod.Instance.globalSettings.ReducePreloads = preloadsBtn.CurrentSelection;
            }
            modeBtn.Changed += s => HandleProgressionLock();

            presetSkipsBtn.Changed += UpdateSkipsButtons;
            presetPoolsBtn.Changed += UpdatePoolPreset;

            mildSkipsBtn.Changed += SkipsSettingChanged;
            shadeSkipsBtn.Changed += SkipsSettingChanged;
            shadeSkipsBtn.Changed += SaveShadeVal;
            acidSkipsBtn.Changed += SkipsSettingChanged;
            spikeTunnelsBtn.Changed += SkipsSettingChanged;
            spicySkipsBtn.Changed += SkipsSettingChanged;
            fireballSkipsBtn.Changed += SkipsSettingChanged;
            darkRoomsBtn.Changed += SkipsSettingChanged;

            RandoDreamersBtn.Changed += PoolSettingChanged;
            RandoSkillsBtn.Changed += PoolSettingChanged;
            RandoCharmsBtn.Changed += PoolSettingChanged;
            RandoKeysBtn.Changed += PoolSettingChanged;
            RandoGeoChestsBtn.Changed += PoolSettingChanged;
            RandoGeoRocksBtn.Changed += PoolSettingChanged;
            RandoSoulTotemsBtn.Changed += PoolSettingChanged;

            RandoPalaceBtn.Changed += PoolSettingChanged;
            RandoLoreTabletsBtn.Changed += PoolSettingChanged;

            RandoMaskBtn.Changed += PoolSettingChanged;
            RandoVesselBtn.Changed += PoolSettingChanged;
            RandoOreBtn.Changed += PoolSettingChanged;
            RandoNotchBtn.Changed += PoolSettingChanged;
            RandoEggBtn.Changed += PoolSettingChanged;
            RandoRelicsBtn.Changed += PoolSettingChanged;
            RandoStagBtn.Changed += PoolSettingChanged;
            RandoMapBtn.Changed += PoolSettingChanged;
            RandoGrubBtn.Changed += PoolSettingChanged;
            RandoRootsBtn.Changed += PoolSettingChanged;
            RandoCocoonsBtn.Changed += PoolSettingChanged;
            RandoFlamesBtn.Changed += PoolSettingChanged;
            DuplicateBtn.Changed += s => HandleProgressionLock();

            MiniPM pm = new MiniPM();

            void UpdatePM()
            {
                pm.logicFlags["ITEMRANDO"] = modeBtn.CurrentSelection == "Item Randomizer";
                pm.logicFlags["AREARANDO"] = modeBtn.CurrentSelection.EndsWith("Area Randomizer");
                pm.logicFlags["ROOMRANDO"] = modeBtn.CurrentSelection.EndsWith("Room Randomizer");

                pm.logicFlags["MILDSKIPS"] = mildSkipsBtn.CurrentSelection;
                pm.logicFlags["SHADESKIPS"] = shadeSkipsBtn.CurrentSelection;
                pm.logicFlags["ACIDSKIPS"] = acidSkipsBtn.CurrentSelection;
                pm.logicFlags["FIREBALLSKIPS"] = fireballSkipsBtn.CurrentSelection;
                pm.logicFlags["SPIKETUNNELS"] = spikeTunnelsBtn.CurrentSelection;
                pm.logicFlags["DARKROOMS"] = darkRoomsBtn.CurrentSelection;
                pm.logicFlags["SPICYSKIPS"] = spicySkipsBtn.CurrentSelection;

                pm.logicFlags["VERTICAL"] = RandoStartItemsBtn.CurrentSelection;
                pm.logicFlags["SWIM"] = !RandoSwimBtn.CurrentSelection; // represents starting with SWIM
                pm.logicFlags["2MASKS"] = !cursedMasksBtn.CurrentSelection;

                UpdateStartLocationColor();
            }
            UpdatePM();

            modeBtn.Changed += _ => UpdatePM();

            mildSkipsBtn.Changed += _ => UpdatePM();
            shadeSkipsBtn.Changed += _ => UpdatePM();
            acidSkipsBtn.Changed += _ => UpdatePM();
            spikeTunnelsBtn.Changed += _ => UpdatePM();
            fireballSkipsBtn.Changed += _ => UpdatePM();
            darkRoomsBtn.Changed += _ => UpdatePM();
            spicySkipsBtn.Changed += _ => UpdatePM();
            presetSkipsBtn.Changed += _ => UpdatePM();

            RandoStartItemsBtn.Changed += _ => UpdatePM();
            RandoSwimBtn.Changed += _ => UpdatePM();
            cursedMasksBtn.Changed += _ => UpdatePM();


            void UpdateStartLocationColor()
            {
                if (RandoStartLocationsModeBtn.CurrentSelection == "Random")
                {
                    StartLocationsListBtn.SetSelection("King's Pass");
                    StartLocationsListBtn.Lock();
                    StartLocationsListBtn.SetColor(RandoMenuItem<int>.LOCKED_FALSE_COLOR);
                    return;
                }
                else StartLocationsListBtn.Unlock();

                // cf. TestStartLocation in PreRandomizer. Note that color is checked in StartGame to determine if a selected start was valid
                if (LogicManager.GetStartLocation(StartLocationsListBtn.CurrentSelection) is StartDef startDef)
                {
                    if (pm.Evaluate(startDef.logic))
                    {
                        StartLocationsListBtn.SetColor(Color.white);
                    }
                    else
                    {
                        StartLocationsListBtn.SetColor(Color.red);
                    }
                }
            }

            RandoStartItemsBtn.Changed += (RandoMenuItem<bool> Item) => UpdateStartLocationColor();
            RandoStartItemsBtn.Changed += s => HandleProgressionLock();
            RandoStartLocationsModeBtn.Changed += (RandoMenuItem<string> Item) => UpdateStartLocationColor();
            StartLocationsListBtn.Changed += (RandoMenuItem<string> Item) => UpdateStartLocationColor();
            modeBtn.Changed += (RandoMenuItem<string> Item) => UpdateStartLocationColor();
            recentItemsBtn.Changed += RecentItemsSettingChanged;
            npcBtn.Changed += NPCSettingChanged;
            preloadsBtn.Changed += PreloadsSettingChanged;

            // Setup game type button changes
            void SaveShadeVal(RandoMenuItem<bool> item)
            {
                SetShadeSkips(shadeSkipsBtn.CurrentSelection);
            }

            /*void SwitchGameType(bool steelMode)
            {
                if (!steelMode)
                {
                    // Normal mode
                    startRandoBtn.transform.localPosition = new Vector2(0, 200);
                    startNormalBtn.transform.localPosition = new Vector2(0, -200);
                    startSteelRandoBtn.transform.localPosition = new Vector2(10000, 10000);
                    startSteelNormalBtn.transform.localPosition = new Vector2(10000, 10000);

                    //backBtn.SetNavigation(startNormalBtn, startNormalBtn, modeBtn.Button, startRandoBtn);
                   //magolorBtn.Button.SetNavigation(fireballSkipsBtn.Button, gameTypeBtn.Button, startNormalBtn, lemmBtn.Button);
                    //lemmBtn.Button.SetNavigation(charmNotchBtn.Button, shadeSkipsBtn.Button, startRandoBtn, allSkillsBtn.Button);
                }
                else
                {
                    // Steel Soul mode
                    startRandoBtn.transform.localPosition = new Vector2(10000, 10000);
                    startNormalBtn.transform.localPosition = new Vector2(10000, 10000);
                    startSteelRandoBtn.transform.localPosition = new Vector2(0, 200);
                    startSteelNormalBtn.transform.localPosition = new Vector2(0, -200);

                    SetShadeSkips(false);

                    //backBtn.SetNavigation(startSteelNormalBtn, startSteelNormalBtn, modeBtn.Button, startSteelRandoBtn);
                    //magolorBtn.Button.SetNavigation(fireballSkipsBtn.Button, gameTypeBtn.Button, startSteelNormalBtn, lemmBtn.Button);
                    //lemmBtn.Button.SetNavigation(charmNotchBtn.Button, shadeSkipsBtn.Button, startSteelRandoBtn, allSkillsBtn.Button);
                }
            }

            gameTypeBtn.Button.AddEvent(EventTriggerType.Submit,
                garbage => SwitchGameType(gameTypeBtn.CurrentSelection != "Normal"));
                */

            // Setup start game button events
            void StartGame(bool rando)
            {
                RandomizerMod.Instance.Settings.CharmNotch = true;
                RandomizerMod.Instance.Settings.Grubfather = true;
                RandomizerMod.Instance.Settings.JinnSellAll = true;
                RandomizerMod.Instance.Settings.EarlyGeo = EarlyGeoBtn.CurrentSelection;


                if (rando)
                {
                    RandomizerMod.Instance.Settings.Jiji = false;
                    RandomizerMod.Instance.Settings.Quirrel = false;
                    RandomizerMod.Instance.Settings.ItemDepthHints = false;
                    RandomizerMod.Instance.Settings.NPCItemDialogue = npcBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.ExtraPlatforms = softlockBtn.CurrentSelection;

                    RandomizerMod.Instance.Settings.RandomizeDreamers = RandoDreamersBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeSkills = RandoSkillsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeCharms = RandoCharmsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeKeys = RandoKeysBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeGeoChests = RandoGeoChestsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeMaskShards = RandoMaskBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeVesselFragments = RandoVesselBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizePaleOre = RandoOreBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeCharmNotches = RandoNotchBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeRancidEggs = RandoEggBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeRelics = RandoRelicsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeMaps = RandoMapBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeStags = RandoStagBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeGrubs = RandoGrubBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeLifebloodCocoons = RandoCocoonsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeWhisperingRoots = RandoRootsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeRocks = RandoGeoRocksBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeSoulTotems = RandoSoulTotemsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeLoreTablets = RandoLoreTabletsBtn.CurrentSelection;                    
                    RandomizerMod.Instance.Settings.RandomizeGrimmkinFlames = RandoFlamesBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeBossEssence = RandoBossEssenceBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeBossGeo = RandoBossGeoBtn.CurrentSelection;

                    RandomizerMod.Instance.Settings.RandomizePalaceTotems = RandoPalaceBtn.CurrentSelection && 
                        (RandoSoulTotemsBtn.CurrentSelection || !RandoLoreTabletsBtn.CurrentSelection);
                    RandomizerMod.Instance.Settings.RandomizePalaceTablets = RandoPalaceBtn.CurrentSelection &&
                        (!RandoSoulTotemsBtn.CurrentSelection || RandoLoreTabletsBtn.CurrentSelection);

                    RandomizerMod.Instance.Settings.DuplicateMajorItems = DuplicateBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.CreateSpoilerLog = RandoSpoilerBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeNotchCosts = RandoNotchCostBtn.CurrentSelection;

                    RandomizerMod.Instance.Settings.Cursed = cursedBtn.CurrentSelection.StartsWith("O");
                    RandomizerMod.Instance.Settings.RandomizeCloakPieces = splitCloakBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeClawPieces = splitClawBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeFocus = RandoFocusBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeSwim = RandoSwimBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.CursedNail = cursedNailBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.CursedNotches = cursedNotchesBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.CursedMasks = cursedMasksBtn.CurrentSelection;

                    RandomizerMod.Instance.Settings.Randomizer = rando;
                    RandomizerMod.Instance.Settings.RandomizeAreas = modeBtn.CurrentSelection.EndsWith("Area Randomizer");
                    RandomizerMod.Instance.Settings.RandomizeRooms = modeBtn.CurrentSelection.EndsWith("Room Randomizer");
                    RandomizerMod.Instance.Settings.ConnectAreas = modeBtn.CurrentSelection.StartsWith("Item + Connected-Area");

                    RandomizerMod.Instance.Settings.MildSkips = mildSkipsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.ShadeSkips = shadeSkipsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.FireballSkips = fireballSkipsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.AcidSkips = acidSkipsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.SpikeTunnels = spikeTunnelsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.DarkRooms = darkRoomsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.SpicySkips = spicySkipsBtn.CurrentSelection;

                    RandomizerMod.Instance.Settings.RandomizeStartItems = RandoStartItemsBtn.CurrentSelection;
                    RandomizerMod.Instance.Settings.RandomizeStartLocation = RandoStartLocationsModeBtn.CurrentSelection == "Random";
                    RandomizerMod.Instance.Settings.StartName = StartLocationsListBtn.GetColor() == Color.red ? "King's Pass" : StartLocationsListBtn.CurrentSelection;
                }

                RandomizerMod.Instance.StartNewGame();
            }

            //startNormalBtn.AddEvent(EventTriggerType.Submit, garbage => StartGame(false));
            startRandoBtn.AddEvent(EventTriggerType.Submit, garbage => StartGame(true));
            //startSteelNormalBtn.AddEvent(EventTriggerType.Submit, garbage => StartGame(false));
            //startSteelRandoBtn.AddEvent(EventTriggerType.Submit, garbage => StartGame(true));
        }

        public static MultiWorldMenu CreateMultiWorldMenu()
        {
            back.gameObject.SetActive(true);
            MenuButton existingButton = back;
            
            RandoMenuItem<string> multiWorldBtn = new RandoMenuItem<string>(existingButton, new Vector2(0, 800), "Multiworld", "No", "Yes");
            RandoMenuItem<bool> multiWorldReadyBtn = new RandoMenuItem<bool>(existingButton, new Vector2(0, 600), "Ready", false, true);

            InputField createInputObject()
            {
                GameObject gameObject = existingButton.Clone("entry", MenuButton.MenuButtonType.Activate, new Vector2(0, 1130)).gameObject;
                Object.DestroyImmediate(gameObject.GetComponent<MenuButton>());
                Object.DestroyImmediate(gameObject.GetComponent<EventTrigger>());
                Object.DestroyImmediate(gameObject.transform.Find("Text").GetComponent<AutoLocalizeTextUI>());
                Object.DestroyImmediate(gameObject.transform.Find("Text").GetComponent<FixVerticalAlign>());
                Object.DestroyImmediate(gameObject.transform.Find("Text").GetComponent<ContentSizeFitter>());

                RectTransform rect = gameObject.transform.Find("Text").GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(337, 63.2f);

                InputField input = gameObject.AddComponent<InputField>();
                input.textComponent = gameObject.transform.Find("Text").GetComponent<Text>();
                input.colors = new ColorBlock
                {
                    highlightedColor = Color.yellow,
                    pressedColor = Color.red,
                    disabledColor = Color.black,
                    normalColor = Color.white,
                    colorMultiplier = 2f
                };

                return input;
            }

            InputField urlInput = createInputObject();
            urlInput.transform.localPosition = new Vector3(0, 860);
            urlInput.text = "127.0.0.1";
            urlInput.textComponent.fontSize = urlInput.textComponent.fontSize - 5;

            urlInput.caretColor = Color.white;
            urlInput.contentType = InputField.ContentType.Standard;
            urlInput.navigation = Navigation.defaultNavigation;
            urlInput.caretWidth = 8;
            urlInput.characterLimit = 0;

            InputField nicknameInput = createInputObject();
            nicknameInput.transform.localPosition = new Vector3(0, 720);
            nicknameInput.text = "whoAmI";
            nicknameInput.textComponent.fontSize = nicknameInput.textComponent.fontSize - 5;

            nicknameInput.caretColor = Color.white;
            nicknameInput.contentType = InputField.ContentType.Standard;
            
            nicknameInput.navigation = Navigation.defaultNavigation;
            nicknameInput.caretWidth = 8;
            nicknameInput.characterLimit = 15;


            InputField roomInput = createInputObject();
            roomInput.transform.localPosition = new Vector3(0, 660);
            roomInput.text = "";
            roomInput.textComponent.fontSize = roomInput.textComponent.fontSize - 5;

            roomInput.caretColor = Color.white;
            roomInput.contentType = InputField.ContentType.Standard;
            roomInput.navigation = Navigation.defaultNavigation;
            roomInput.caretWidth = 8;
            roomInput.characterLimit = 15;


            GameObject urlLabel = CreateLabel(existingButton, new Vector2(-200, 865), "URL:");
            urlLabel.transform.localScale = new Vector3(0.8f, 0.8f);
            GameObject nicknameLabel = CreateLabel(existingButton, new Vector2(-300, 725), "Nickname:");
            nicknameLabel.transform.localScale = new Vector3(0.8f, 0.8f);
            GameObject roomLabel = CreateLabel(existingButton, new Vector2(-300, 665), "Room:");
            roomLabel.transform.localScale = new Vector3(0.8f, 0.8f);
            GameObject readyPlayers = CreateLabel(existingButton, new Vector2(0, 540), "");
            readyPlayers.transform.localScale = new Vector3(0.5f, 0.5f);

            MenuButton rejoinBtn = existingButton.Clone("Rejoin", MenuButton.MenuButtonType.Proceed, new Vector2(0, 280), "Rejoin");
            rejoinBtn.ClearEvents();

            MenuButton startMultiWorldBtn = startRandoBtn.Clone("StartMultiWorld", MenuButton.MenuButtonType.Proceed,
                new Vector3(0, -160), "Start Game", "MultiWorld", RandomizerMod.GetSprite("UI.logo"));
            startMultiWorldBtn.transform.localScale = new Vector2(0.75f, 0.75f);

            back.gameObject.SetActive(false);
            return new MultiWorldMenu(multiWorldBtn, multiWorldReadyBtn, urlLabel, urlInput, nicknameLabel, nicknameInput, 
                roomLabel, roomInput, readyPlayers, rejoinBtn, startMultiWorldBtn, startRandoBtn);
        }

        private static void ParseSeedInput(string input)
        {
            if (int.TryParse(input, out int newSeed))
            {
                RandomizerMod.Instance.Settings.Seed = newSeed;
            }
            else
            {
                LogWarn($"Seed input \"{input}\" could not be parsed to an integer");
            }
        }

        private static GameObject CreateLabel(MenuButton baseObj, Vector2 position, string text)
        {
            GameObject label = baseObj.Clone(text + "Label", MenuButton.MenuButtonType.Activate, position, text)
                .gameObject;
            Object.Destroy(label.GetComponent<EventTrigger>());
            Object.Destroy(label.GetComponent<MenuButton>());
            return label;
        }
    }
}
