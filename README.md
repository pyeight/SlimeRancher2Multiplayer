# Slime Rancher 2 Multiplayer Mod - <a href="https://discord.com/invite/a7wfBw5feU" target="_blank" rel="noopener noreferrer">Join our Discord</a>

### You can also get the mod on <a href="https://www.nexusmods.com/slimerancher2/mods/118" target="_blank" rel="noopener noreferrer">NexusMods</a>

**LEGEND:**
- 🟩 fully synced
- 🟨 partially synced
- 🟧 in progress
- 🟥 not synced

|      Feature      | Status |                                Details about Feature                                 |
|:-----------------:|:------:|:------------------------------------------------------------------------------------:|
|  Player Movement  |   🟩   |                                   Fully functional                                   |
| Player Animations |   🟩   |                                   Fully functional                                   |
|  Player Sound FX  |   🟩   |                                   Fully functional                                   |
| Player Visual FX  |   🟨   |                      Vac Shockwave + Vac suction trail missing                       |
| Player Inventory  |   🟥   |                Not stored on the server, use the same save to rejoin                 |
| Initial Save load |   🟩   |    Everything that is ___currently___ synchronised will be loaded on Player join     |
|      Slimes       |   🟩   |                         Fully functional (Including Radiant)                         |
|  Slime behaviour  |   🟨   | Partially functional (Yolky, Dervish, Sloomber, Gold, Lucky), Tangle & Tabby missing |
|   Gordo Slimes    |   🟩   |                                   Fully functional                                   |
|  Actors (Items)   |   🟩   |                                   Fully functional                                   |
|  Resource Nodes   |   🟩   |                                   Fully functional                                   |
|     Landplots     |   🟩   |      Fully functional (Gardens, Silos, etc. + Plort collectors & Auto-feeders)       |
| Landplot upgrades |   🟩   |                                   Fully functional                                   |
|      Gadgets      |   🟩   |                                   Fully functional                                   |
|      Drones       |   🟩   |                                   Fully functional                                   |
|     Upgrades      |   🟩   |                                   Fully functional                                   |
|     Currency      |   🟩   |                                   Fully functional                                   |
|   Market Prices   |   🟩   |                                   Fully functional                                   |
|     Sprinkles     |   🟩   |                                   Fully functional                                   |
|     Refinery      |   🟩   |                                   Fully functional                                   |
|     World FX      |   🟩   |                                   Fully functional                                   |
|    Slimepedia     |   🟩   |                                   Fully functional                                   |
|        Map        |   🟩   |               Fully functional (Including player icons & locator bar)                |
|  Map Expansions   |   🟩   |                                   Fully functional                                   |
|    Teleporters    |   🟩   |                                   Fully functional                                   |
|      Geysers      |   🟩   |                                   Fully functional                                   |
|      Weather      |   🟨   |              Initial weather sometimes fails, updates fully functional               |
|     Lightning     |   🟩   |                                   Fully functional                                   |
|     Tornados      |   🟥   |                                   Not implemented                                    |
|       Time        |   🟩   |                                   Fully functional                                   |
|  Gray Labyrinth   |   🟨   |      Partially functional (Switches, Prisma Barriers, Puzzle Slots, Depositors)      |
|  Multiplayer API  |   🟩   |                                   Fully functional                                   |

# ⚠ MAKE SURE TO BACK UP YOUR SAVES ⚠

## Installation
1. Download and run the [MelonLoader Installer](https://github.com/LavaGang/MelonLoader/releases/download/v0.7.1/MelonLoader.Installer.exe) and install **MelonLoader 0.7.3** onto Slime Rancher 2.
2. Download [Starlight 4.0.3](https://github.com/ThatFinnDev/Starlight/releases/download/v4.0.3/Starlight.dll) and put `Starlight.dll` into the `Mods` folder of your game install.
3. Download `Ranching Together` from [Nexusmods](https://www.nexusmods.com/slimerancher2/mods/118) or our [Discord Server](https://discord.com/invite/a7wfBw5feU) and put it into the `Mods` folder.
4. Launch the game, the multiplayer menu appears on the main menu screen.
5. Configure the settings in the first time settings screen.

Every player needs the **same version** of all mods being played with.

The host's save is the one being played on, everyone else joins it.
If you wanna host, join the save file you wanna play on.

If you wanna connect, join an **EMPTY SAVE FILE** and always use this one to connect to this hosts save.
Once you join the host, **ALL YOUR PROGRESS ON THE SAVE FILE YOU JOINED WITH IS ERASED AND REPLACED!**

### Requirements
- [MelonLoader 0.7.3](https://github.com/LavaGang/MelonLoader/releases/download/v0.7.1/MelonLoader.Installer.exe)
- [Starlight 4.0.3](https://github.com/ThatFinnDev/Starlight/releases/download/v4.0.3/Starlight.dll)

## Configuration
All settings are in MelonPreferences (`UserData/MelonPreferences.cfg`) under the `SR2MP` category.
They are also editable via the *Mod Settings* screen (`ESC -> Mods -> Mods Settings -> Scroll down`).
All settings you change via the Multiplayer GUI are automatically saved.

|            Setting            | Default  |                       Description                        |
|:-----------------------------:|:--------:|:--------------------------------------------------------:|
|          `username`           | `Player` |           Your name as shown to other players            |
|       `username_color`        | `FFFFFF` |             Hex color of your username label             |
|         `allow_cheats`        | `false`  |          Allows cheats in multiplayer sessions           |
|        `streamer_mode`        | `false`  |      Hides IPs and join codes in the multiplayer UI      |
|          `host_port`          |  `1919`  |                UDP port used when hosting                |
|   `recent_ip` / `recent_port` | *empty*  |        Last used connection, saved automatically         |
|     `firewall_exceptions`     | *empty*  | Firewall rules created by the mod, managed automatically |
|       `packet_size_log`       | `false`  |            Logs packet sizes (debug setting)             |
|        `packet_ack_log`       |  `true`  |  Logs reliable-packet acknowledgements (debug setting)   |
|    `internal_setup_ui_new`    |  `true`  |         Internal first time settings screen flag         |
| `the_rock_plorts_are_coming`  | `false`  |  Rock Plort Mode, a troll mode, **FULLY BREAKS SAVES!**  |

## Official Mod Support
These mods have dedicated Ranching Together integration, they are fully working in multiplayer when installed:

|                             Mod                             | Version |                             Nexus                             |                           Source Code                           |                         Support                         |
|:-----------------------------------------------------------:|:-------:|:-------------------------------------------------------------:|:---------------------------------------------------------------:|:-------------------------------------------------------:|
| **Better Drones**: explorer drones really fly to resources  | `0.1.1` | [NexusMods](https://www.nexusmods.com/slimerancher2/mods/177) |        [GitHub](https://github.com/pyeight/BetterDrones)        | [Discord Server](https://discord.com/invite/a7wfBw5feU) |
| **Placement Improvements**: custom placement rules & colors | `1.0.0` | [NexusMods](https://www.nexusmods.com/slimerancher2/mods/179) | [GitHub](https://github.com/BlackthornZZ/PlacementImprovements) |    Discord: `lunar_snail` (ID: `426024775333314570`)    |
| **Vacuum Modifications**: custom limits & instant transfers | `2.3.5` | [NexusMods](https://www.nexusmods.com/slimerancher2/mods/45)  |  [GitHub](https://github.com/Bread-Ch4n/Vacuum-Modifications)   |   Discord: `.bread_chan.` (ID: `212243828831289344`)    |

### Mod Integration Requirements
If you want your mod to be supported by us, it has to meet a few requirements so we can build and maintain an integration for it:
- **It must depend on [Starlight](https://github.com/ThatFinnDev/Starlight)**, this gives us reliable mod detection and versioning to work with.
- **It must be somewhat structured.** Our integrations find your mod's members via reflection, 
  so the logic has to live in stable, reasonable named classes and methods (helpers, preference entries).
  Renaming or restructuring these breaks the integration, so let us know before you do it.
- Your mod must be uploaded on [NexusMods](https://www.nexusmods.com/games/slimerancher2/mods)
- The source code must be publicly available so we can review what actually needs syncing.

If your mod fits (or you need help making it fit) the requirements, reach out on the [Discord Server](https://discord.com/invite/a7wfBw5feU).

## Forks
As a community policy, we ask that forks of this project:
- provide a clearly visible way to contact their developer (e.g. a Discord handle or e-mail) in their README,
- are taken down if the Ranching Together developers request it.

Note that we may cherry-pick content from your fork back into Ranching Together if we want to.  
(forks **MUST BE** GPLv3 like this project, which allows this).

If you want to build on this project, you can reach out to us on the [Discord Server](https://discord.com/invite/a7wfBw5feU) to discuss first.

## Contributing
Contributions are welcome! A few ground rules:
- **Fully vibe-coded commits will be declined and not merged.** AI assistance is fine,
  but you need to understand every line you submit, have tested it in-game, and be able to explain
  and defend it in review. PRs that are clearly unreviewed AI output waste everyone's time.  
- Match the existing code style and architecture (see the sync pattern used across the codebase).
- Test your changes in an actual multiplayer session (host + multiple clients) before opening a PR.
- Keep the build warning-clean.

If you are unsure whether a feature fits or a practise is good, you can ask on the [Discord Server](https://discord.com/invite/a7wfBw5feU) before you start.

## Acknowledgements
Special thanks to:
- [ThatFinn](https://github.com/ThatFinnDev) for developing and maintaining their [Starlight Core Essentials Mod](https://github.com/ThatFinnDev/Starlight)
- [Lachee](https://github.com/Lachee/) for developing and maintaining their [Discord RPC C# library](https://github.com/Lachee/discord-rpc-csharp)
- Each and every one of you for supporting us!
