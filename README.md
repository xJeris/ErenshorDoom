# ErenshorDoom

A [BepInEx](https://github.com/BepInEx/BepInEx) plugin that lets you play **Doom (1993)** inside the game **Erenshor**. The Doom engine runs as a draggable in-game window, rendered to a Unity texture in real time.

Built on [Managed Doom](https://github.com/sinshu/managed-doom) by sinshu — a pure C# port of the original Doom engine with zero native dependencies.

## Features

- Full Doom gameplay inside Erenshor (single-player, all episodes)
- Windowed viewport with draggable title bar
- Keyboard, mouse, and controller input
- Sound effects with 3D positional audio
- Activate with `/doom` chat command or configurable hotkey (default F9)
- Erenshor remains interactive while Doom is open

## Installation

1. Install [BepInEx 5.4.x](https://github.com/BepInEx/BepInEx/releases) into your Erenshor game directory
2. Download the latest release zip
3. Extract the `ErenshorDoom/` folder into `<Erenshor>/BepInEx/plugins/`
4. Add a Doom WAD file to the `ErenshorDoom/` plugin folder (see [WAD Files](#wad-files) below)
5. Launch Erenshor

Your plugin folder should look like:
```
BepInEx/plugins/ErenshorDoom/
    ErenshorDoom.dll
    System.Memory.dll
    System.Buffers.dll
    System.Runtime.CompilerServices.Unsafe.dll
    DOOM1.WAD
```

## Usage

- **Open Doom:** Type `/doom` in Erenshor's chat, or press **F9**
- **Close Doom:** Click the X button on the window, press F9 again, or type `/doom`
- **Drag window:** Click and drag the title bar
- **Controls:** Standard Doom keybindings (WASD/arrows to move, mouse to aim/fire, Space to use)
- **Controller:** Left stick moves, right stick turns, RT fires, A uses, B opens menu

## Configuration

Edit `BepInEx/config/com.erenshordoom.plugin.cfg` after first launch:

| Setting | Default | Description |
|---------|---------|-------------|
| ToggleKey | F9 | Key to open/close the Doom viewport |
| RenderScale | 1 | Resolution multiplier (1=320x200, 2=640x400, 3=960x600) |
| SfxVolume | 80 | Sound effects volume (0-100) |
| MusicVolume | 60 | Music volume (0-100, not yet implemented) |
| WadFileName | DOOM1.WAD | WAD file to load |
| Deadzone | 0.15 | Controller stick deadzone (0-0.5) |
| TurnSensitivity | 5 | Controller turn speed (1-10) |

## Building from Source

### Prerequisites

- .NET SDK with .NET Framework 4.7.2 targeting pack
- Erenshor installed with BepInEx 5.4.x

### Build

```bash
dotnet build ErenshorDoom/ErenshorDoom.csproj -c Release -p:ErenshorDir="C:\path\to\Erenshor"
```

Or set the `ERENSHOR_DIR` environment variable:

```bash
set ERENSHOR_DIR=C:\path\to\Erenshor
dotnet build ErenshorDoom/ErenshorDoom.csproj -c Release
```

The build auto-detects common Steam install paths if neither is set.

## Current Status (v0.1.0)

- Video, input, sound effects, and UI are fully functional
- Music playback is stubbed (MeltySynth integration planned)
- Save/load uses Doom's built-in system (saves to working directory)

## WAD Files

You need a Doom WAD file to play. A free shareware copy of `DOOM1.WAD` is included in the [`WAD/`](WAD/) folder of this repository — just download it and place it in your plugin folder.

You can also supply your own WAD file if you own a copy of Doom. Supported WADs:

| WAD | Contents | How to get it |
|-----|----------|---------------|
| `DOOM1.WAD` | Shareware — Episode 1: Knee-Deep in the Dead | Included in [`WAD/`](WAD/) folder |
| `DOOM.WAD` | Ultimate Doom — all 4 episodes | [Steam](https://store.steampowered.com/app/2280/Ultimate_Doom/), [GOG](https://www.gog.com/game/the_ultimate_doom) |
| `DOOM2.WAD` | Doom II: Hell on Earth | [Steam](https://store.steampowered.com/app/2300/Doom_II/), [GOG](https://www.gog.com/game/doom_ii_final_doom) |
| `FREEDOOM1.WAD` / `FREEDOOM2.WAD` | Free open-source replacements | [Freedoom](https://freedoom.github.io/) |

Place whichever WAD you want to use in your plugin folder and set the `WadFileName` config option if it's not `DOOM1.WAD`.

## License

This project is licensed under the **GNU General Public License v2.0** (GPLv2), inherited from the original Doom source code and Managed Doom.

## Credits

- **id Software** — Original Doom engine
- **sinshu (Nobuaki Tanaka)** — [Managed Doom](https://github.com/sinshu/managed-doom), the pure C# Doom port this project builds on
- **BepInEx team** — Plugin framework
