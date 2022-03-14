using BinderTool.Core.Bhd5;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BinderTool
{
    public class FileSearch
    {
        public delegate IEnumerable<string> GetEnumerator();

        public static List<string> parallelSearch(Bhd5File header, string stub, GetEnumerator[] firstLevel, GetEnumerator[] rest)
        {
            List<string> ans = new List<string>();
            var tasks = new List<Task<List<string>>>();
            foreach (var gen in firstLevel) {
                List<GetEnumerator> gens = new List<GetEnumerator>() { gen };
                gens.AddRange(rest);
                var cs = new TaskCompletionSource<List<String>>();
                new Thread(() => {
                    var ans2 = search(header, stub, gens.ToArray());
                    cs.SetResult(ans2);
                }).Start();
                tasks.Add(cs.Task);
            }
            Task.WaitAll(tasks.ToArray());
            foreach (var task in tasks) {
                ans.AddRange(task.Result);
            }
            return ans;
        }

        public static List<string> search(Bhd5File header, string stub, GetEnumerator[] generators)
        {
            HashSet<ulong> hashes = new HashSet<ulong>();
            foreach (var bucket in header.GetBuckets()) {
                foreach (var entry in bucket.GetEntries()) {
                    hashes.Add(entry.FileNameHash);
                }
            }
            List<string> ans = new List<string>();
            var stack = new List<(string, IEnumerable<string>, int)> {
                (stub, generators[0](), 1)
            };
            while (stack.Count > 0) {
                var (soFar, gen, ind) = stack.Last();
                stack.RemoveAt(stack.Count - 1);
                var nextGen = generators.Length > ind ? generators[ind] : null;
                foreach (var c in gen) {
                    var next = soFar.Replace($"{{{ind - 1}}}", c);
                    if (nextGen != null) stack.Add((next, nextGen(), ind + 1));
                    else {
                        var hash = FileNameDictionary.GetHashCodeLong(next);
                        if (hashes.Contains(hash)) {
                            ans.Add(next);
                            Debug.WriteLine($"{hash} {next}");
                        }
                    }
                }
            }
            return ans;
        }

        public static GetEnumerator Range0Padded(int start, int count, int length)
        {
            return () => Enumerable.Range(start, count).Select(i => string.Format($"{{0:D{length}}}", i));
        }

        public static (string, GetEnumerator[]) data2MaphkxHkxbdtSearch = (
            "/map/m{0}/m{0}_{1}_{2}_{3}/h{0}_{1}_{2}_{3}.hkxbdt",
            new GetEnumerator[] {
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2)
            }
        );
        public static (string, GetEnumerator[]) data2MaphkxHkxbdtFSearch = (
            "/map/m{0}/m{0}_{1}_{2}_{3}/f{0}_{1}_{2}_{3}.hkxbdt",
            new GetEnumerator[] {
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2)
            }
        );
        public static (string, GetEnumerator[]) data2MaphkxHkxbdtLSearch = (
            "/map/m{0}/m{0}_{1}_{2}_{3}/l{0}_{1}_{2}_{3}.hkxbdt",
            new GetEnumerator[] {
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2)
            }
        );
        public static (string, GetEnumerator[]) data2MapRelationinfobndSearch = (
            "/map/m{0}/m{0}_{1}_{2}_{3}/m{0}_{1}_{2}_{3}.relationinfobnd.dcx",
            new GetEnumerator[] {
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2)
            }
        );
        public static (string, GetEnumerator[]) data2MapOnavSearch = (
            "/map/onav/m{0}_{1}_{2}_{3}.onav.dcx",
            new GetEnumerator[] {
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2)
            }
        );
        public static (string, GetEnumerator[]) data2MapMsbSearch = (
            "/map/mapstudio/m{0}_{1}_{2}_{3}.msb.dcx",
            new GetEnumerator[] {
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2)
            }
        );
        public static (string, GetEnumerator[]) data2MapMfrSearch = (
            "/map/m{0}/m{0}_{1}_{2}_{3}/m{0}_{1}_{2}_{3}.mfr.dcx",
            new GetEnumerator[] {
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2)
            }
        );
        public static (string, GetEnumerator[]) data2MapNvaSearch = (
            "/map/m{0}/m{0}_{1}_{2}_{3}/m{0}_{1}_{2}_{3}.nva.dcx",
            new GetEnumerator[] {
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2)
            }
        );
        public static (string, GetEnumerator[]) data2MapEnflSearch = (
            "/map/entryfilelist/{0}.entryfilelist",
            new GetEnumerator[] {
                () => Enumerable.Range(0, 1000000000).Select(i => string.Format("e{0:D6}/e{1:D10}", i / 10000, i))
            }
        ); 
        public static (string, GetEnumerator[]) data2MapiEnflSearch = (
            "/map/entryfilelist/{0}.entryfilelist",
            new GetEnumerator[] {
               () => Enumerable.Range(0, 1000000000).Select(i => string.Format("e{0:D6}/i{1:D10}", i / 10000, i))
            }
        );
        public static (string, GetEnumerator[], GetEnumerator[]) data2MapiEnflSearchParallel = (
            "/map/entryfilelist/{0}{1}.entryfilelist",
            new GetEnumerator[] {
                () => Enumerable.Range(0, 100000).Select(i => string.Format("e{0:D6}/i{0:D6}", i)),
                () => Enumerable.Range(100000, 100000).Select(i => string.Format("e{0:D6}/i{0:D6}", i)),
                () => Enumerable.Range(200000, 100000).Select(i => string.Format("e{0:D6}/i{0:D6}", i)),
                () => Enumerable.Range(300000, 100000).Select(i => string.Format("e{0:D6}/i{0:D6}", i)),
                () => Enumerable.Range(400000, 100000).Select(i => string.Format("e{0:D6}/i{0:D6}", i)),
                () => Enumerable.Range(500000, 100000).Select(i => string.Format("e{0:D6}/i{0:D6}", i)),
                () => Enumerable.Range(600000, 100000).Select(i => string.Format("e{0:D6}/i{0:D6}", i)),
                () => Enumerable.Range(700000, 100000).Select(i => string.Format("e{0:D6}/i{0:D6}", i)),
                () => Enumerable.Range(800000, 100000).Select(i => string.Format("e{0:D6}/i{0:D6}", i)),
                () => Enumerable.Range(900000, 100000).Select(i => string.Format("e{0:D6}/i{0:D6}", i)),
            },
            new GetEnumerator[] {
                () => Enumerable.Range(0, 10000).Select(i => string.Format("{0:D4}", i))
            }
        );
        public static (string, GetEnumerator[], GetEnumerator[]) data2MapeEnflSearchParallel = (
            "/map/entryfilelist/{0}{1}.entryfilelist",
            new GetEnumerator[] {
                () => Enumerable.Range(0, 100000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(100000, 100000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(200000, 100000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(300000, 100000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(400000, 100000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(500000, 100000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(600000, 100000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(700000, 100000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(800000, 100000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(900000, 100000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
            },
            new GetEnumerator[] {
                () => Enumerable.Range(0, 10000).Select(i => string.Format("{0:D4}", i))
            }
        );
        public static (string, GetEnumerator[]) data2MapEnvmapSearch(string extractedFolderPath)
        {

            return (
                "/map/{0}_envmap_{1}_{2}_{3}.tpfbnd.dcx",
                new GetEnumerator[] {
                    () => getMaps(extractedFolderPath).Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                    Range0Padded(0, 100, 2),
                    () => new string[] {"low", "middle", "high"},
                    Range0Padded(0, 100, 2)
                }
            );
        }
        public static (string, GetEnumerator[]) data2MapIvinfoSearch(string extractedFolderPath)
        {

            return (
                "/map/{1}_{0}.ivinfobnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"low", "middle", "high"},
                    () => getMaps(extractedFolderPath).Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                }
            );
        }
        public static (string, GetEnumerator[]) data2MapBtlSearch(string extractedFolderPath)
        {

            return (
                "/map/{0}_{1}.btl.dcx",
                new GetEnumerator[] {
                    () => getMaps(extractedFolderPath).Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                    Range0Padded(0, 10000, 4),
                }
            );
        }
        public static (string, GetEnumerator[]) data2MapFvbSearch(string extractedFolderPath)
        {

            return (
                "/map/{0}_{1}.fvb.dcx",
                new GetEnumerator[] {
                    () => getMaps(extractedFolderPath).Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                    Range0Padded(0, 10000, 4),
                }
            );
        }
        public static (string, GetEnumerator[]) data2MapNvmhktbndSearch(string extractedFolderPath)
        {

            return (
                "/map/{0}.nvmhktbnd.dcx",
                new GetEnumerator[] {
                    () => getMaps(extractedFolderPath).Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                }
            );
        }
        public static (string, GetEnumerator[]) data2MapMpwSearch(string extractedFolderPath)
        {

            return (
                "/map/{0}.mpw.dcx",
                new GetEnumerator[] {
                    () => getMaps(extractedFolderPath).Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                }
            );
        }
        public static (string, GetEnumerator[]) data2MapFlverSearch(string extractedFolderPath)
        {

            return (
                "/map/{0}.flver.dcx",
                new GetEnumerator[] {
                    () => getMaps(extractedFolderPath).Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                }
            );
        }
        public static (string, GetEnumerator[]) data2MapNvcSearch(string extractedFolderPath)
        {

            return (
                "/map/nvc/{0}_{1}.nvc.dcx",
                new GetEnumerator[] {
                    () => getMaps(extractedFolderPath),
                    () => getMaps(extractedFolderPath),
                }
            );
        }
        public static (string, GetEnumerator[]) data2MapBreakgeomSearch(string extractedFolderPath)
        {

            return (
                "/map/breakgeom/lod{0}/{1}_lod{0}.breakgeom.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 10, 1),
                    () => getMaps(extractedFolderPath),
                }
            );
        }
        public static (string, GetEnumerator[]) data2MaptpfCommonSearch(string extractedFolderPath)
        {

            return (
                "/map/{0}_CGrading.tpf.dcx",
                new GetEnumerator[] {
                    () => Enumerable.Range(0, 100).Select(i => string.Format("m{0:D2}", i)).Select(s => $"{s}/Common/{s}")
                }
            );
        }
        public static (string, GetEnumerator[]) data2MaptpfCommonSearch2(string extractedFolderPath)
        {

            return (
                "/map/{0}_{1}.tpf.dcx",
                new GetEnumerator[] {
                    () => Enumerable.Range(0, 100).Select(i => string.Format("m{0:D2}", i)).Select(s => $"{s}/Common/{s}"),
                    () => Enumerable.Range(0, 10000).Select(i => string.Format("m{0:D4}", i))
                }
            );
        }
        public static IEnumerable<string> getMaps(string extractedFolderPath)
        {
            return File.ReadLines("ERMaps.csv");
            /*List<string> maps = new List<string>();
            foreach (var folder1 in Directory.GetDirectories(extractedFolderPath)) {
                var dir = Path.GetFileName(folder1);
                if (Regex.IsMatch(dir, @"m\d\d_\d\d_\d\d_\d\d")) {
                    maps.Add(dir);
                    foreach (var folder2 in Directory.GetDirectories(folder1)) {
                        var dir2 = Path.GetFileName(folder2);
                        if (Regex.IsMatch(dir2, @"^m\d\d_\d\d_\d\d_\d\d$")) maps.Add(dir2);
                    }
                }
            }
            return maps;*/
        }
        public static (string, GetEnumerator[]) data2MapMapbndSearch(string extractedFolderPath)
        {
            List<string> maps = new List<string>();
            foreach (var folder1 in Directory.GetDirectories(extractedFolderPath)) {
                foreach (var folder2 in Directory.GetDirectories(folder1)) {
                    var dir = Path.GetFileName(folder2);
                    if (Regex.IsMatch(dir, @"m\d\d_\d\d_\d\d_\d\d_\d\d\d\d\d\d")) maps.Add(dir);
                    else if (Regex.IsMatch(dir, @"m\d\d_\d\d_\d\d_\d\d")) {
                        foreach (var folder3 in Directory.GetDirectories(folder2)) {
                            var dir2 = Path.GetFileName(folder3);
                            if (Regex.IsMatch(dir2, @"m\d\d_\d\d_\d\d_\d\d_\d\d\d\d\d\d")) maps.Add(dir2);
                        }
                    }
                }
            }
            return (
                "/map/{0}.mapbnd.dcx", 
                new GetEnumerator[] { () => maps.Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s) }
            );
        }
        public static void CreateMapList(string outputPath)
        {
            var lines = File.ReadAllLines("DictionaryER.csv");
            HashSet<string> maps = new HashSet<string>();
            foreach (var line in lines) {
                var m = Regex.Match(line, @"m\d\d_\d\d_\d\d_\d\d");
                if (m.Success) maps.Add(m.Value);
            }
            var ans = maps.ToArray();
            Array.Sort(ans);
            File.WriteAllLines(outputPath, ans);
        }
        public static void UpdateDictionary(IEnumerable<string> newFiles, string currPrefix, string newPrefix, string savePath)
        {
            var lines = File.ReadAllLines("DictionaryER.csv");
            HashSet<string> files = new HashSet<string>();
            foreach (var line in lines) {
                files.Add(line.Trim());
            }
            int added = 0;
            foreach (var file in newFiles) {
                if (files.Add(file.Replace(currPrefix, newPrefix))) added++;
            }
            var ans = files.ToArray();
            Array.Sort(ans);
            File.WriteAllLines(savePath, ans);
            Debug.WriteLine($"Added {added} to the dictionary");
        }
    }
}
