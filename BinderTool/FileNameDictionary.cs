using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BinderTool
{
    public class FileNameDictionary
    {
        private static string[] _virtualRoots = {
            @"N:\SPRJ\data\",
            @"N:\FDP\data\"
        };

        private readonly Dictionary<string, Dictionary<ulong, List<string>>> _dictionary;

        private FileNameDictionary()
        {
            _dictionary = new Dictionary<string, Dictionary<ulong, List<string>>>();
        }

        public bool TryGetFileName(ulong hash, string archiveName, out string fileName)
        {
            fileName = "";
            Dictionary<ulong, List<string>> archiveDictionary;
            if (_dictionary.TryGetValue(archiveName, out archiveDictionary))
            {
                List<string> fileNames;
                if (archiveDictionary.TryGetValue(hash, out fileNames))
                {
                    fileName = NormalizeFileName(fileNames.First());
                    return true;
                }
            }

            return false;
        }

        public bool TryGetFileName(ulong hash, IEnumerable<string> archiveNames, out string fileName)
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
            string hashablePath = "/" + archiveName + "/" + fileName;
            ulong hash = GetHashCode(hashablePath);

            Dictionary<ulong, List<string>> archiveDictionary;
            if (_dictionary.TryGetValue(archiveName, out archiveDictionary) == false)
            {
                archiveDictionary = new Dictionary<ulong, List<string>>();
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

            string[] lines = File.ReadAllLines(dictionaryPath);
            foreach (string line in lines)
            {
                int i = line.IndexOf(":/", StringComparison.Ordinal);
                if (i != -1)
                {
                    string archiveName = line.Substring(0, i);
                    string fileName = line.Substring(i + 2, line.Length - i - 2);

                    dictionary.Add(archiveName, fileName);
                }
            }

            return dictionary;
        }

        private static uint GetHashCode(string filePath, uint prime = 37u)
        {
            if (string.IsNullOrEmpty(filePath))
                return 0u;
            return filePath.Replace('\\', '/')
                .ToLowerInvariant()
                .Aggregate(0u, (i, c) => i * prime + c);
        }

        public static string NormalizeFileName(string fileName)
        {
            foreach (var virtualRoot in _virtualRoots)
            {
                if (fileName.StartsWith(virtualRoot))
                {
                    fileName = fileName.Substring(virtualRoot.Length);
                    break;
                }
            }

            return fileName.Replace('/', '\\').TrimStart('\\');
        }
    }
}