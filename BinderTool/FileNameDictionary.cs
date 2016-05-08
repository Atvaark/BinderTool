using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BinderTool
{
    public class FileNameDictionary
    {
        private static readonly string[] VirtualRoots = {
            @"N:\SPRJ\data\",
            @"N:\FDP\data\"
        };

        private static readonly string[] PhysicalRoots = {
            "capture",
            "data1",
            "data2",
            "data3",
            "data4",
            "data5",
            "system",
            "temp",
            "config",
            "debug",
            "debugdata",
            "dbgai",
            "parampatch",

            "chrhkx",
            "chrflver",
            "tpfbnd",
            "hkxbnd",
        };

        /// <example>
        ///     1. gparam:/m_template.gparam.dcx
        ///     2. data1:/param/drawparam/m_template.gparam.dcx
        ///     3. /param/drawparam/m_template.gparam.dcx
        /// </example>
        private static readonly Dictionary<string, string> SubstitutionMap = new Dictionary<string, string>
            {
                { "cap_breakobj", "capture:/breakobj" },
                { "cap_dbgsaveload", "capture:/dbgsaveload" },
                { "cap_debugmenu", "capture:/debugmenu" },
                { "cap_entryfilelist", "capture:/entryfilelist" },
                { "cap_envmap", "capture:/envmap" },
                { "cap_report", "capture:/fdp_report" },
                { "cap_gparam", "capture:/gparam" },
                { "cap_havok", "capture:/havok" },
                { "cap_log", "capture:/log" },
                { "cap_mapstudio", "capture:/mapstudio" },
                { "cap_memdump", "capture:/memdump" },
                { "cap_param", "capture:/param" },
                { "cap_screenshot", "capture:/screenshot" },

                { "title", "data1:/" },
                { "event", "data1:/event" },
                { "facegen", "data1:/facegen" },
                { "font", "data1:/font" },
                { "menu", "data1:/menu" },
                { "menuesd_dlc", "data1:/menu" },
                { "menutexture", "data1:/menu" },
                { "movie", "data1:/movie" },
                { "msg", "data1:/msg" },
                { "mtd", "data1:/mtd" },
                { "other", "data1:/other" },
                { "param", "data1:/param" },
                { "gparam", "data1:/param/drawparam" },
                { "regulation", "data1:/param/regulation" },
                { "paramdef", "data1:/paramdef" },
                { "remo", "data1:/remo" },
                { "aiscript", "data1:/script" },
                { "luascriptpatch", "data1:/script" },
                { "script", "data1:/script" },
                { "talkscript", "data1:/script/talk" },
                { "patch_sfxbnd", "data1:/sfx" },
                { "sfx", "data1:/sfx" },
                { "sfxbnd", "data1:/sfx" },
                { "shader", "data1:/shader" },
                { "fmod", "data1:/sound" },
                { "sndchr", "data1:/sound" },
                { "sndmap", "data1:/sound" },
                { "sndremo", "data1:/sound" },
                { "sound", "data1:/sound" },
                { "stayparamdef", "data1:/stayparamdef" },
                { "testdata", "data1:/testdata" },

                { "parts", "data2:/parts" },

                { "action", "data3:/action" },
                { "actscript", "data3:/action/script" },
                { "chr", "data3:/chr" },
                { "chranibnd", "data3:/chr" },
                { "chranibnd_dlc", "data3:/chr" },
                { "chrbnd", "data3:/chr" },
                { "chresd", "data3:/chr" },
                { "chresdpatch", "data3:/chr" },
                { "chrtpf", "data3:/chr" },

                { "obj", "data4:/obj" },
                { "objbnd", "data4:/obj" },

                { "map", "data5:/map" },
                { "maphkx", "data5:/map" },
                { "maptpf", "data5:/map" },
                { "patch_maptpf", "data5:/map" },
                { "breakobj", "data5:/map/breakobj" },
                { "entryfilelist", "data5:/map/entryfilelist" },
                { "mapstudio", "data5:/map/mapstudio" },
                { "onav", "data5:/map/onav" },

                { "adhoc", "debugdata:/adhoc" }
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

        private bool TrySplitFileName(string file, out string archiveName, out string fileName)
        {
            archiveName = null;
            fileName = null;

            int i = file.IndexOf(":/", StringComparison.Ordinal);
            if (i == -1)
            {
                return false;
            }

            archiveName = file.Substring(0, i);
            fileName = file.Substring(i + 2, file.Length - i - 2);

            return true;
        }

        private void Add(string file)
        {
            string archiveName;
            string fileName;
            if (!TrySplitFileName(file, out archiveName, out fileName))
            {
                return;
            }

            string substitutionArchiveName;
            if (!SubstitutionMap.TryGetValue(archiveName, out substitutionArchiveName))
            {
                if (!PhysicalRoots.Contains(archiveName))
                {
                    return;
                }

                substitutionArchiveName = archiveName + ":";
            }

            file = substitutionArchiveName + "/" + fileName;
            if (!TrySplitFileName(file, out archiveName, out fileName))
            {
                return;
            }

            string hashablePath = "/" + fileName;
            uint hash = GetHashCode(hashablePath);

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
                dictionary.Add(line);
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
            foreach (var virtualRoot in VirtualRoots)
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