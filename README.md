# BinderTool
A Dark Souls II bdt, bnd and dcx unpacking tool

##Requirements
```
Microsoft .NET Framework 4.5 
SharpZipLib
BouncyCastle
```

##Usage
```
BinderTool input_file_path [output_folder_path]
```
If no output folder path is specified then the files are unpacked in a folder called after the archive that is getting unpacked. 

##Examples

Unpacking an encrypted bdt file. This requires the corresponding KeyCode.pem and Ebl.bhd files to be in the same folder.
```
BinderTool GameDataEbl.bdt
```

Unpacking an unencrypted bdt file. This requires the corresponding bhd file to be in the same folder.
```
BinderTool t10_23_00_00.tpfbdt
```

Unpacking a bnd file
```
BinderTool c0001.bnd
```

Unpacking a dcx file
```
BinderTool 01.febnd.dcx
```

##Remarks

Alternative file extensions for bdt files are:

Extension        | Content
---------------- | ------------
gibdt            | Map models
hkdbdt           | Map models (Havok)
mapbdt           | Map models
tpfbdt           | Map textures
