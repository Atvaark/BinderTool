using BinderTool.Core;
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
    /// <summary>
    /// Code related to finding file names from their hashes. Currently only works for Elden Ring since it has 64-bit file hashes.
    /// Static fields of type FileSearch are premade searches, which are accessed using reflection - it makes for cleaner looking code to do it this way instead of in a dictionary.
    /// The way I made the searches was by browsing the "decompiled" code of the .exe in Ghidra and searching for strings containing the prefixes (e.g. "map:/").
    /// In Elden Ring at least, these file name strings are in UTF-16 - not UTF-8. There's a lot of using format() with %s/%0d/%c. Usually the general format
    /// of a %s can be recovered by browsing the nearby code. Due to the amount of virtual methods (methods in vtables), it's extremely difficult to recover
    /// which numbers may be used in the format strings, so the code generally just searches through every number in the possible range.
    /// </summary>
    public class FileSearch
    {
        /// <summary>
        /// Template string. For an enumerator of index <code>n</code>, every string of format <code>"{n}"</code> will be replaced with the value returned by the enumerator.
        /// </summary>
        public string template;
        /// <summary>
        /// Array of functions that return enumerators, which are then used to replace template placeholders in the template string.
        /// </summary>
        public GetEnumerator[] enumerators;
        /// <summary>
        /// Array of integers representing the number of values each enumerator will contain. May be an estimate, since the value is only used for writing progress to the console. A value of <code>0</code> indicates that the <code>.Count()</code> method should be used to find the count (useful for small enumerators).
        /// </summary>
        public ulong[] enumeratorLens;
        /// <summary>
        /// Array of functions that return enumerators, similar to <code>enumerators</code>. If null, indicates this field is not used and the search will be single-threaded. If non-null, each value of the array will be prepended to a copy of <code>enumerators</code> and a single-threaded search over the result will be run on a new thread.
        /// </summary>
        public GetEnumerator[] firstLevel;
        /// <summary>
        /// Array of integers representing the number of values each enumerator in <code>firstLevel</code> will contain. May be null if <code>firstLevel</code> is null. Functions the same as <code>enumeratorLens</code>.
        /// </summary>
        public ulong[] firstLevelLens;
        /// <summary>
        /// The prefix in the "hashable" file path that should be replaced with a filesystem root (e.g. "/map").
        /// </summary>
        public string currPrefix;
        /// <summary>
        /// The prefix currPrefix will be replaced with (e.g. "map:/").
        /// </summary>
        public string newPrefix;

        /// <summary>
        /// Creates a FileSearch that may be parallel. If it's not parallel, firstLevel must be null.
        /// </summary>
        /// <param name="template">Template string. For an enumerator of index <code>n</code>, every string of format <code>"{n}"</code> will be replaced with the value returned by the enumerator.</param>
        /// <param name="enumerators">Array of functions that return enumerators, which are then used to replace template placeholders in the template string.</param>
        /// <param name="enumeratorLens">Array of integers representing the number of values each enumerator will contain. May be an estimate, since the value is only used for writing progress to the console. A value of <code>0</code> indicates that the <code>.Count()</code> method should be used to find the count (useful for small enumerators).</param>
        /// <param name="currPrefix">The prefix in the "hashable" file path that should be replaced with a filesystem root (e.g. "/map").</param>
        /// <param name="newPrefix">The prefix currPrefix will be replaced with (e.g. "map:/").</param>
        /// <param name="firstLevel">Array of functions that return enumerators, similar to <code>enumerators</code>. If null, indicates this field is not used and the search will be single-threaded. If non-null, each value of the array will be prepended to a copy of <code>enumerators</code> and a single-threaded search over the result will be run on a new thread.</param>
        /// <param name="firstLevelLens">Array of integers representing the number of values each enumerator in <code>firstLevel</code> will contain. May be null if <code>firstLevel</code> is null. Functions the same as <code>enumeratorLens</code>.</param>
        public FileSearch(string template, GetEnumerator[] enumerators, ulong[] enumeratorLens, string currPrefix, string newPrefix, GetEnumerator[] firstLevel, ulong[] firstLevelLens)
        {
            this.template = template;
            this.firstLevel = firstLevel;
            this.firstLevelLens = firstLevelLens;
            this.currPrefix = currPrefix;
            this.newPrefix = newPrefix;
            this.enumerators = enumerators;
            this.enumeratorLens = enumeratorLens;
        }
        /// <summary>
        /// Creates a non-parallel FileSearch.
        /// </summary>
        /// <param name="template">Template string. For an enumerator of index <code>n</code>, every string of format <code>"{n}"</code> will be replaced with the value returned by the enumerator.</param>
        /// <param name="enumerators">Array of functions that return enumerators, which are then used to replace template placeholders in the template string.</param>
        /// <param name="enumeratorLens">Array of integers representing the number of values each enumerator will contain. May be an estimate, since the value is only used for writing progress to the console. A value of <code>0</code> indicates that the <code>.Count()</code> method should be used to find the count (useful for small enumerators).</param>
        /// <param name="currPrefix">The prefix in the "hashable" file path that should be replaced with a filesystem root (e.g. "/map")</param>
        /// <param name="newPrefix">The prefix currPrefix will be replaced with (e.g. "map:/")</param>
        public FileSearch(string template, GetEnumerator[] enumerators, ulong[] enumeratorLens, string currPrefix, string newPrefix) : this(template, enumerators, enumeratorLens, currPrefix, newPrefix, null, null) {}

        /// <summary>
        /// Utility type describing the generators for enumerators. We use a function that returns the enumerator instead of a static value
        /// for a few reasons, primarily 1. so the enumerator can be repeatedly constructed since it ust be used for each value of the previous-level enumerator,
        /// and 2. the operations involved in creating the enumerator may be expensive and include filesystem accesses which we want to do lazily.
        /// </summary>
        public delegate IEnumerable<string> GetEnumerator();

        /// <summary>
        /// Top-level Search method - this is one of two methods code outside this file should need, the other being SearchAndUpdate.
        /// This method handles single-threaded vs. parallel, progress output to the console, etc.
        /// </summary>
        /// <returns>A List containing file names whose hashes were in the provided HashSet</returns>
        public List<string> Search(HashSet<ulong> hashes)
        {
            if (firstLevel == null) {
                ulong total = 1;
                for (int i = 0; i < enumerators.Length; i++) {
                    if (enumeratorLens[i] == 0) total *= (ulong)enumerators[i]().Count();
                    else total *= enumeratorLens[i];
                }
                var pos = (Console.CursorLeft, Console.CursorTop);
                Action<ulong> updateNumDone = (numDone) => {
                    Console.SetCursorPosition(pos.CursorLeft, pos.CursorTop);
                    Console.WriteLine($"Searched {numDone}/{total} ({string.Format("{0:F2}", ((float)numDone / total * 100.0))}%)");
                };
                updateNumDone(0);
                var ans = Search(hashes, template, enumerators, updateNumDone);
                updateNumDone(total);
                return ans;
            } else {
                return ParallelSearch(hashes, template, firstLevel, firstLevelLens, enumerators, enumeratorLens);
            }
        }
        /// <summary>
        /// Convenience method that runs FileSearch.Search and then UpdateDictionary with the results - this is one of two methods code outside this file should need, the other being Search. 
        /// File names found that were not included in the dictionary are added to it, and the dictionary is re-written at savePath in code point (standard string sorting) order.
        /// </summary>
        /// <returns>A List containing file names whose hashes were in the provided HashSet</returns>
        public List<string> SearchAndUpdate(HashSet<ulong> hashes, string savePath)
        {
            var ans = this.Search(hashes);
            UpdateDictionary(ans, currPrefix, newPrefix, savePath);
            return ans;
        }

        /// <summary>
        /// Runs a parallel search with the given parameters.
        /// </summary>
        /// <returns>A List containing file names whose hashes were in the provided HashSet</returns>
        private static List<string> ParallelSearch(HashSet<ulong> hashes, string template, GetEnumerator[] firstLevel, ulong[] firstLevelLens, GetEnumerator[] rest, ulong[] enumeratorLens)
        {
            List<string> ans = new List<string>();
            var tasks = new List<Task<List<string>>>();
            var consoleMutex = new Mutex();
            var threadInd = 0;
            int left = Console.CursorLeft;
            int topStart = Console.CursorTop;
            for (int genInd = 0; genInd < firstLevel.Length; genInd++) {
                var gen = firstLevel[genInd];
                List<GetEnumerator> gens = new List<GetEnumerator>() { gen };
                gens.AddRange(rest);
                ulong total = firstLevelLens[genInd];
                if (total == 0) total = (ulong)gen().Count();
                for (int i = 0; i < rest.Length; i++) {
                    if (enumeratorLens[i] == 0) total *= (ulong)rest[i]().Count();
                    else total *= enumeratorLens[i];
                }
                int top = topStart + threadInd;
                var currInd = threadInd;
                threadInd++;
                Action<ulong> updateNumDone = (numDone) => {
                    consoleMutex.WaitOne();
                    Console.SetCursorPosition(left, top);
                    Console.WriteLine($"Thread {currInd} searched {numDone}/{total} ({(int)Math.Round((float)numDone / total * 100.0)}%)");
                    consoleMutex.ReleaseMutex();
                };
                updateNumDone(0);
                var cs = new TaskCompletionSource<List<String>>();
                new Thread(() => {
                    var ans2 = Search(new HashSet<ulong>(hashes), template, gens.ToArray(), updateNumDone);
                    cs.SetResult(ans2);
                    updateNumDone(total);
                }).Start();
                tasks.Add(cs.Task);
            }
            Task.WaitAll(tasks.ToArray());
            Console.SetCursorPosition(left, topStart + threadInd);
            Console.WriteLine("All threads finished");
            foreach (var task in tasks) {
                ans.AddRange(task.Result);
            }
            return ans;
        }

        /// <summary>
        /// Runs a single-threaded search with the given parameters.
        /// </summary>
        /// <returns>A List containing file names whose hashes were in the provided HashSet</returns>
        private static List<string> Search(HashSet<ulong> hashes, string template, GetEnumerator[] generators, Action<ulong> updateNumDone)
        {
            List<string> ans = new List<string>();
            var stack = new List<(string, IEnumerable<string>, int)> {
                (template, generators[0](), 1)
            };
            ulong numDone = 0;
            while (stack.Count > 0) {
                var (soFar, gen, ind) = stack.Last();
                stack.RemoveAt(stack.Count - 1);
                var nextGen = generators.Length > ind ? generators[ind] : null;
                foreach (var c in gen) {
                    var next = soFar.Replace($"{{{ind - 1}}}", c);
                    if (nextGen != null) stack.Add((next, nextGen(), ind + 1));
                    else {
                        var hash = FileNameDictionary.GetHashCodeLong(next);
                        numDone++;
                        if (numDone % 100_000 == 0) updateNumDone(numDone);
                        if (hashes.Contains(hash)) {
                            ans.Add(next);
                            Debug.WriteLine($"{hash} {next}");
                        }
                    }
                }
            }
            return ans;
        }

        /// <summary>
        /// Utility method that creates a GetEnumerator over a range of integers formatted as 0-padded strings of a given length.
        /// Extremely useful since there's a lot of 0-padded numbers in ER file names.
        /// </summary>
        public static GetEnumerator Range0Padded(int start, int count, int length)
        {
            return () => Enumerable.Range(start, count).Select(i => string.Format($"{{0:D{length}}}", i));
        }

        public static FileSearch data2MaphkxHkxbdtSearch = new FileSearch(
            "/map/m{1}/m{1}_{2}_{3}_{4}/{0}{1}_{2}_{3}_{4}.hkxbdt",
            new GetEnumerator[] {
                () => new string[] { "h", "f", "l" },
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2)
            },
            new ulong[] {
                100, 100, 100, 100
            },
            "/map",
            "maphkx:"
        );
        public static FileSearch data2MaphkxHkxbhdSearch = new FileSearch(
            "{0}.hkxbhd",
            new GetEnumerator[] {
                () => GetFilesHashable().Where(f => f.EndsWith(".hkxbdt")).Select(f => f.Replace(".hkxbdt", ".hkxbhd"))
            },
            new ulong[] {0},
            "/map",
            "maphkx:"
        );
        public static FileSearch data2MapRelationinfobndSearch = new FileSearch(
            "/map/m{0}/m{0}_{1}_{2}_{3}/m{0}_{1}_{2}_{3}.relationinfobnd.dcx",
            new GetEnumerator[] {
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2)
            },
            new ulong[] {100, 100, 100, 100},
            "/map",
            "map:"
        );
        public static FileSearch data2MapOnavSearch = new FileSearch(
            "/map/onav/m{0}_{1}_{2}_{3}.onav.dcx",
            new GetEnumerator[] {
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2)
            },
            new ulong[] { 100, 100, 100, 100 },
            "/map/onav",
            "onav:"
        );
        public static FileSearch data2MapMsbSearch = new FileSearch(
            "/map/mapstudio/m{0}_{1}_{2}_{3}.msb.dcx",
            new GetEnumerator[] {
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2)
            },
            new ulong[] { 100, 100, 100, 100 },
            "/map/mapstudio",
            "mapstudio:"
        );
        public static FileSearch data2MapMfrSearch = new FileSearch(
            "/map/m{0}/m{0}_{1}_{2}_{3}/m{0}_{1}_{2}_{3}.mfr.dcx",
            new GetEnumerator[] {
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2)
            },
            new ulong[] { 100, 100, 100, 100 },
            "/map",
            "map:"
        );
        public static FileSearch data2MapNvaSearch = new FileSearch(
            "/map/m{0}/m{0}_{1}_{2}_{3}/m{0}_{1}_{2}_{3}.nva.dcx",
            new GetEnumerator[] {
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2)
            },
            new ulong[] { 100, 100, 100, 100 },
            "/map",
            "map:"
        );
        public static FileSearch data2MapiEnflSearch = new FileSearch(
            "/map/entryfilelist/{0}{1}.entryfilelist",
            new GetEnumerator[] {
                () => Enumerable.Range(0, 10000).Select(i => string.Format("{0:D4}", i))
            },
            new ulong[] {
                10_000
            },
            "/map/entryfilelist",
            "entryfilelist:",
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
            new ulong[] {
                100_000,
                100_000,
                100_000,
                100_000,
                100_000,
                100_000,
                100_000,
                100_000,
                100_000,
                100_000,
            }
        );
        public static FileSearch data2MapEnflSearch = new FileSearch(
            "/map/entryfilelist/{0}{1}.entryfilelist",
            new GetEnumerator[] {
                () => Enumerable.Range(0, 10000).Select(i => string.Format("{0:D4}", i))
            },
            new ulong[] {
                10_000
            },
            "/map/entryfilelist",
            "entryfilelist:",
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
            new ulong[] {
                100_000,
                100_000,
                100_000,
                100_000,
                100_000,
                100_000,
                100_000,
                100_000,
                100_000,
                100_000,
            }
        ); 
        public static FileSearch data2MapEnflSearchFast = new FileSearch(
             "/map/entryfilelist/{0}{1}.entryfilelist",
             new GetEnumerator[] {
                () => Enumerable.Range(0, 10000).Select(i => string.Format("{0:D4}", i))
             },
             new ulong[] {
                10_000
             },
             "/map/entryfilelist",
             "entryfilelist:",
             new GetEnumerator[] {
                () => Enumerable.Range(0, 20000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(20000, 20000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(40000, 20000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(60000, 20000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(80000, 20000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(100000, 20000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(120000, 20000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(140000, 20000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(160000, 20000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
                () => Enumerable.Range(180000, 20000).Select(i => string.Format("e{0:D6}/e{0:D6}", i)),
             },
             new ulong[] {
                20_000,
                20_000,
                20_000,
                20_000,
                20_000,
                20_000,
                20_000,
                20_000,
                20_000,
                20_000,
             }
         );
        public static FileSearch data2MapEnvmapSearch = new FileSearch(
                "/map/{0}_envmap_{1}_{2}_{3}.tpfbnd.dcx",
                new GetEnumerator[] {
                    () => GetMaps().Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                    Range0Padded(0, 100, 2),
                    () => new string[] {"low", "middle", "high"},
                    Range0Padded(0, 100, 2)
                },
                new ulong[] {
                    1_00_00_00,
                    100,
                    3,
                    100
                },
                "/map",
                "map:"
            );
        public static FileSearch data2MapIvinfoSearch = new FileSearch(
                "/map/{1}_{0}.ivinfobnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"low", "middle", "high"},
                    () => GetMaps().Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                },
                new ulong[] {
                    100,
                    1_00_00_00
                },
                "/map",
                "map:"
            );
        public static FileSearch data2MapBtlSearch = new FileSearch(
                "/map/{0}_{1}.btl.dcx",
                new GetEnumerator[] {
                    () => GetMaps().Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                    Range0Padded(0, 10000, 4),
                },
                new ulong[] {
                    1_00_00_00,
                    10000
                },
                "/map",
                "map:"
            );
        public static FileSearch data2MapFvbSearch = new FileSearch(
                "/map/{0}_{1}.fvb.dcx",
                new GetEnumerator[] {
                    () => GetMaps().Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                    Range0Padded(0, 10000, 4),
                },
                new ulong[] {
                    1_00_00_00,
                    10000
                },
                "/",
                "map:"
            );
        public static FileSearch data2MapNvmhktbndSearch = new FileSearch(
                "/map/{0}.nvmhktbnd.dcx",
                new GetEnumerator[] {
                    () => GetMaps().Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                },
                new ulong[] {
                    1_00_00_00
                },
                "/map",
                "map:"
            );
        public static FileSearch data2MapMpwSearch = new FileSearch(
                "/map/{0}.mpw.dcx",
                new GetEnumerator[] {
                    () => GetMaps().Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                },
                new ulong[] {
                    1_00_00_00
                },
                "/map",
                "map:"
            );
        public static FileSearch data2MapFlverSearch = new FileSearch(
                "/map/{0}.flver.dcx",
                new GetEnumerator[] {
                    () => GetMaps().Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                },
                new ulong[] {
                    1_00_00_00
                },
                "/map",
                "map:"
            );
        public static FileSearch data2MapNvcSearch = new FileSearch(
                "/map/nvc/{0}_{1}.nvc.dcx",
                new GetEnumerator[] {
                    () => GetMaps(),
                    () => GetMaps(),
                },
                new ulong[] {
                    1_00_00_00,
                    1_00_00_00
                },
                "/map",
                "map:"
            );
        public static FileSearch data2MapBreakgeomSearch = new FileSearch(
                "/map/breakgeom/lod{0}/{1}_lod{0}.breakgeom.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 10, 1),
                    () => GetMaps(),
                },
                new ulong[] {
                    10,
                    1_00_00_00
                },
                "/map/breakgeom",
                "breakgeom:"
            );
        public static FileSearch data2MaptpfCommonSearch = new FileSearch(
                "/map/{0}_CGrading.tpf.dcx",
                new GetEnumerator[] {
                    () => Enumerable.Range(0, 100).Select(i => string.Format("m{0:D2}", i)).Select(s => $"{s}/Common/{s}")
                },
                new ulong[] {
                    100
                },
                "/map",
                "map:"
            );
        public static FileSearch data2MaptpfCommonSearch2 = new FileSearch(
                "/map/{0}_{1}.tpf.dcx",
                new GetEnumerator[] {
                    () => Enumerable.Range(0, 100).Select(i => string.Format("m{0:D2}", i)).Select(s => $"{s}/Common/{s}"),
                    () => Enumerable.Range(0, 10000).Select(i => string.Format("m{0:D4}", i))
                },
                new ulong[] {
                    100,
                    10000
                },
                "/map",
                "maptpf:"
            );
        public static FileSearch data0MapinfotexSearch = new FileSearch(
                "/other/mapinfotex/{0}.mapinfotexbnd.dcx",
                new GetEnumerator[] {
                    () => GetMaps(),
                },
                new ulong[] {
                    1_00_00_00
                },
                "/other/mapinfotex",
                "mapinfotex:"
            );
        public static FileSearch data0PckSearch = new FileSearch(
            "/sound/pck/{0}.pck",
            new GetEnumerator[] {
                () => new string[] {"normal", "nt", "first"}
            },
            new ulong[] {0},
            "/sound/pck",
            "wwise_pck:"
        );
        public static FileSearch data0OtherSearch = new FileSearch(
                "/other/{0}",
                new GetEnumerator[] {
                    () => new string[] {
                        "InGameStay.loadlist",
                        "InGameStay.loadlist.dcx",
                        "network/server_public_key",
                        "AutoInvadePoint.aipbnd",
                        "AutoInvadePoint.aipbnd.dcx",
                        "SysTex.tpf",
                        "SysTex.tpf.dcx",
                        "ModelViewer_Default_rem.dds",
                        "ModelViewer_Default_iem.dds",
                        "SysEnvTex.tpf",
                        "SysEnvTex.tpf.dcx",
                        "decalTex.tpf",
                        "decalTex.tpf.dcx",
                        "MovTae.movtae",
                        "MovTae.movtae.dcx",
                    }
                },
                new ulong[] {
                    0
                },
                "/other",
                "other:"
            );
        public static FileSearch data0MaterialSearch = new FileSearch(
                "/material/{1}{0}",
                new GetEnumerator[] {
                    () => new string[] {"", ".dcx"},
                    () => new string[] {
                        "AllMaterial.matbinbnd.devpatch",
                        "AllMaterial.matbinbnd",
                        "SpeedTree.matbinbnd"
                    }
                },
                new ulong[] {
                    0, 0
                },
                "/material",
                "material:"
            );
        public static FileSearch data0ShaderSearch = new FileSearch(
                "/shader/{1}{0}",
                new GetEnumerator[] {
                    () => new string[] {"", ".dcx"},
                    () => new string[] {
                        "PipelineStateCache.dat",
                        "GXShader.shaderbnd",
                        "GXRendererShader.shaderbnd",
                        "GXGui.shaderbnd",
                        "GXFlverShader.shaderbnd",
                        "GXFfxShader.shaderbnd",
                        "GXDecal.shaderbnd",
                        "GXPostEffect.shaderbnd",
                        "GXRayTracing.shaderbnd",
                        "Shaderbdle.shaderbdlebnd.devpatch",
                        "Shaderbdle.shaderbdlebnd",
                        "Shaderbdle_[RT].shaderbdlebnd",
                        "Grass.shaderbnd",
                        "SpeedTree.shaderbdlebnd",
                        "SpeedTree_[RT].shaderbdlebnd"
                    }
                },
                new ulong[] {
                    0, 0
                },
                "/shader",
                "shader:"
            );
        public static FileSearch data0FontSearch = new FileSearch(
                "/font/{1}/font.gfx{0}",
                new GetEnumerator[] {
                    () => new string[] {"", ".dcx"},
                    () => new string[] {
                        "MatisseProN",
                        "EU_Std",
                        "Hangul_std",
                        "TraditionalChinese",
                        "SimplifiedChinese",
                        "Thai",
                        "Arabic",
                        "TsukuMinProSub",
                        "EU_Map",
                        "Hangul_Map"
                    }
                },
                new ulong[] {
                    0, 0
                },
                "/font",
                "font:/"
            );
        public static FileSearch data0ActionSearch = new FileSearch(
                "/action/{1}{0}",
                new GetEnumerator[] {
                    () => new string[] {"", ".dcx"},
                    () => new string[] {
                        "eventNameId.txt",
                        "variableNameId.txt",
                        "stateNameId.txt",
                        "script/c9997.hks",
                        "script/common_define.hks",
                        "script/modifier.hks",
                        "script/c0000_talk.hks"
                    }
                },
                new ulong[] {
                    0, 0
                },
                "/action",
                "action:"
            );
        public static FileSearch data0ActscriptSearch = new FileSearch(
                "/action/script/c{1}.hks{0}",
                new GetEnumerator[] {
                    () => new string[] {"", ".dcx"},
                    Range0Padded(0, 10_000, 4)
                },
                new ulong[] {
                    0, 10_000
                },
                "/action/script",
                "actscript:"
            );
        public static FileSearch data0TalkscriptSearch = new FileSearch(
                "/script/talk/m{0}_{1}_{2}_{3}.talkesdbnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2)
                },
                new ulong[] {
                    100, 100, 100, 100
                },
                "/script/talk",
                "talkscript:"
            );
        public static FileSearch data0AiscriptSearch = new FileSearch(
                "/script/m{0}_{1}_{2}_{3}.luabnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2)
                },
                new ulong[] {
                    100, 100, 100, 100
                },
                "/script",
                "aiscript:"
            );
        public static FileSearch data0AiscriptSearch2 = new FileSearch(
                "/script/{1}_{0}.luabnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"logic", "battle"},
                    Range0Padded(0, 1000000, 6),
                },
                new ulong[] {
                    1_000_000
                },
                "/script",
                "aiscript:"
            );
        public static FileSearch data0MsgSearch = new FileSearch(
                "/msg/{1}/{0}.msgbnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"item", "menu", "ngword", "sellregion"},
                    () => new string[] {
                        "jpnJP", "engUS", "fraFR", "spaES", "itaIT", "deuDE", "korKR", "zhoTW",
                        "zhoCN", "polPL", "rusRU", "porBR", "spaAR", "thaTH", "areAE", "JP",
                        "NA", "EU", "AS", "UK"
                    }
                },
                new ulong[] {
                    0, 0
                },
                "/msg",
                "msg:"
            );
        public static FileSearch data0ParamSearch = new FileSearch(
                "/param/{0}.parambnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {
                        "systemparam/systemparam",
                        "GameParam/GameParam",
                        "EventParam/EventParam"
                    }
                },
                new ulong[] {
                    0
                },
                "/param",
                "param:"
            );
        public static FileSearch data0GparamSearch = new FileSearch(
                "/param/drawparam/{1}.gparam{0}",
                new GetEnumerator[] {
                    () => new string[] {"", ".dcx"},
                    () => new string[] {
                        "",
                        "default",
                        "M_template",
                        "S_template",
                        "MapBase",
                        "empty",
                        "m_00_00_0000",
                        "MapOverride_forRemo",
                        "RemoBase",
                        "s_00_00_0000",
                        "CutsceneWeatherOverrideDefault",
                        "s_00_00_0000_CustceneWeatherOverride_00",
                        "MDLVW",
                        "IvInfo"
                    }
                },
                new ulong[] {
                    0, 0
                },
                "/param/drawparam",
                "gparam:"
            );
        public static FileSearch data0GparamSearch2 = new FileSearch(
                "/param/drawparam/s{0}_{1}_{2}.gparam.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 10000, 4)
                },
                new ulong[] {
                    100, 100, 10_000
                },
                "/param/drawparam",
                "gparam:"
            );
        public static FileSearch data0GparamSearch3 = new FileSearch(
                "/param/drawparam/m{1}_{2}_{3}{0}.gparam.dcx",
                new GetEnumerator[] {
                    () => new string[] {"", "_WeatherBase", "_WeatherOutdoor", "_CommonEvent", "_CommonEventMapUnique"},
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 10000, 4)
                },
                new ulong[] {
                    100, 100, 10_000
                },
                "/param/drawparam",
                "gparam:"
            );
        public static FileSearch data0GparamSearch4 = new FileSearch(
                "/param/drawparam/s{0}_{1}_{2}_WeatherOverride_{3}.gparam.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 10000, 4),
                    Range0Padded(0, 100, 2)
                },
                new ulong[] {
                    100, 100, 10_000, 100
                },
                "/param/drawparam",
                "gparam:"
            );
        public static FileSearch data0EventSearch = new FileSearch(
                "/event/{1}{0}",
                new GetEnumerator[] {
                    () => new string[] {"", ".dcx"},
                    () => new string[] {
                        "eventflag/LegacyMap.eventflagalloclist",
                        "eventflag/OpenMap.eventflagalloclist",
                        "common_func.emevd",
                        "common.emevd"
                    }
                },
                new ulong[] {
                    0, 0
                },
                "/event",
                "event:"
            );
        public static FileSearch data0EventSearch2 = new FileSearch(
                "/event/m{0}_{1}_{2}_{3}.emevd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2)
                },
                new ulong[] {
                    100, 100, 100, 100
                },
                "/event",
                "event:"
            );
        public static FileSearch data0EventSearch3 = new FileSearch(
                "/event/m{0}.emevd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2)
                },
                new ulong[] {
                    100
                },
                "/event",
                "event:"
            );
        public static FileSearch data0MenuSearch = new FileSearch(
                "/menu/{1}{0}",
                new GetEnumerator[] {
                    () => new string[] {"", ".dcx"},
                    () => new string[] {
                        "71_MapTile.mtmskbnd"
                    }
                },
                new ulong[] {
                    0, 0
                },
                "/menu",
                "menu:"
            );
        public static FileSearch data0MenuSearch2 = new FileSearch(
                "/menu/{0}{1}{2}.sblytbnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"", "Hi/", "Low/"},
                    () => new string[] {
                        "", "jpnJP/", "engUS/", "fraFR/", "spaES/", "itaIT/", "deuDE/", "korKR/", "zhoTW/",
                        "zhoCN/", "polPL/", "rusRU/", "porBR/", "spaAR/", "thaTH/", "areAE/", "JP/",
                        "NA/", "EU/", "AS/", "UK/"
                    },
                    () => new string[] {
                        "00_Solo",
                        "01_Common",
                        "02_Title",
                        "03_ChrMake",
                        "04_NowLoading",
                        "05_Dummy",
                        "06_Platform",
                        "71_MapTile",
                        "80_Language"
                    }
                },
                new ulong[] {
                    0, 0, 0
                },
                "/menu",
                "menu:"
            );
        public static FileSearch data0MenuSearch3 = new FileSearch(
                "/menu/{1}{2}{3}.{0}",
                new GetEnumerator[] {
                    () => new string[] {"tpf.dcx", "tpfbhd"},
                    () => new string[] {"", "Hi/", "Low/"},
                    () => new string[] {
                        "", "jpnJP/", "engUS/", "fraFR/", "spaES/", "itaIT/", "deuDE/", "korKR/", "zhoTW/",
                        "zhoCN/", "polPL/", "rusRU/", "porBR/", "spaAR/", "thaTH/", "areAE/", "JP/",
                        "NA/", "EU/", "AS/", "UK/"
                    },
                    () => new string[] {
                        "00_Solo",
                        "01_Common",
                        "02_Title",
                        "03_ChrMake",
                        "04_NowLoading",
                        "05_Dummy",
                        "06_Platform",
                        "71_MapTile",
                        "80_Language"
                    }
                },
                new ulong[] {
                    0, 0, 0, 0
                },
                "/menu",
                "menu:"
            );
        public static FileSearch data0MenuSearch4 = new FileSearch(
                "/menu/MapImage/{0}{1}.tpf.dcx",
                new GetEnumerator[] {
                    () => Enumerable.Range(0, byte.MaxValue).Select(c => ""+(char)c),
                    () => Enumerable.Range(0, byte.MaxValue).Select(c => ""+(char)c),
                },
                new ulong[] {
                    255, 255
                },
                "/menu",
                "menu:"
            );
        public static FileSearch data0PartsSearch = new FileSearch(
                "/parts/{1}{2}_{3}_{4}{0}.partsbnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"", "_l"},
                    () => Enumerable.Range('a', 26).Select(c => ""+(char)c),
                    () => Enumerable.Range('a', 26).Select(c => ""+(char)c),
                    () => Enumerable.Range('a', 26).Select(c => ""+(char)c),
                    Range0Padded(0, 10000, 4)
                },
                new ulong[] {
                    26, 26, 26, 10_000
                },
                "/parts",
                "parts:"
            );
        public static FileSearch data0FacegenSearch = new FileSearch(
                "/facegen/{0}.fgbnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"FaceGen"}
                },
                new ulong[] {
                    0
                },
                "/facegen",
                "facegen:"
            );
        public static FileSearch data0CutsceneSearch = new FileSearch(
                "/cutscene/s{0}_{1}_{2}.cutscenebnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 10000, 4)
                },
                new ulong[] {
                    100, 100, 10_000
                },
                "/cutscene",
                "cutscenebnd:"
            );
        public static FileSearch data0CutsceneSearch2 = new FileSearch(
                "/cutscene/{0}_{1}.tpfbnd.dcx",
                new GetEnumerator[] {
                    () => GetFilesHashable().Select(f => {
                        var m = Regex.Match(f, @"(s\d\d_\d\d_\d\d\d\d).cutscenebnd");
                        if (m.Success) return m.Groups[1].Value;
                        return null;
                    }).Where(f => f != null),
                    Range0Padded(0, 100, 2),
                },
                new ulong[] {
                    0, 100
                },
                "/cutscene",
                "cutscenebnd:"
            );
        public static FileSearch data0MovieSearch = new FileSearch(
                "/movie/{1}.bk2{0}",
                new GetEnumerator[] {
                    () => new string[] {"", ".dcx"},
                    Range0Padded(0, 100000000, 8),
                },
                new ulong[] {
                    0, 1_0000_0000
                },
                "/movie",
                "movie:"
            );
        public static FileSearch data0WwiseSearch = new FileSearch(
                "/sound/{0}.mobnkinfo",
                new GetEnumerator[] {
                    () => new string[] {"SoundbanksInfo"}
                },
                new ulong[] {
                    0
                },
                "/sound",
                "wwise_mobnkinfo:"
            );
        public static FileSearch data0WwiseSearch2 = new FileSearch(
                "/sound/{0}{1}.moaeibnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"", "enus/"},
                    () => new string[] {"AdditionalEventInfo"}
                },
                new ulong[] {
                    0, 0
                },
                "/sound",
                "wwise_moaeibnd:"
            );
        public static FileSearch data0SfxbndSearch = new FileSearch(
                "/sfx/SfxBnd_c{0}.ffxbnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 10000, 4)
                },
                new ulong[] {
                    10_000
                },
                "/sfx",
                "sfxbnd:"
            );
        public static FileSearch data0SfxbndSearch2 = new FileSearch(
            "/sfx/SfxBnd_m{0}.ffxbnd.dcx",
            new GetEnumerator[] {
                Range0Padded(0, 100, 2)
            },
            new ulong[] {
                100
            },
            "/sfx",
            "sfxbnd:"
        );
        public static FileSearch data0SfxbndSearch3 = new FileSearch(
            "/sfx/SfxBnd_m{0}_{1}_{2}_{3}.ffxbnd.dcx",
            new GetEnumerator[] {
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2),
                Range0Padded(0, 100, 2)
            },
            new ulong[] {
                100, 100, 100, 100
            },
            "/sfx",
            "sfxbnd:"
        );
        public static FileSearch data0SfxbndSearch4 = new FileSearch(
            "/sfx/{0}.ffxbnd.dcx",
            new GetEnumerator[] {
                () => new string[] {"SfxBnd_CommonEffects"}
            },
            new ulong[] {
                0
            },
            "/sfx",
            "sfxbnd:"
        );
        public static FileSearch data0SfxbndSearch5 = new FileSearch(
            "/sfx/SfxBnd_mimic{0}{1}.ffxbnd.dcx",
            new GetEnumerator[] {
                Range0Padded(0, 100_000_000, 8)
            },
            new ulong[] {
                100_000_000
            },
            "/sfx",
            "sfxbnd:",
            Enumerable.Range(0, 10).Select<int, GetEnumerator>(i => () => new string[] { i.ToString() }).ToArray(),
            new ulong[] {
                1, 1, 1, 1, 1, 1, 1, 1, 1, 1
            }
        );
        public static FileSearch data0SfxbndDevpatchSearch = new FileSearch(
            "{0}.devpatch.dcx",
            new GetEnumerator[] {
                () => GetFilesHashable().Where(f => f.StartsWith("/sfx") && f.EndsWith(".ffxbnd.dxc")).Select(f => f.Replace(".dcx", ""))
            },
            new ulong[] {
                0
            },
            "/sfx",
            "patch_sfxbnd:"
        );
        public static FileSearch data1AetTpfSearch = new FileSearch(
            "/asset/aet/aet{1}/aet{1}_{2}{0}.tpf.dcx",
            new GetEnumerator[] {
                () => new string[] {"", "_l", "_billboards", "_billboards_l"},
                Range0Padded(0, 1000, 3),
                Range0Padded(0, 1000, 3)
            },
            new ulong[] {
                0, 1000, 1000
            },
            "/asset",
            "asset:"
        );
        public static FileSearch data1AegGeombndSearch = new FileSearch(
                "/asset/aeg/aeg{0}/aeg{0}_{1}.geombnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 1000, 3),
                    Range0Padded(0, 1000, 3)
                },
                new ulong[] {
                    1000, 1000
                },
                "/asset",
                "asset:"
            );
        public static FileSearch data1AegGeomhkxbndSearch = new FileSearch(
                "/asset/aeg/aeg{1}/aeg{1}_{2}_{0}.geomhkxbnd.dcx",
                new GetEnumerator[] {
                    () => new string[] { "h", "l" },
                    Range0Padded(0, 1000, 3),
                    Range0Padded(0, 1000, 3)
                },
                new ulong[] {
                    0, 1000, 1000
                },
                "/asset",
                "asset:"
            );
        public static FileSearch data3ChrChrbndSearch = new FileSearch(
                "/chr/c{0}.chrbnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 10000, 4),
                },
                new ulong[] {
                    10_000
                },
                "/chr",
                "chr:"
            );
        public static FileSearch data3ChrAnibndSearch = new FileSearch(
                "/chr/c{0}.anibnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 10000, 4),
                },
                new ulong[] {
                    10_000
                },
                "/chr",
                "chranibnd:"
            );
        public static FileSearch data3ChrAnibndSearch2 = new FileSearch(
                "/chr/c0000_a{0}.anibnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"00_lo", "00_md", "00_hi", "0x", "1x", "2x", "3x", "4x", "5x", "6x", "7x", "8x", "9x"},
                },
                new ulong[] {
                    0
                },
                "/chr",
                "chranibnd:"
            );
        public static FileSearch data3ChrAnibndSearch3 = new FileSearch(
                "/chr/c{1}_div{0}.anibnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 10000, 4),
                },
                new ulong[] {
                    100, 10_000
                },
                "/chr",
                "chranibnd:"
            );
        public static FileSearch data3ChrBehbndSearch = new FileSearch(
                "/chr/c{0}.behbnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 10000, 4),
                },
                new ulong[] {
                    10_000
                },
                "/chr",
                "chrbehbnd:"
            );
        public static FileSearch data3ChrTexbndSearch = new FileSearch(
                "/chr/c{1}_{0}.texbnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"h", "l"},
                    Range0Padded(0, 10000, 4),
                },
                new ulong[] {
                    0, 10_000
                },
                "/chr",
                "chrtexbnd:"
            );
        public static FileSearch sdSearch = new FileSearch(
                "/{0}cs_c{1}.bnk",
                new GetEnumerator[] {
                    () => new string[] {"", "enus/"},
                    Range0Padded(0, 10000, 4),
                },
                new ulong[] {
                    0, 10_000
                },
                "/",
                "wwise:/"
            );
        public static FileSearch sdSearch2 = new FileSearch(
                "/aeg{0}_{1}.bnk",
                new GetEnumerator[] {
                    Range0Padded(0, 1000, 3),
                    Range0Padded(0, 1000, 3),
                },
                new ulong[] {
                    1000, 1000
                },
                "/",
                "wwise:/"
            );
        public static FileSearch sdSearch3 = new FileSearch(
                "/cs_{0}m{1}{2}.bnk",
                new GetEnumerator[] {
                    () => new string[] {"", "s"},
                    Range0Padded(0, 100, 2),
                    () => (new string[] { "" }).AsEnumerable().Union(Range0Padded(0, 1000, 3)().Select(s => "_"+s)),
                },
                new ulong[] {
                    0, 100, 1001
                },
                "/",
                "wwise:/"
            );
        public static FileSearch sdSearch4 = new FileSearch(
                "/{0}.bnk",
                new GetEnumerator[] {
                    () => new string[] {"cs_main", "cs_smain", "init", "vc700", "enus/vcmain"},
                },
                new ulong[] {
                    0
                },
                "/enus",
                "wwise:/enus"
            );
        public static FileSearch sdSearch5 = new FileSearch(
                "/{0}s{1}_{2}_{3}.bnk",
                new GetEnumerator[] {
                    () => new string[] {"", "enus/"},
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 10000, 4),
                },
                new ulong[] {
                    0, 100, 100, 10_000
                },
                "/enus",
                "wwise:/enus"
            );
        public static FileSearch sdSearch6 = new FileSearch(
                "/enus/vc{0}.bnk",
                new GetEnumerator[] {
                    Range0Padded(0, 1000, 3),
                },
                new ulong[] {
                    1000
                },
                "/enus",
                "wwise:/enus"
            );

        public static FileSearch pscSearch = new FileSearch(
                "/shader/PipelineStateCache{0}.dat.dcx",
                new GetEnumerator[] {
                    () => new string[] {"_Phantom", "_[RT]", "_[RT]_Phantom"},
                },
                new ulong[] {
                    0
                },
                "/shader",
                "shader:"
            );
        public static FileSearch sdSearch7 = new FileSearch(
            "/enus/wem/{0}/{0}{1}.wem",
            new GetEnumerator[] {
                () => Enumerable.Range(0, 100000000).SelectMany(i => new string[] {i.ToString(), string.Format("{0:D8}", i) })
            },
            new ulong[] { 200000000 },
            "/enus",
            "wwise:/enus",
            new GetEnumerator[] {
                () => Enumerable.Range(0, 10).Select(i => "0"+i),
                () => Enumerable.Range(10, 10).Select(i => i.ToString()),
                () => Enumerable.Range(20, 10).Select(i => i.ToString()),
                () => Enumerable.Range(30, 10).Select(i => i.ToString()),
                () => Enumerable.Range(40, 10).Select(i => i.ToString()),
                () => Enumerable.Range(50, 10).Select(i => i.ToString()),
                () => Enumerable.Range(60, 10).Select(i => i.ToString()),
                () => Enumerable.Range(70, 10).Select(i => i.ToString()),
                () => Enumerable.Range(80, 10).Select(i => i.ToString()),
                () => Enumerable.Range(90, 10).Select(i => i.ToString()),
            },
            new ulong[] { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 }
        );
        public static FileSearch sdSearch8 = new FileSearch(
            "/wem/{0}/{0}{1}.wem",
            new GetEnumerator[] {
                () => Enumerable.Range(0, 100000000).SelectMany(i => new string[] {i.ToString(), string.Format("{0:D8}", i) })
            },
            new ulong[] { 200000000 },
            "/wem",
            "wwise:/wem",
            new GetEnumerator[] {
                () => Enumerable.Range(0, 10).Select(i => "0"+i),
                () => Enumerable.Range(10, 10).Select(i => i.ToString()),
                () => Enumerable.Range(20, 10).Select(i => i.ToString()),
                () => Enumerable.Range(30, 10).Select(i => i.ToString()),
                () => Enumerable.Range(40, 10).Select(i => i.ToString()),
                () => Enumerable.Range(50, 10).Select(i => i.ToString()),
                () => Enumerable.Range(60, 10).Select(i => i.ToString()),
                () => Enumerable.Range(70, 10).Select(i => i.ToString()),
                () => Enumerable.Range(80, 10).Select(i => i.ToString()),
                () => Enumerable.Range(90, 10).Select(i => i.ToString()),
            },
            new ulong[] { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 }
        );
        public static FileSearch data0MenuSearch5 = new FileSearch(
                "/menu/{0}{1}.gfx",
                new GetEnumerator[] {
                    () => new string[] {
                        "", "Win/"
                    },
                    () => new string[] {
                        "01_900_Black",
                        "01_080_EmergencyNotice",
                        "01_090_SummonMessage",
                        "01_910_Fade",
                        "01_920_Movie",
                        "01_930_KeyGuide",
                        "06_000_TermOfService_BNE",
                        "05_903_Warn_IllegalCopy",
                        "05_900_Logo_FromSoft",
                        "05_901_Logo_BNE",
                        "05_905_Logo_Copyright",
                        "01_010_MessageBox",
                        "01_012_ItemQuantitySelect",
                        "01_014_ItemQuantitySelect_Recipe",
                        "04_000_ChrMake_Top",
                        "04_010_ChrMake_CommandList",
                        "04_021_ChrMake_TextSelect_center",
                        "04_030_ChrMake_ColorSelect",
                        "04_031_ChrMake_ColorEditor",
                        "04_040_ChrMake_MultiSlider",
                        "04_050_ChrMake_IconGrid",
                        "04_060_ChrMake_ChildCommandList",
                        "04_100_ChrMake_BG",
                        "04_200_ChrMake_BaseChrSelect",
                        "01_000_FE",
                        "01_001_FE_Soul",
                        "01_002_FE_SaveIcon",
                        "01_011_MessageBox_Small",
                        "01_013_MessageBox_Small_NB",
                        "01_060_Caption",
                        "01_070_CommandList",
                        "01_071_CommandList2",
                        "02_020_Inventory",
                        "02_000_IngameTop",
                        "02_010_EquipTop",
                        "02_011_Equip",
                        "02_012_SoulForging",
                        "02_031_NumSelect",
                        "02_032_SortMenu",
                        "02_030_Inventory_CommandList",
                        "02_045_PC_CommandList",
                        "02_033_IconHelp",
                        "02_060_Kick",
                        "03_000_ShopTop",
                        "03_001_Shop",
                        "03_002_SPExchangeShop",
                        "03_010_LevelUp",
                        "02_080_Warp",
                        "01_020_Sign",
                        "01_030_BloodMessage",
                        "01_031_BloodMessage_Top",
                        "01_032_BloodMessage_Edit",
                        "01_034_BloodMessage_WriteList",
                        "01_035_BloodMessage_ReadList",
                        "01_050_GraveMessage",
                        "03_020_Spell",
                        "01_040_GestureTop",
                        "01_041_GestureRepository",
                        "03_050_ItemBox",
                        "02_070_Status",
                        "03_041_EnhancedWeaponSelect",
                        "02_100_AllocationOfEstus",
                        "02_110_DetailKeyGuide",
                        "03_000_ShopTop",
                        "03_063_TalkChoice",
                        "03_070_UseWarpItem1",
                        "03_071_UseWarpItem2",
                        "05_000_Title",
                        "05_001_Title_Logo",
                        "05_010_ProfileSelect",
                        "05_020_TitleInformation",
                        "05_040_TermOfService",
                        "02_904_NowLoading3",
                        "02_903_NowLoading2",
                        "02_990_TextInput",
                        "02_991_TextInput2",
                        "02_050_DetailStatus_Player",
                        "02_058_DetailStatus_Player_2P",
                        "02_051_DetailStatus_Item",
                        "02_052_DetailStatus_Armor",
                        "02_053_DetailStatus_Spell",
                        "02_054_DetailStatus_Arrow",
                        "02_055_DetailStatus_Weapon1",
                        "03_030_Comparison_equip_Weapon1",
                        "03_043_EnhancedWeapon_Status",
                        "03_032_Comparison_equip_Armor",
                        "03_033_Comparison_equip_Arrow",
                        "03_034_Comparison_equip_Buddy",
                        "02_057_ItemDetailText",
                        "02_056_DetailStatus_Recipe",
                        "02_059_DetailStatus_Elixir",
                        "02_049_DetailStatus_Gem",
                        "02_048_DetailStatus_Buddy",
                        "02_040_OptionSetting",
                        "05_031_NewGame_BrightnessSetting",
                        "05_032_NewGame_ControlSetting",
                        "02_042_PC_GraphicSetting",
                        "02_044_PC_TextSelect",
                        "02_046_BrightnessSetting",
                        "02_160_KeyConfiguration",
                        "02_150_Network",
                        "02_151_SignRankNetwork",
                        "02_152_NetworkKeywordSetting",
                        "02_120_WorldMap",
                        "02_121_WorldMap_MemoList",
                        "02_122_WorldMap_WarpList",
                        "02_130_Tutorial_Modal",
                        "02_131_Tutorial_Toast",
                        "01_100_Clock",
                        "05_050_StaffRoll",
                        "05_800_Trial_CharacterSelect",
                        "02_043_PC_KeyConfiguration",
                        "font"
                    }
                },
                new ulong[] {
                    0, 0
                },
                "/menu",
                "menu:"
            );
        public static FileSearch devpatchSearch = new FileSearch(
                "{0}",
                new GetEnumerator[] {
                    () => GetFilesHashable()
                    .SelectMany(f => f.EndsWith(".dcx") ? new string[] {f, f.Replace(".dcx", "")} : new string[] {f})
                    .SelectMany(f => new string[] {f+".devpatch", f+".devpatch.dcx"})
                },
                new ulong[] {
                    0
                },
                "/",
                ""
            );
        /// <summary>
        /// Returns an enumeration of the map names provided in ERMaps.csv
        /// </summary>
        public static IEnumerable<string> GetMaps()
        {
            return File.ReadLines("ERMaps.csv");
        }
        /// <summary>
        /// Returns an enumeration over "hashable" (have the roots e.g. "map:/" replaced with their values e.g. "/map") file names contained in DictionaryER.csv
        /// </summary>
        public static IEnumerable<string> GetFilesHashable()
        {
            return GetFiles().Select(FileNameDictionary.MakeHashable);
        }
        /// <summary>
        /// Returns an enumeration over the file names contained in DictionaryER.csv. 
        /// Note that if these are hashed, they will not match up with the hashes in .bhd files since they are stored with root aliases (e.g. "map:/") which are not used in the .bhd hashes.
        /// </summary>
        public static IEnumerable<string> GetFiles()
        {
            return File.ReadLines("DictionaryER.csv");
        }

        /// <summary>
        /// Creates a list of map names (all strings of format "mXX_XX_XX_XX" where Xs may be any number) in the current DictionaryER.csv.
        /// The list is written to <code>outputPath</code>.
        /// </summary>
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
        /// <summary>
        /// Creates a binary .hashlist file which lists all file name hashes in the given .bhd files.
        /// The .hashlist file is a single u64 length which gives the number of hashes, followed by those hashes in binary u64 format.
        /// The .hashlist file is used because it's much faster than decrypting and parsing one or more .bhd files every time you want to run a file search.
        /// </summary>
        /// <param name="outputPath">The location to write the .hashlist</param>
        /// <param name="bhds">A list of .bhd file paths to read hashes from</param>
        /// <returns>The number of unique hashes</returns>
        public static int CreateHashList(string outputPath, params string[] bhds)
        {
            HashSet<ulong> hashes = new HashSet<ulong>();
            foreach (var bhdName in bhds) {
                var bhd = Bhd5File.Read(Program.DecryptBhdFile(bhdName, GameVersion.EldenRing), GameVersion.EldenRing);
                foreach (var bucket in bhd.GetBuckets()) {
                    foreach (var entry in bucket.GetEntries()) {
                        hashes.Add(entry.FileNameHash);
                    }
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            var output = new FileStream(outputPath, FileMode.Create);
            var writer = new BinaryWriter(output);
            var arr = hashes.ToArray();
            Array.Sort(arr);
            writer.Write((ulong)arr.Length);
            foreach (var hash in arr) {
                writer.Write(hash);
            }
            output.Close();
            return hashes.Count;
        }

        /// <summary>
        /// Reads a .hashlist (presumably created by <code>CreateHashList</code>) into a HashSet.
        /// </summary>
        /// <param name="filename">The path to the .hashlist</param>
        /// <returns>A HashSet containing all the hashes from the .hashlist</returns>
        public static HashSet<ulong> ReadHashList(string filename)
        {
            var ans = new HashSet<ulong>();
            var fs = new FileStream(filename, FileMode.Open);
            var reader = new BinaryReader(fs);
            var len = reader.ReadUInt64();
            for (ulong i = 0; i < len; i++) {
                ans.Add(reader.ReadUInt64());
            }
            return ans;
        }
        /// <summary>
        /// Updates DictionaryER.csv with newly found hashes. The dictionary will be sorted by code point (default string sort) and duplicate entries will be removed.
        /// </summary>
        /// <param name="newFiles">The list of new file names (may contain names already in the dictionary)</param>
        /// <param name="currPrefix">The path prefix to be replaced (e.g. "/map")</param>
        /// <param name="newPrefix">The alias path to replace <code>currPrefix</code> with (e.g. "map:")</param>
        /// <param name="savePath">The location to write the new dictionary to</param>
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
