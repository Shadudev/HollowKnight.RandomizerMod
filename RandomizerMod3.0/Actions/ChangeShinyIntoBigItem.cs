using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using RandomizerMod.Components;
using RandomizerMod.FsmStateActions;
using SeanprCore;
using UnityEngine;
using static RandomizerMod.GiveItemActions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RandomizerMod.Actions
{
    public struct BigItemDef
    {
        public string Name;
        public string BoolName;
        public string SpriteKey;
        public string TakeKey;
        public string NameKey;
        public string ButtonKey;
        public string DescOneKey;
        public string DescTwoKey;
    }


    public class ChangeShinyIntoBigItem : RandomizerAction
    {
        private readonly string _sceneName;
        private readonly string _objectName;
        private readonly string _fsmName;

        private readonly BigItemDef[] _itemDefs;

        private readonly GiveAction _action;        
        private readonly string _item;
        private readonly string _location;

        // BigItemDef array is meant to be for additive items
        // For example, items[0] could be vengeful spirit and items[1] would be shade soul
        public ChangeShinyIntoBigItem(string sceneName, string objectName, string fsmName, BigItemDef[] items, GiveAction action,
            string item, string location)
        {
            _sceneName = sceneName;
            _objectName = objectName;
            _fsmName = fsmName;
            
            _itemDefs = items;

            _action = action;
            _item = item;
            _location = location;
        }

        public override ActionType Type => ActionType.PlayMakerFSM;

        public override void Process(string scene, Object changeObj)
        {
            if (scene != _sceneName || !(changeObj is PlayMakerFSM fsm) || fsm.FsmName != _fsmName ||
                fsm.gameObject.name != _objectName)
            {
                return;
            }

            FsmState pdBool = fsm.GetState("PD Bool?");
            FsmState charm = fsm.GetState("Charm?");
            FsmState bigItem = fsm.GetState("Big Item?");
            FsmState bigGetFlash = fsm.GetState("Big Get Flash");
            FsmState trinkFlash = fsm.GetState("Trink Flash");
            FsmState giveTrinket = fsm.GetState("Store Key");

            // Remove actions that stop shiny from spawning
            pdBool.RemoveActionsOfType<StringCompare>();

            // Change pd bool test to our new bool
            pdBool.RemoveActionsOfType<PlayerDataBoolTest>();
            pdBool.AddAction(
                new RandomizerExecuteLambda(() => fsm.SendEvent(
                    RandomizerMod.Instance.Settings.CheckLocationFound(_location) ? "COLLECTED" : null
                    )));

            
            // Charm must be preserved as the entry point for AddYNDialogueToShiny
            charm.ClearTransitions();
            charm.AddTransition("FINISHED", "Big Item?");

            // Check if each additive item has already been obtained. Give 300 geo instead of popup if so.
            bigItem.ClearTransitions();
            bigItem.AddFirstAction(new RandomizerExecuteLambda(() => bigItem.AddTransition("FINISHED", BigItemPopup.AdditiveMaxedOut(_itemDefs) ? "Trink Flash" : "Big Get Flash"))); // if we have duplicates, the last item is not a big popup

            // give 300 geo for last duplicate
            trinkFlash.ClearTransitions();
            trinkFlash.AddTransition("FINISHED", "Store Key");
            fsm.GetState("Trinket Type").ClearTransitions();
            trinkFlash.AddTransition("FINISHED", "Store Key");
            giveTrinket.RemoveActionsOfType<SetPlayerDataBool>();
            giveTrinket.AddAction(new RandomizerExecuteLambda(() => GiveItem(GiveItemActions.GiveAction.AddGeo, _item, _location, 300)));
            giveTrinket.GetActionsOfType<GetLanguageString>().First().convName = _itemDefs.Last().NameKey;
            giveTrinket.GetActionsOfType<SetSpriteRendererSprite>().First().sprite = RandomizerMod.GetSprite(Randomization.LogicManager.GetItemDef(_itemDefs.Last().Name).shopSpriteKey);

            // Normal path for big items. Set bool and show the popup after the flash
            bigGetFlash.AddAction(new RandomizerCallStaticMethod(
                typeof(BigItemPopup),
                nameof(BigItemPopup.ShowAdditive),
                _itemDefs,
                fsm.gameObject,
                "GET ITEM MSG END"));

            // Don't actually need to set the skill here, that happens in BigItemPopup
            // Maybe change that at some point, it's not where it should happen
            bigGetFlash.AddAction(new RandomizerExecuteLambda(() => GiveItem(_action, _item, _location)));

            // Exit the fsm after the popup
            bigGetFlash.ClearTransitions();
            bigGetFlash.AddTransition("GET ITEM MSG END", "Hero Up");
            bigGetFlash.AddTransition("HERO DAMAGED", "Finish");
        }
    }
}
