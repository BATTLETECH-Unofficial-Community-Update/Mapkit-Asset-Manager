Mapkit Asset Manager is a fork of [AssetRipper](https://github.com/AssetRipper/AssetRipper) (GPL-3.0). It is not
affiliated with the AssetRipper project.

It is the tool the BattleTech UCU (Unofficial Community Update) map kit runs to extract a retail map from the game's
files into a Unity project. The kit calls it as a separate program and never links it.

## How it differs from upstream

The base is upstream commit `189ccb660ddaf6e1abdd1422113ea60fde8470b4` (AssetRipper 1.3.14, "Fix warnings in AssemblyDumper
project"). Namespaces, assembly names and the command line's `AssetRipper.CLI.dll` are upstream's; only the name you read
changed. Each change below is one commit.

1. **Stable GUIDs.** Upstream gives every exported asset a random GUID (`UnityGuid.NewGuid()`), so two exports of different
   levels disagree on the assets they share. Here the GUID is the MD5 of the asset's identity in the game files:
   `file/<file name>/<path ID>` for an asset read from a game file, `processed/<collection>/<class ID>/<directory>/<name>`
   for one AssetRipper generates, `scene/<level file name>` for a scene with no GUID of its own. A generated prefab takes its
   root GameObject's identity, and a hash collision appends `#n`. The CLI writes `AuxiliaryFiles/guid_map.tsv` (path, GUID,
   kind, key). Script and assembly GUIDs were already deterministic upstream.
2. **Class-name fallback for an unresolved script.** When a MonoScript's assembly, namespace and class name do not resolve,
   the same assembly's types (nested ones included) are scanned and a type that matches on `ClassName_R`, `ClassName`, or
   `ClassName_R` ignoring case is used; the first match in enumeration order wins. Without it, BattleTech's nested and
   renamed MonoBehaviour types stay raw blobs and their references come out as placeholders.
3. **`ScriptContentLevel` defaults to `Level1`.** At `Level2`, ICSharpCode.Decompiler overflows the stack
   (`AddCheckedBlocks.GetResult` recursion) on self-referencing MonoBehaviours, such as BattleTech's `Assembly-CSharp`.
   `Level1` still loads the assemblies for type resolution, so sprite GUIDs and component references resolve, but method
   bodies come out stubbed. Do not read game logic from this tool's script output.
4. **`ShaderExportMode` defaults to `Yaml`.** Shaders export as Unity YAML ShaderAssets (ShaderLab source plus the compiled
   programs) instead of the `Dummy` stand-in, so the Unity 6 Editor can recompile them. The export is larger and slower.
5. **A command-line front end.** Upstream has no CLI project at this commit. `Source/AssetRipper.CLI` takes
   `<input> <output>`: it loads and processes the game data and exports a Unity project, with every setting at its default
   (which is why changes 3 and 4 are changes to the defaults).

The kit's copy of the same changes is a patch set against the base commit, in the BattleTech UCU repository under
`tools/map-kit/assetripper-patches/`.

## Build

Needs git and a .NET SDK that targets `net10.0` (built here with SDK 11.0.100). The first restore needs NuGet access.
The upstream package versions restore and build as they are; no package pins are needed.

```
dotnet publish Source/AssetRipper.CLI/AssetRipper.CLI.csproj -c Release -o <publish folder>
```

## Run

```
dotnet <publish folder>/AssetRipper.CLI.dll <input folder> <output folder>
```

The input folder is the game's data folder (for BattleTech, a staged `BATTLETECH` folder holding the level files and their
externals). The output folder gets `ExportedProject/` and `AuxiliaryFiles/`, including `guid_map.tsv`. The tool makes no
network call.

## Licence

GPL-3.0, upstream's `LICENSE.md` unchanged. See `NOTICE` for the copyright holders.
