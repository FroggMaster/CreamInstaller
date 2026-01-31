### [Revived] CreamInstaller: Automatic DLC Unlocker Installer & Configuration Generator
[![Latest Release](https://img.shields.io/github/v/release/FroggMaster/CreamInstaller?label=latest%20release)](https://github.com/FroggMaster/CreamInstaller/releases/latest) [![CI Build](https://github.com/FroggMaster/CreamInstaller/actions/workflows/ci-builds.yml/badge.svg)](https://github.com/FroggMaster/CreamInstaller/actions/workflows/ci-builds.yml)

![Program Preview Image](https://raw.githubusercontent.com/FroggMaster/CreamInstaller/main/preview.png)

###### **NOTE:** This is simply a preview image; this is not a list of supported games nor configurations!

##### The program utilizes the latest version of [CreamAPI](https://cs.rin.ru/forum/viewtopic.php?f=29&t=70576) by [deadmau5](https://cs.rin.ru/forum/viewtopic.php?f=29&t=70576). It also utilizes the latest versions of [SmokeAPI](https://github.com/acidicoala/SmokeAPI), [Koaloader](https://github.com/acidicoala/Koaloader), [ScreamAPI](https://github.com/acidicoala/ScreamAPI), [Uplay R1 Unlocker](https://github.com/acidicoala/UplayR1Unlocker) and [Uplay R2 Unlocker](https://github.com/acidicoala/UplayR2Unlocker), all by [acidicoala](https://github.com/acidicoala). All unlockers are downloaded and embedded into the program itself; no further downloads necessary on your part!
---
#### Description:
Automatically finds all installed Steam, Epic and Ubisoft games with their respective DLC-related DLL locations on the user's computer,
parses SteamCMD, Steam Store and Epic Games Store for user-selected games' DLCs, then provides a very simple graphical interface
utilizing the gathered information for the maintenance of DLC unlockers.

The primary function of the program is to **automatically generate and install DLC unlockers** for whichever
games and DLCs the user selects; however, through the use of **right-click context menus** the user can also:
* automatically repair the Paradox Launcher
* open parsed Steam and/or Epic Games appinfo in Notepad(++)
* refresh parsed Steam and/or Epic Games appinfo
* open root game directories and important DLL directories in Explorer
* open SteamDB, ScreamDB, Steam Store, Epic Games Store, Steam Community, Ubisoft Store, and official game website links (where applicable) in the default browser

---
#### Features:
* Automatic download and installation of SteamCMD as necessary whenever a Steam game is chosen. *For gathering appinfo such as name, buildid, listofdlc, depots, etc.*
* Automatic gathering and caching of information for all selected Steam and Epic games and **ALL** of their DLCs.
* Automatic DLL installation and configuration generation for CreamAPI, Koaloader, ScreamAPI, Uplay R1 Unlocker and Uplay R2 Unlocker.
* Automatic uninstallation of DLLs and configurations for CreamAPI, Koaloader, SmokeAPI, ScreamAPI, Uplay R1 Unlocker and Uplay R2 Unlocker.
* Automatic reparation of the Paradox Launcher (and manually via the right-click context menu "Repair" option). *For when the launcher updates whilst you have CreamAPI, SmokeAPI or ScreamAPI installed to it.*
---
<details>
  <summary><strong>Continuous Integration (CI) Builds</strong></summary>

  - CreamInstaller is automatically built and tested using GitHub Actions on every push to the **main** branch. You can view all recent CI build runs by clicking the status badge at the top or here: [![CI Build](https://github.com/FroggMaster/CreamInstaller/actions/workflows/ci-builds.yml/badge.svg)](https://github.com/FroggMaster/CreamInstaller/actions/workflows/ci-builds.yml)

</details>

---
#### Installation:
1. Click [here](https://github.com/FroggMaster/CreamInstaller/releases/latest/download/CreamInstaller.exe) to download the latest release from [GitHub](https://github.com/FroggMaster/CreamInstaller).
2. Move the executable to anywhere on your computer you want. *It's completely self-contained.*

If the program doesn't seem to launch, try downloading and installing [.NET Desktop Runtime 8.0.7](https://download.visualstudio.microsoft.com/download/pr/bb581716-4cca-466e-9857-512e2371734b/5fe261422a7305171866fd7812d0976f/windowsdesktop-runtime-8.0.7-win-x64.exe) and restarting your computer. Note that the program currently only supports Windows 10+ 64-bit machines as seen [here](https://github.com/dotnet/core/blob/main/release-notes/8.0/supported-os.md).

---
#### Usage:
1. Start the program executable. *Read above under Installation if it doesn't launch.*
2. Choose which programs and/or games the program should scan for DLC. *The program automatically gathers all installed games from Steam, Epic and Ubisoft directories.*
3. Wait for the program to download and install SteamCMD (if you chose a Steam game). *Very fast, depends on internet speed.*
4. Wait for the program to gather and cache the chosen games' information & DLCs. *May take a good amount of time on the first run, depends on how many games you chose and how many DLCs they have.*
5. **CAREFULLY** select which games' DLCs you wish to unlock. *Obviously none of the DLC unlockers are tested for every single game!*
6. Choose whether or not to install in Proxy mode, and if so then also pick the proxy DLL to use. *If the default winmm.dll doesn't work, then see [here](https://cs.rin.ru/forum/viewtopic.php?p=2552172#p2552172) to find one that does.*
7. Click the **Generate and Install** button.
8. Click the **OK** button to close the program.
9. If any of the DLC unlockers cause problems with any of the games you installed them on, simply go back to step 5 and select what games you wish you **revert** changes to, and instead click the **Uninstall** button this time.

##### **NOTE:** This program does not automatically download nor install actual DLC files for you; as the title of the program states, this program is only a *DLC Unlocker* installer. Should the game you wish to unlock DLC for not already come with the DLCs installed, as is the case with a good majority of games, you must find, download and install those to the game yourself. This process includes manually installing new DLCs and manually updating the previously manually installed DLCs after game updates.

---
#### FAQ / Common Issues

**Q:** The program won't launch.

**A:** Check the following in order:

1. **System requirements**: Windows 10+ 64-bit only ([.Net 8.0 Supported OS List](https://github.com/dotnet/core/blob/main/release-notes/8.0/supported-os.md))
2. **Extract before running**: Ensure you've extracted the executable from the ZIP file
3. **Antivirus**: Add an exception for CreamInstaller (see [False Positives](#false-positive-antivirus-detections) below)
4. **Runtime**: Install [.NET 8 Desktop Runtime](https://github.com/FroggMaster/CreamInstaller#installation) and restart your computer

If none of these work, your system may not support .NET 8 or have underlying system issues.

---

**Q:** DLCs aren't unlocking in my game.

**A:** CreamInstaller only installs the unlockers it doesn't guarantee they'll work for every game. Assuming the program functioned as it was supposed to by properly installing DLC unlockers to your chosen games, this is not an issue I can do anything about and it's entirely up to you to seek the appropriate resources to fix it yourself

If the installation completed successfully but DLCs still aren't unlocked:

- Check the [Usage section](https://github.com/FroggMaster/CreamInstaller#usage) for proper setup
- Visit the [CS.RIN.RU forum](https://cs.rin.ru/forum/viewforum.php?f=10) for game-specific troubleshooting

This is **not** a bug with CreamInstaller.

---

**Q:** My antivirus detects CreamInstaller as malware.

**A:** **These are false positives.** See the detailed explanation below.

### False Positive Antivirus Detections

<details>
<summary>Click to expand for information about false positives</summary>

## Why Antivirus Software Flags CreamInstaller

CreamInstaller is **not malware**, but it's commonly flagged because of its functionality:

| Reason | Explanation |
|--------|-------------|
| **DLL modification** | Replaces game DLLs to unlock content which is identical behavior to some malware |
| **DLC Unlocker Process hooking** | Embedded DLC unlockers interact with Steam/Epic/Ubisoft/Game processes |
| **Compressed executable** | Published as a single-file .exe, which AVs can associate with malware packing |
| **Not code-signed** | No Extended Validation certificate ($300-500/year) means lower AV reputation (**_I won't be paying for this either._**) |
| **MISC** | Game modding tools are often flagged by pattern matching, regardless of intent |

## Common False Positive Names

| Detection Name                          | What It Usually Means / Why It’s a False Positive |
|----------------------------------------|---------------------------------------------------|
| Mamson.A!ac                            | Generic heuristic detection. Often triggered by packed or obfuscated executables, especially mods, cracks, or custom tools. |
| Phonzy.A!ml                            | Machine-learning based detection. Flags unusual behavior patterns rather than known malware signatures. Common with new or unsigned software. |
| Wacatac.H!ml                           | Very common false positive. Triggered by compressed, encrypted, or self-updating programs (installers, launchers, game mods). |
| Malgent!MSR                           | “Generic malware” label from Microsoft. Means the file behaves oddly but is not confirmed malicious. Often hits scripts and admin tools. |
| Tiggre!rfn                            | Heuristic detection related to runtime behavior. Often seen with automation tools, cheats, or programs that inject or hook processes. |
| UDS:DangerousObject.Multi.Generic     | User-defined or reputation-based detection. Flags tools that *can* be abused (network, scripting, admin utilities). |
| Trojan.Win64.Agent                    | Extremely broad category. Indicates suspicious activity but not a specific trojan. Very common false positive for unsigned binaries. |
| Trojan.Win64.Agent.oa!s1              | Variant of the generic Agent detection using cloud/AI heuristics. Often triggered by low-prevalence or newly compiled software. |

**See also**: [Archived issue #40](https://web.archive.org/web/20240604162435/https://github.com/pointfeev/CreamInstaller/issues/40)

## Verify Safety Yourself

CreamInstaller is **100% open source**:

1. **Review the source code** in this repository
2. **Build it yourself**
3. **Compare hashes** of your build with the official release

</details>


---
##### Bugs/Crashes/Issues:
For reliable and quick assistance, all bugs, crashes and other issues should be referred to the [GitHub Issues](https://github.com/FroggMaster/CreamInstaller/issues) page!

##### **HOWEVER**: Please read the [FAQ entry](https://github.com/FroggMaster/CreamInstaller#faq--common-issues) above and/or [template issue](https://github.com/FroggMaster/CreamInstaller/issues/new/choose) corresponding to your problem should one exist! Also, note that the [GitHub Issues](https://github.com/FroggMaster/CreamInstaller/issues) page is not your personal assistance hotline, rather it is for genuine bugs/crashes/issues with the program itself. If you post an issue which is off-topic or has already been explained within the FAQ, template issues, and/or within this text in general, I will just close it and you will be ignored.

---

##### More Information:
* SteamCMD installation and appinfo cache can be found at **C:\ProgramData\CreamInstaller**.
* The program automatically and very quickly updates from [GitHub](https://github.com/FroggMaster/CreamInstaller) by choice of the user through a dialog on startup.
* The program source and other information can be found on [GitHub](https://github.com/FroggMaster/CreamInstaller).
* Credit to [Mattahan](https://www.mattahan.com) for the program icon.
