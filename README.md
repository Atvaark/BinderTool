# BinderTool
A Dark Souls III bdt, bhd, bnd, dcx and sl2 unpacking tool

If you are looking for the Dark Souls II file check out the [v0.3](https://github.com/Atvaark/BinderTool/tree/v0.3) branch.

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

## Remarks

### File names
There is  currently no dictionary for Dark Souls III files. Once the hashing algorithm has been figured out the dictionary can be filled again.
