# BinderTool
A Dark Souls III bdt, bhd, bnd, dcx and sl2 unpacking tool

Binary releases will be uploaded once v0.4 is stable and can work with all new archives.

If you are looking for the Dark Souls II release check out the [v0.3](https://github.com/Atvaark/BinderTool/tree/v0.3) branch.

## Progress
- [ ] Fix decrypting Data0.bhd
- *This file serves no purpose, because Data0.bdt is an encrypted .bnd.dcx file.*
- [x] Extract decryption keys
  - [ ] Data0.bhd
  - [ ] Data0.bdt
  - [x] Data1.bhd - Data5.bhd
  - [x] Data1.bdt - Data5.bdt
  - [ ] .sl2
  - [ ] .dcx inside .bhd files
  - [ ] .bhd inside .bhd files
- [x] Fix decrypting Data1.bhd - Data5.bhd
  - [x] Fix decrypting files with known file sizes
  - [x] Fix decrypting files with unknown file sizes
  - *The file sizes of .dcx files are missing from the archives. They can only be determined by reading the .dcx header*
- [ ] Fix unpacking Data0.bdt
- *This is the regulation file, which contains all game balance changes. The decryption key (either AES or RSA) is embedded somewhere in the application.*
- [x] Fix unpacking Data1.bdt - Data5.bdt
  - [x] Fix unpacking unencrypted files
  - [ ] Fix unpacking encrypted files e.g. .dcx and .bhd
  - *.dcx and .bhd files are encrypted one more time for some reason.*
- [ ] Fix unpacking common .bhd files
  - [ ] Fix unpacking 
  - *Only works with smaller files at the moment. All data after position 1024 is still encrypted.*
  - [ ] Fix reading embedded file names
  - *The embedded file names can't be used at the moment, because they are partially encrypted*
- [x] Fix unpacking common .bdt files
- [x] Fix unpacking .dcx files
- [x] Fix unpacking .bnd files and their 
- [ ] Fix unpacking .sl2 files
- *This is basically the same format as .bnd. The files in this archives are encrypted with AES in CBC mode.*
- [x] Fix unpacking .dcx files
- [x] Fix file extension of unpacked .dcx files
- [ ] Fix file name lookup
  - [x] Fix hash code algorithm
  - *FROM just changed the prime numer used in the algorithm from 37 to 137*
  - [ ] Fix virtual file name to physical file name mapping
- [ ] Create a dictionary
  - [x] Extract file names statically from the application
  - [ ] Extract file names statically from the archive files
  - [ ] Extract file names dynamically from the application
  - *Hooking the hash function and logging all the valid files is probably the right way. Unfortunately the function is inlined at lots of locations in the application. (Search for "imul 137")* 


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
