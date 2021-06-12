# ZipExtract-MakeBak
 Extract an archive file to a destination (with making backup before overwriting).

### ZipEB = Zip Extractor with Backup.
 Despite the name, it can also extract 7z, RAR and GZip, aside from Zip.
 
# Usage:
 ```
 zipeb.exe -i <archive to be extracted> -o <output directory to extract to> -b <a path to create an archive which contains all files before overwritten>
 ```
 Explain:
 * `zipeb.exe` is the filename of the executable.
 * `-i <archive to be extracted>`
   * Relative or full path to the archive file that you want to extract/uncompress.
   * E.g: (Notice the double quote being used when the path has blank space(s), and the required blank space after the `-i`)
     * `-i my_data\file\patch.zip`
     * `-i "my data\files\patch.zip"`
     * `-i C:\data\files\patch.zip`
     * `-i "C:\data files\patch.zip"`
 * `-o <output directory to extract to>`
   * Relative or full path to the folder where the archive will be extracted to.
   * E.g: (Notice the double quote being used when the path has blank space(s), and the required blank space after the `-o`)
     * `-o my_data\patched`
     * `-o "my data\patched"`
     * `-o C:\data\patched`
     * `-o "C:\data files patched"`
 * `-b <a path to create an archive which contains all files before overwritten>`
   * Relative or full path to the folder where the archive will be extracted to.
   * E.g: (Notice the double quote being used when the path has blank space(s), and the required blank space after the `-b`)
     * `-b my_data\backup\original.zip`
     * `-b "my data\backup\original.zip"`
     * `-b C:\data\backup\original.zip`
     * `-b "C:\data files original.zip"`
     
 Full example:
 * `zipeb.exe -i enpatch.zip -o bin\data -b original_files.zip`
 * `zipeb.exe -i patch.zip -o "game bin\data" -b "original files.zip"`
 * `zipeb.exe -i "D:\my games\patch english.zip" -o "E:\game bin\data" -b "D:\my games\original files.zip"`