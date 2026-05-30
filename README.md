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
4. Place a Doom WAD file (`DOOM1.WAD`, `DOOM.WAD`, or `DOOM2.WAD`) in the `ErenshorDoom/` plugin folder
5. Launch Erenshor

Your plugin folder should look like:
```
BepInEx/plugins/ErenshorDoom/
    ErenshorDoom.dll
    System.Memory.dll
    System.Buffers.dll
    System.Runtime.CompilerServices.Unsafe.dll
    DOOM1.WAD          <-- you provide this
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

You need a Doom WAD file to play. Supported WADs:

- `DOOM1.WAD` — Shareware (Episode 1 only, freely distributable)
- `DOOM.WAD` — Registered/Ultimate Doom (all episodes)
- `DOOM2.WAD` — Doom II
- `FREEDOOM1.WAD` / `FREEDOOM2.WAD` — Free open-source alternatives from [Freedoom](https://freedoom.github.io/)

WAD files are copyrighted game data and are not included in this repository.

## License

This project is licensed under the **GNU General Public License v2.0** (GPLv2), inherited from the original Doom source code and Managed Doom.

## Credits

- **id Software** — Original Doom engine
- **sinshu (Nobuaki Tanaka)** — [Managed Doom](https://github.com/sinshu/managed-doom), the pure C# Doom port this project builds on
- **BepInEx team** — Plugin framework
