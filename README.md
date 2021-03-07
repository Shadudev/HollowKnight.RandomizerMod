# Hollow Knight Randomizer 3.0 Multiworld

For general randomizer information, see: https://github.com/JasonILTG/HollowKnight.RandomizerMod

This mod is an extension of the existing Randomizer3.0 and MultiWorld mods to allow multiworld with all of the features of Randomizer3.0. A multiworld is a version of randomizer where items are not only scattered throughout your file, but through any number of linked game files. When picking up an item that belongs to another player, it is sent over the internet and they receive it in their game. This allows co-operative randomizer playthroughs, where you may need to pick up each other's progression to go forward.

## Features
- All randomizer features from Randomizer3.0 are supported up to Geo Rocks, Lifeblood Cocoons, and Soul Totems, including area and room randomization.
- Per player settings - Each player has full access to all settings of the randomizer, meaning they make their own choice of which item pools to randomize, which skips are allowed in logic for their world, whether items/areas/rooms are randomized, and starting location
- Nicknames - Players can set a nickname for themselves which will show up when picking up their items in other worlds
- Support for saving and rejoining - Once a multiworld file is generated, quitting and rejoining should just work, no need to fuss with player IDs or server settings
- (Mostly) compatible with BingoUI - Counters may pop up at strange times, but they should be correct including items sent from other players
- Support for room codes - When connected to the server and readying up, a room code can be specified, which can be use to coordinate readying up with other players
- Concurrent sessions - Once a randomization is generated, a random identifier is included with it which is used to spin up a new session when connecting to the server. This way, multiple concurrent rando sessions can run simultaneously on the same server

## Getting Started
1. Install the Modding API, Serecore, QoL, Vasi and Benchwarp if you haven't already (this can be done through the mod installer: https://www.nexusmods.com/hollowknight/mods/9)
2. Download `RandomizerModMW.zip` from the releases page on github: https://github.com/CallumMoseley/HollowKnight.RandomizerMod/releases
3. Copy `MultiWorldProtocol.dll`, `RandomizerLib3.0.dll`, and `RandomizerMod3.0.dll` into `Hollow Knight/hollow_knight_Data/Managed/Mods` (this will replace the existing RandomizerMod3.0 if you have it installed)
4. Note: if you run ModInstaller after this, it will tell you Randomizer is outdated and ask you to update. Select no, as this will replace multiworld randomizer with the main version.

This is all that is needed in terms of setup. To play multiworld:

1. Open Hollow Knight and start a new file
2. Enter the IP address of the server, and set Multiworld to "Yes". This will connect to the server. If the connection fails, Multiworld will stay set to "No"
3. Enter the nickname you would like to use in the "Nickname" field
4. Configure your randomizer settings however you would like
5. Enter a room code to coordinate with other players, or leave blank for the default room (easier if only one group is trying to rando at once)
6. Click "Ready" to toggle your ready status. The buttton will show how many players in the room are currently ready, and this will lock your settings in. If you would like to change settings, click again to become unready
7. Once everyone you are playing with is connected and readied up, one player should click start, and this will begin the randomizer for everyone. The player who clicks start will be marked as "Player 1", and their seed will be used for all randomization.

## Rejoining a Game
If you start a Multiworld, but one or more players crash before saving their game, you no longer need to remake the entire game. For players who no longer have the save file, simply go to the file creation screen, connect to the same Multiworld server, and click "Rejoin". This will send the item placements again, and allow you to continue playing as normal.

To avoid this happening, I recommend benchwarping immediately once you load in, because this will save and ensure the file is created.

## Setting up a Server
Currently, I have an instance of the server running at 3.96.213.176. If you want to host your own server, follow these instructions:

1. Download `MultiWorldServer.zip` from releases and extract it to wherever you would like to run the server from
2. Port forward 38281 to the machine running the server (look up tutorials online for your router)
3. Run `MultiWorldServer.exe`, and ensure that it is allowed through Windows firewall

## Server Commands

A few useful commands are implemented on the server:
1. `ready` - Gives a list of the current rooms and how many players are ready in each
2. `list` - Lists the currently active game sessions, and the players in each
3. `give <item> <session> <playerid>` - Sends `item` to player `playerId` in session `session`. Use this if an item gets lost somehow (crash or Alt-F4)

## Gallery

![Menu MW: No](/images/menu_no.jpg?raw=true)
![Menu MW: Yes](/images/menu_yes.jpg?raw=true)
![Send Item](/images/send.jpg?raw=true)
![Receive Item](/images/recv.jpg?raw=true)
![Colo Hint](/images/colo.jpg?raw=true)

## Future Plans/Known Issues
- Item placements are currently not validated for feasibility. In theory, they should be correct as they are placed, but if there are bugs an impossible seed may be generated
- Currently, grubs and essence aren't properly shown on the helper log
- There is no indicator in-game if you disconnect from the server. I plan to hopefully add better UI for managing and surfacing server connections
