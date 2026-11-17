# MusicMod for Sailwind

A BepInEx mod for [Sailwind](https://store.steampowered.com/app/1764530/Sailwind/) that adds playable instruments: a **lute**, a **xiao flute**, **nakers** (hand drums), a **hurdy-gurdy**, a **square piano**, a **kalimba** and a **shamisen**.

- Download / install: [Thunderstore page](https://thunderstore.io/c/sailwind/p/Viking_D_Mike/MusicMod/) (recommended, works with Gale and r2modman), or the zip in [Releases](../../releases/latest).
- How to play, where to buy each instrument and configuration: see the Thunderstore page.

## Source code

The plugin is a small C# class library (.NET Framework 4.7.2) for BepInEx 5.

| File | What it does |
|---|---|
| `LaudMod/LaudPlugin.cs` | Plugin entry point: loads the asset bundle synchronously in Awake, fixes the material colours, registers the instruments in the game's prefab list (fixed slots 20391-20398, through a Harmony prefix on `PrefabsDirectory.Start`), puts them in their shops (and keeps shop items in place) and registers the hook offsets for HooksHangMore (optional). |
| `LaudMod/Instrumento.cs` | Data and config entries of each instrument (keys, volume, positions, shop). |
| `LaudMod/InstrumentoItem.cs` | Shared item logic (play mode with the activate key, hand/play positions) plus the nakers, kalimba, shamisen, the flute (held notes with crossfade and pitch glide, with a per-sample volume ramp to avoid clicks) and the instruments built on it (hurdy-gurdy with a drone and a turning crank, and more). |
| `LaudMod/LaudItem.cs` | The lute item. |
| `LaudMod/PianoItem.cs` | The square piano: played where it stands (not in your hands), with an opening keyboard lid and big lid. |

Comments and identifiers are in Spanish.

### Building

1. Open `LaudMod.slnx` in Visual Studio 2022 or later.
2. Check the reference paths in `LaudMod/LaudMod.csproj`. They point to a default Steam install of Sailwind (`Sailwind_Data/Managed`) and to `BepInEx/core/BepInEx.dll` and `BepInEx/core/0Harmony.dll` inside the game folder. Change them if your install is elsewhere.
3. Build in **Release**. Copy `LaudMod.dll` together with the `laud_bundle` asset bundle (from the release zip) into `BepInEx/plugins/MusicMod/`.

The Debug build exposes extra settings for development (positions, shop placement, an F10 key that logs coordinates, test prices). The Release build only exposes note keys, volume, the hurdy-gurdy drone volume and the piano keys.

The asset bundle (3D models and sounds) was built with Unity 2019.1.10f1. The Unity project is not included in this repository.

## License

The **source code** is released under the MIT License (see `LICENSE`).

The **3D models** were made by the author. The **sounds** are not covered by the MIT License and keep their original licenses:

- Xiao flute: Pixabay recording (Pixabay Content License).
- Hurdy-gurdy: Pixabay recordings (Pixabay Content License).
- Square piano: Freesound recordings by nanliu_music (CC BY-NC 4.0, non-commercial).
- Kalimba: Freesound recording by ultradust (CC BY-NC, non-commercial).
- Shamisen: SoundFont by Gubotom on Polyphone (public domain).
- Some notes: VSCO 2 Community Edition by Versilian Studios (CC0).
- The lute and drum sounds were synthesized for this mod.

## Credits

Made by Viking D. Mike. See the Thunderstore page for the full sound credits.
