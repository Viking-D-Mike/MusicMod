# MusicMod for Sailwind

A BepInEx mod for [Sailwind](https://store.steampowered.com/app/1764530/Sailwind/) that adds four playable instruments: a **lute**, a **xiao flute**, **nakers** (hand drums) and a **hurdy-gurdy**.

- Download / install: [Thunderstore page](https://thunderstore.io/c/sailwind/p/Viking_D_Mike/MusicMod/) (recommended, works with Gale and r2modman), or the zip in [Releases](../../releases).
- How to play, where to buy each instrument and configuration: see the Thunderstore page.

## Source code

The plugin is a small C# class library (.NET Framework 4.7.2) for BepInEx 5.

| File | What it does |
|---|---|
| `LaudMod/LaudPlugin.cs` | Plugin entry point: loads the asset bundle, registers the four instruments in the game's prefab list (fixed slots 20391-20394), puts them in their shops and registers the hook offsets for HooksHangMore (optional). |
| `LaudMod/Instrumento.cs` | Data and config entries of each instrument (keys, volume, positions, shop). |
| `LaudMod/InstrumentoItem.cs` | Shared item logic (play mode with the activate key, hand/play positions) plus the nakers, the flute (held notes with crossfade and pitch glide, with a per-sample volume ramp to avoid clicks) and the hurdy-gurdy (flute logic plus a drone and a turning crank). |
| `LaudMod/LaudItem.cs` | The lute item. |

Comments and identifiers are in Spanish.

### Building

1. Open `LaudMod.slnx` in Visual Studio 2022 or later.
2. Check the reference paths in `LaudMod/LaudMod.csproj`. They point to a default Steam install of Sailwind (`Sailwind_Data/Managed`) and to `BepInEx/core/BepInEx.dll` inside the game folder. Change them if your install is elsewhere.
3. Build in **Release**. Copy `LaudMod.dll` together with the `laud_bundle` asset bundle (from the release zip) into `BepInEx/plugins/MusicMod/`.

The Debug build exposes extra settings for development (positions, shop placement, an F10 key that logs coordinates, test prices). The Release build only exposes note keys, volume and the hurdy-gurdy drone volume.

The asset bundle (3D models and sounds) was built with Unity 2019.1.10f1. The Unity project is not included in this repository.

## License

The **source code** is released under the MIT License (see `LICENSE`).

The **3D models** were made by the author. The **sounds** are not covered by the MIT License: the flute recording comes from Pixabay (Pixabay Content License) and keeps its original license; the lute and drum sounds were synthesized for this mod.

## Credits

Made by Viking D. Mike. Flute sound based on "Xiao Chinese Bamboo Flute on the Bridge of Avignon" by nourishedbymusic (Pixabay).
