# BinderTool
A Dark Souls II / Dark Souls III / Bloodborne / Sekiro bdt, bhd, bnd, dcx, tpf, fmg and param unpacking tool

[![Build status](https://ci.appveyor.com/api/projects/status/t6tf7uuggto1827a?svg=true)](https://ci.appveyor.com/project/Atvaark/bindertool)

Binaries can be downloaded under [releases](https://github.com/Atvaark/BinderTool/releases).

# Sekiro support still WIP

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
BinderTool input_file_path [output_folder_path]
```
If no output folder path is specified then the files are unpacked in a folder called after the archive that is getting unpacked.

## Examples

Unpacking an encrypted bdt file. This requires the corresponding .bhd files to be in the same folder.
```
BinderTool Data1.bdt
BinderTool DLC1.bdt
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
```

Unpacking a dcx file
```
BinderTool 01.febnd.dcx
```
