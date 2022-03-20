# BinderTool
A Dark Souls II / Dark Souls III / Bloodborne / Sekiro / Elden Ring bdt, bhd, bnd, dcx, tpf, fmg and param unpacking tool.

[![Build status](https://ci.appveyor.com/api/projects/status/t6tf7uuggto1827a?svg=true)](https://ci.appveyor.com/project/Atvaark/bindertool)

Binaries can be downloaded under [releases](https://github.com/Atvaark/BinderTool/releases).

### Elden Ring Dictionary Progress

| archive   | found names | total names | found percentage |
| :---      | ---:        | ---:        | ---:             |
| data0     |        4422 |        4434 |           99.73% |
| data1     |       37878 |       37878 |          100.00% |
| data2     |       36240 |       36240 |          100.00% |
| data3     |        1556 |        1556 |          100.00% |
| sd        |        8298 |        8298 |          100.00% |
| **total** |   **88394** |   **88406** |       **99.99%** |

### DS III Dictionary Progress

| archive   | found names | total names | found percentage |
| :---      | ---:        | ---:        | ---:             |
| data1     |        2067 |        2110 |           97,96% |
| data2     |        2140 |        2140 |          100,00% |
| data3     |         671 |         673 |           99,70% |
| data4     |         951 |         951 |          100,00% |
| data5     |        6301 |        6755 |           93,28% |
| dlc1      |           0 |         775 |               0% |
| dlc2      |           0 |        1264 |               0% |
| **total** |   **12130** |   **14668** |          **82%** |

## Requirements
```
64-bit Windows
Microsoft .NET Framework 4.6.2
```

## Usage
```
BinderTool input_file_path [output_path] [flags]
```
If no output folder path is specified then the files are unpacked in a folder called after the archive that is getting unpacked.

Flags:

| Flag | Value | Effect |
| --- | --- | --- |
| `-g`, `--game` | See below | Sets which game the files to extract are from |
| `-t`, `--type` | See below | Sets which type of file to interpret the input file as |
| `-r`, `--recurse` | `true` or `false` | When `true`, recurses to child folders when the input is a folder |
| `--extract-bnd` | `true` or `false` | When `true`, automatically extracts `bnd`-type files instead of outputting the container file |
| `--extract-tpf` | `true` or `false` | When `true`, automatically extracts `tpf`-type files instead of outputting the container file |
| `--extract-param` | `true` or `false` | When `true`, automatically extracts `param`-type files instead of outputting the container file |
| `--extract-fmg` | `true` or `false` | When `true`, automatically extracts `fmg`-type files instead of outputting the container file |
| `--extract-enfl` | `true` or `false` | When `true`, automatically extracts `entryfilelist`-type files instead of outputting the container file |
| `--collate-enfl-path` | A path to a file | When non-empty, collates all file names listed in `entryfilelist`-type files into a single file |
| `--only-process-extension` | A file extension, including the period | When non-empty, only processes *inner* files (ones contained by the input of the program) with the given extension |

Dev flags:
| Flag | Value | Effect |
| --- | --- | --- |
| `--only-output-unknown` | `true` or `false` | Only outputs files where the file name is not in the dictionary |
| `--filename-search` | The field name of a file name search from `FileSearch.cs` | Runs a search over the input `.hashlist` for matching file names |
| `--dictionary-progress` | A folder containing `.bhd`s or a `.bhd` | Outputs information about the number of file names missing from the dictionary |

Valid values for `game`:
- Detect (default)
- Common
- DarkSouls2
- DarkSouls3
- Bloodborne
- Sekiro
- EldenRing

Valid values for `file type`:
- Detect (default)
- Folder
- Regulation
- Dcx
- EncryptedBdt
- EncryptedBhd
- Bdt
- Bhd
- Bnd
- Savegame
- Tpf
- Param
- Fmg
- Enfl

## Examples

Unpacking an encrypted bdt file. This requires the corresponding .bhd files to be in the same folder.
```
BinderTool Data1.bdt
BinderTool DLC1.bdt
BinderTool Data1.bdt "C:\Dark Souls 3\Data1" --game DarkSouls3 --extract-bhd true --extract-tpf true
```

Unpacking an unencrypted bdt file. This requires the corresponding bhd file to be in the same folder.
```
BinderTool t10_23_00_00.tpfbdt
```

Unpacking an encrypted bhd file. This will only work for files with known decryption keys such as Data1.bhd-Data5.bhd.
```
BinderTool Data1.bhd
BinderTool DLC1.bhd
```

Unpacking a bnd file
```
BinderTool c0001.bnd
BinderTool c0001.tpfbnd "..\..\." --extract-tpf true
```

Unpacking a dcx file
```
BinderTool 01.febnd.dcx
```

## Descriptions of file types BinderTool can handle

### `.bdt`, `.bhd`

`.bdt` files are archive files (like a `.zip` file) that may contain an arbitrary number of files. `.bhd` files are "header" files for a corresponding `.bdt` file which list information about where files are in the `.bdt` and what their names/name hashes are. These files may be encrypted, in which case file names are not stored, and only their hashes are available. Both the `.bhd` and `.bdt` are needed to extract files.

### `.bnd`

`.bnd` files are archive files (like a `.zip` file) that may contain an arbitrary number of files. `.bnd` files include file names for every file.

### `.tpf`

`.tpf` files are archive files (like a `.zip` file) that may contain an arbitrary number of files, but usually only contain one file. `.tpf` files typically only contain `.dds` files, which are image files used by the game engine.

### `.dcx`

`.dcx` files are compressed files which contain exactly one file, like `.gz` files.

### `.param`

`.param` files contain parameters about various game items and mechanics. `.param` files are typically stored in "regulation" files.

### `.fmg`

`.fmg` files contain a list of strings which each have an id number.

### `.entryfilelist`

`.entryfilelist` files primarily contain a list of file names which are believed to be used to determine which files to load when entering a certain area.

### `.sl2`

`.sl2` files are the game's save files.

### `regulation.bin` (Elden Ring)

`regulation.bin` is the name of the "regulation" file in Elden Ring, but is actually an encrypted and compressed `.bnd` file.

### `Data0.bdt` (Dark Souls 3)

`Data0.bdt` is the name of the "regulation" file in Dark Souls 3, but is actually an encrypted and compressed `.bnd` file.