# BinderTool
A Dark Souls III bdt, bhd, bnd, dcx and sl2 unpacking tool

Binaries can be downloaded under [releases](https://github.com/Atvaark/BinderTool/releases).

If you are looking for the Dark Souls II release check out the [v0.3](https://github.com/Atvaark/BinderTool/tree/v0.3) branch.

## Progress
- [X] Fix decrypting and unpacking DS III files.
- [ ] Create a dictionary
  - [x] Extract file names statically from the application
  - [ ] Extract file names statically from the archive files
  - [ ] Extract file names dynamically from the application
  - *Hooking the hash function and logging all the valid files is probably the right way. Unfortunately the function is inlined at lots of locations in the application. (Search for "imul 37")* 

### Dictionary Progress

| archive   | found names | total names | found percentage |
| :---      | ---:        | ---:        | ---:             |
| data1     |        2067 |        2110 |           97,96% |
| data2     |        2140 |        2140 |          100,00% |
| data3     |         671 |         673 |           99,70% |
| data4     |         951 |         951 |          100,00% |
| data5     |        6301 |        6755 |           93,28% |
| **total** |   **12130** |   **12629** |       **96,05%** |

## Requirements
```
Microsoft .NET Framework 4.5
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
```

Unpacking an unencrypted bdt file. This requires the corresponding bhd file to be in the same folder.
```
BinderTool t10_23_00_00.tpfbdt
```

Unpacking an encrypted bhd file. This requires the corresponding KeyCode.pem files to be in the same folder.
```
BinderTool Data1.bhd
```

Unpacking a bnd file
```
BinderTool c0001.bnd
```

Unpacking a dcx file
```
BinderTool 01.febnd.dcx
```
