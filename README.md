# BinderTool
A Dark Souls II bdt, bhd, bnd, dcx and sl2 unpacking tool

## Requirements
```
Microsoft .NET Framework 4.5.2
```

## Usage
```
BinderTool input_file_path [output_folder_path]
```
If no output folder path is specified then the files are unpacked in a folder called after the archive that is getting unpacked.

## Examples

Unpacking an encrypted bdt file. This requires the corresponding KeyCode.pem and Ebl.bhd files to be in the same folder.
```
BinderTool GameDataEbl.bdt
```

Unpacking an unencrypted bdt file. This requires the corresponding bhd file to be in the same folder.
```
BinderTool t10_23_00_00.tpfbdt
```

Unpacking an encrypted bhd file. This requires the corresponding KeyCode.pem files to be in the same folder.
```
BinderTool GameDataEbl.bhd
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

### Alternative file extensions for bdt files
Extension        | Content
---------------- | ------------
gibdt            | Map models
hkxbdt           | Map models (Havok)
mapbdt           | Map models
tpfbdt           | Map textures

### File names
The file names in the *Dictionary.csv* are just guesses. They were automatically generated and contain several collisions. When there are multiple file names for a hash the first file name gets picked.
If a file name can not be found by its hash value, the file extension is guessed and the file is called after the hash and the archive it originates from. Since there are a few file formats (e.g. .param) without a magic number at offset 0 there will be a few .dat files that could contain anything.
