using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BinderTool
{
    public class FileNameDictionary
    {
        private readonly Dictionary<string, Dictionary<uint, List<string>>> _dictionary;

        private FileNameDictionary()
        {
            _dictionary = new Dictionary<string, Dictionary<uint, List<string>>>();
        }

        public bool TryGetFileName(uint hash, string archiveName, out string fileName)
        {
            fileName = "";
            Dictionary<uint, List<string>> archiveDictionary;
            if (_dictionary.TryGetValue(archiveName, out archiveDictionary))
            {
                List<string> fileNames;
                if (archiveDictionary.TryGetValue(hash, out fileNames))
                {
                    // TODO: There should be no hash collisions inside an archive.
                    //if (fileNames.Count == 1)
                    //{
                    //fileName = fileNames.Single().Replace('/', '\\').TrimStart('\\');
                    fileName = fileNames.First().Replace('/', '\\').TrimStart('\\');
                    return true;
                    //}
                }
            }

            return false;
        }

        public bool TryGetFileName(uint hash, IEnumerable<string> archiveNames, out string fileName)
        {
            fileName = "";
            foreach (var archiveName in archiveNames)
            {
                if (TryGetFileName(hash, archiveName, out fileName))
                {
                    return true;
                }
            }

            return false;
        }

        public void Add(string archiveName, string fileName)
        {
            uint hash = GetHashCode(fileName, 0);

            Dictionary<uint, List<string>> archiveDictionary;
            if (_dictionary.TryGetValue(archiveName, out archiveDictionary) == false)
            {
                archiveDictionary = new Dictionary<uint, List<string>>();
                _dictionary.Add(archiveName, archiveDictionary);
            }

            List<string> fileNameList;
            if (archiveDictionary.TryGetValue(hash, out fileNameList) == false)
            {
                fileNameList = new List<string>();
                archiveDictionary.Add(hash, fileNameList);
            }

            if (fileNameList.Contains(fileName) == false)
            {
                fileNameList.Add(fileName);
            }
        }

        public static FileNameDictionary OpenFromFile(string dictionaryPath)
        {
            var dictionary = new FileNameDictionary();

            // TODO: Find out the names of the high quality files.
            // e.g. this is pair of texture packs has different name hashes while the latter contains the same textures but in higher quality.
            // 2500896703   gamedata   /model/chr/c3096.texbnd
            // 1276904764   chrhq      /???.texbnd
            string[] lines = File.ReadAllLines(dictionaryPath);
            foreach (string line in lines)
            {
                string[] splitLine = line.Split('\t');
                string archiveName = splitLine[0];
                string fileName = splitLine[1];
                dictionary.Add(archiveName, fileName);
            }

            return dictionary;
        }

        private static uint GetHashCode(string filePath, uint initialHash = 0)
        {
            if (string.IsNullOrEmpty(filePath))
                return initialHash;
            return filePath.Replace('\\', '/')
                .ToLowerInvariant()
                .Aggregate(initialHash, (i, c) => i * 37 + c);
        }
    }
}