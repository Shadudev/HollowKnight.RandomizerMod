using RandomizerMod.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace RandomizerMod.MultiWorld
{
    public class MultiWorldMenu
    {
        public MultiWorldMenu(RandoMenuItem<string> multiworldBtn, RandoMenuItem<bool> multiworldReadyBtn,
                              GameObject urlLabel, InputField urlInput, GameObject nicknameLabel,
                              InputField nicknameInput, GameObject roomLabel, InputField roomInput,
                              GameObject readyPlayers, MenuButton rejoinBtn, MenuButton startMultiWorldBtn,
                              MenuButton startRandoBtn)
        {
            MultiWorldBtn = multiworldBtn;
            MultiWorldReadyBtn = multiworldReadyBtn;
            URLLabel = urlLabel;
            URLInput = urlInput;
            NicknameLabel = nicknameLabel;
            NicknameInput = nicknameInput;
            RoomLabel = roomLabel;
            RoomInput = roomInput;
            ReadyPlayersLabel = readyPlayers;
            RejoinBtn = rejoinBtn;
            StartMultiWorldBtn = startMultiWorldBtn;
            StartRandoBtn = startRandoBtn;
        }

        public RandoMenuItem<string> MultiWorldBtn { get; }
        public RandoMenuItem<bool> MultiWorldReadyBtn { get; }
        public GameObject URLLabel { get; }
        public InputField URLInput { get; }
        public GameObject NicknameLabel { get; }
        public InputField NicknameInput { get; }
        public GameObject RoomLabel { get; }
        public InputField RoomInput { get; }
        public GameObject ReadyPlayersLabel { get; }
        public MenuButton RejoinBtn { get; }
        public MenuButton StartMultiWorldBtn { get; }
        public MenuButton StartRandoBtn { get; }
    }
}
