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
                            //Debug.WriteLine($"{hash} {next}");
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
                    () => GetMaps().Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
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
                    () => GetMaps().Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                }
            );
        }
        public static (string, GetEnumerator[]) data2MapBtlSearch(string extractedFolderPath)
        {

            return (
                "/map/{0}_{1}.btl.dcx",
                new GetEnumerator[] {
                    () => GetMaps().Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                    Range0Padded(0, 10000, 4),
                }
            );
        }
        public static (string, GetEnumerator[]) data2MapFvbSearch(string extractedFolderPath)
        {

            return (
                "/map/{0}_{1}.fvb.dcx",
                new GetEnumerator[] {
                    () => GetMaps().Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                    Range0Padded(0, 10000, 4),
                }
            );
        }
        public static (string, GetEnumerator[]) data2MapNvmhktbndSearch(string extractedFolderPath)
        {

            return (
                "/map/{0}.nvmhktbnd.dcx",
                new GetEnumerator[] {
                    () => GetMaps().Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                }
            );
        }
        public static (string, GetEnumerator[]) data2MapMpwSearch(string extractedFolderPath)
        {

            return (
                "/map/{0}.mpw.dcx",
                new GetEnumerator[] {
                    () => GetMaps().Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                }
            );
        }
        public static (string, GetEnumerator[]) data2MapFlverSearch(string extractedFolderPath)
        {

            return (
                "/map/{0}.flver.dcx",
                new GetEnumerator[] {
                    () => GetMaps().Select(s => s.Substring(0, 3) + "/" + s.Substring(0, 12) + "/" + s.Substring(0, 12)),
                }
            );
        }
        public static (string, GetEnumerator[]) data2MapNvcSearch(string extractedFolderPath)
        {

            return (
                "/map/nvc/{0}_{1}.nvc.dcx",
                new GetEnumerator[] {
                    () => GetMaps(),
                    () => GetMaps(),
                }
            );
        }
        public static (string, GetEnumerator[]) data2MapBreakgeomSearch(string extractedFolderPath)
        {

            return (
                "/map/breakgeom/lod{0}/{1}_lod{0}.breakgeom.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 10, 1),
                    () => GetMaps(),
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
        public static (string, GetEnumerator[]) data0MapinfotexSearch()
        {

            return (
                "/other/mapinfotex/{0}.mapinfotexbnd.dcx",
                new GetEnumerator[] {
                    () => GetMaps(),
                }
            );
        }
        public static (string, GetEnumerator[]) data0OtherSearch()
        {
            return (
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
                }
            );
        }
        public static (string, GetEnumerator[]) data0MaterialSearch()
        {
            return (
                "/material/{1}{0}",
                new GetEnumerator[] {
                    () => new string[] {"", ".dcx"},
                    () => new string[] {
                        "AllMaterial.matbinbnd.devpatch",
                        "AllMaterial.matbinbnd",
                        "SpeedTree.matbinbnd"
                    }
                }
            );
        }
        public static (string, GetEnumerator[], string, string) data0ShaderSearch()
        {
            return (
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
                "/shader",
                "shader:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0FontSearch()
        {
            return (
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
                "/font/",
                "font:/"
            );
        }
        public static (string, GetEnumerator[], string, string) data0ActionSearch()
        {
            return (
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
                "/action",
                "action:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0ActscriptSearch()
        {
            return (
                "/action/script/c{1}.hks{0}",
                new GetEnumerator[] {
                    () => new string[] {"", ".dcx"},
                    Range0Padded(0, 10_000, 4)
                },
                "/action/script",
                "actscript:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0TalkscriptSearch()
        {
            return (
                "/script/talk/m{0}_{1}_{2}_{3}.talkesdbnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2)
                },
                "/script/talk",
                "talkscript:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0AiscriptSearch()
        {
            return (
                "/script/m{0}_{1}_{2}_{3}.luabnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2)
                },
                "/script",
                "aiscript:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0AiscriptSearch2()
        {
            return (
                "/script/{1}_{0}.luabnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"logic", "battle"},
                    Range0Padded(0, 1000000, 6),
                },
                "/script",
                "aiscript:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0MsgSearch()
        {
            return (
                "/msg/{1}/{0}.msgbnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"item", "menu", "ngword", "sellregion"},
                    () => new string[] {
                        "jpnJP", "engUS", "fraFR", "spaES", "itaIT", "deuDE", "korKR", "zhoTW",
                        "zhoCN", "polPL", "rusRU", "porBR", "spaAR", "thaTH", "areAE", "JP",
                        "NA", "EU", "AS", "UK"
                    }
                },
                "/msg",
                "msg:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0ParamSearch()
        {
            return (
                "/param/{0}.parambnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {
                        "systemparam/systemparam", 
                        "GameParam/GameParam", 
                        "EventParam/EventParam"
                    }
                },
                "/param",
                "param:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0GparamSearch()
        {
            return (
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
                "/param/drawparam",
                "gparam:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0GparamSearch2()
        {
            return (
                "/param/drawparam/s{0}_{1}_{2}.gparam.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 10000, 4)
                },
                "/param/drawparam",
                "gparam:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0GparamSearch3()
        {
            return (
                "/param/drawparam/m{1}_{2}_{3}{0}.gparam.dcx",
                new GetEnumerator[] {
                    () => new string[] {"", "_WeatherBase", "_WeatherOutdoor", "_CommonEvent", "_CommonEventMapUnique"},
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 10000, 4)
                },
                "/param/drawparam",
                "gparam:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0GparamSearch4()
        {
            return (
                "/param/drawparam/s{0}_{1}_{2}_WeatherOverride_{3}.gparam.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 10000, 4),
                    Range0Padded(0, 100, 2)
                },
                "/param/drawparam",
                "gparam:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0EventSearch()
        {
            return (
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
                "/event",
                "event:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0EventSearch2()
        {
            return (
                "/event/m{0}_{1}_{2}_{3}.emevd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2)
                },
                "/event",
                "event:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0EventSearch3()
        {
            return (
                "/event/m{0}.emevd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2)
                },
                "/event",
                "event:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0MenuSearch()
        {
            return (
                "/menu/{1}{0}",
                new GetEnumerator[] {
                    () => new string[] {"", ".dcx"},
                    () => new string[] {
                        "71_MapTile.mtmskbnd"
                    }
                },
                "/menu",
                "menu:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0MenuSearch2()
        {
            return (
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
                "/menu",
                "menu:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0MenuSearch3()
        {
            return (
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
                "/menu",
                "menu:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0MenuSearch4()
        {
            return (
                "/menu/MapImage/{0}{1}.tpf.dcx",
                new GetEnumerator[] {
                    () => Enumerable.Range(0, byte.MaxValue).Select(c => ""+(char)c),
                    () => Enumerable.Range(0, byte.MaxValue).Select(c => ""+(char)c),
                },
                "/menu",
                "menu:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0PartsSearch()
        {
            return (
                "/parts/{1}{2}_{3}_{4}{0}.partsbnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"", "_l"},
                    () => Enumerable.Range('a', 26).Select(c => ""+(char)c),
                    () => Enumerable.Range('a', 26).Select(c => ""+(char)c),
                    () => Enumerable.Range('a', 26).Select(c => ""+(char)c),
                    Range0Padded(0, 10000, 4)
                },
                "/parts",
                "parts:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0FacegenSearch()
        {
            return (
                "/facegen/{0}.fgbnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"FaceGen"}
                },
                "/facegen",
                "facegen:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0CutsceneSearch()
        {
            return (
                "/cutscene/s{0}_{1}_{2}.cutscenebnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 10000, 4)
                },
                "/cutscene",
                "cutscenebnd:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0CutsceneSearch2()
        {
            return (
                "/cutscene/{0}_{1}.tpfbnd.dcx",
                new GetEnumerator[] {
                    () => GetFiles().Select(f => {
                        var m = Regex.Match(f, @"(s\d\d_\d\d_\d\d\d\d).cutscenebnd");
                        if (m.Success) return m.Groups[1].Value;
                        return null;
                    }).Where(f => f != null),
                    Range0Padded(0, 100, 2),
                },
                "/cutscene",
                "cutscenebnd:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0MovieSearch()
        {
            return (
                "/movie/{1}.bk2{0}",
                new GetEnumerator[] {
                    () => new string[] {"", ".dcx"},
                    Range0Padded(0, 100000000, 8),
                },
                "/movie",
                "movie:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0WwiseSearch()
        {
            return (
                "/sound/{0}.mobnkinfo",
                new GetEnumerator[] {
                    () => new string[] {"SoundbanksInfo"}
                },
                "/sound",
                "wwise_mobnkinfo:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0WwiseSearch2()
        {
            return (
                "/sound/{0}{1}.moaeibnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"", "enus/"},
                    () => new string[] {"AdditionalEventInfo"}
                },
                "/sound",
                "wwise_moaeibnd:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0SfxbndSearch()
        {
            return (
                "/sfx/SfxBnd_c{0}.ffxbnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 10000, 4)
                },
                "/sfx",
                "sfxbnd:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0SfxbndSearch2()
        {
            return (
                "/sfx/SfxBnd_m{0}.ffxbnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2)
                },
                "/sfx",
                "sfxbnd:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0SfxbndSearch3()
        {
            return (
                "/sfx/SfxBnd_m{0}_{1}_{2}_{3}.ffxbnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2)
                },
                "/sfx",
                "sfxbnd:"
            );
        }
        public static (string, GetEnumerator[], string, string) data0SfxbndSearch4()
        {
            return (
                "/sfx/{0}.ffxbnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"SfxBnd_CommonEffects"}
                },
                "/sfx",
                "sfxbnd:"
            );
        }
        public static (string, GetEnumerator[], string, string) data1AetTpfSearch()
        {
            return (
                "/asset/aet/aet{1}/aet{1}_{2}{0}.tpf.dcx",
                new GetEnumerator[] {
                    () => new string[] {"", "_l", "_billboards", "_billboards_l"},
                    Range0Padded(0, 1000, 3),
                    Range0Padded(0, 1000, 3)
                },
                "/asset",
                "asset:"
            );
        }
        public static (string, GetEnumerator[], string, string) data1AegGeombndSearch()
        {
            return (
                "/asset/aeg/aeg{0}/aeg{0}_{1}.geombnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 1000, 3),
                    Range0Padded(0, 1000, 3)
                },
                "/asset",
                "asset:"
            );
        }
        public static (string, GetEnumerator[], string, string) data1AegGeomhkxbndSearch()
        {
            return (
                "/asset/aeg/aeg{1}/aeg{1}_{2}_{0}.geomhkxbnd.dcx",
                new GetEnumerator[] {
                    () => new string[] { "h", "l" },
                    Range0Padded(0, 1000, 3),
                    Range0Padded(0, 1000, 3)
                },
                "/asset",
                "asset:"
            );
        }
        public static (string, GetEnumerator[], string, string) data3ChrChrbndSearch()
        {
            return (
                "/chr/c{0}.chrbnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 10000, 4),
                },
                "/chr",
                "chr:"
            );
        }
        public static (string, GetEnumerator[], string, string) data3ChrAnibndSearch()
        {
            return (
                "/chr/c{0}.anibnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 10000, 4),
                },
                "/chr",
                "chranibnd:"
            );
        }
        public static (string, GetEnumerator[], string, string) data3ChrAnibndSearch2()
        {
            return (
                "/chr/c0000_a{0}.anibnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"00_lo", "00_md", "00_hi", "0x", "1x", "2x", "3x", "4x", "5x", "6x", "7x", "8x", "9x"},
                },
                "/chr",
                "chranibnd:"
            );
        }
        public static (string, GetEnumerator[], string, string) data3ChrAnibndSearch3()
        {
            return (
                "/chr/c{1}_div{0}.anibnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 10000, 4),
                },
                "/chr",
                "chranibnd:"
            );
        }
        public static (string, GetEnumerator[], string, string) data3ChrBehbndSearch()
        {
            return (
                "/chr/c{0}.behbnd.dcx",
                new GetEnumerator[] {
                    Range0Padded(0, 10000, 4),
                },
                "/chr",
                "chrbehbnd:"
            );
        }
        public static (string, GetEnumerator[], string, string) data3ChrTexbndSearch()
        {
            return (
                "/chr/c{1}_{0}.texbnd.dcx",
                new GetEnumerator[] {
                    () => new string[] {"h", "l"},
                    Range0Padded(0, 10000, 4),
                },
                "/chr",
                "chrtexbnd:"
            );
        }
        public static (string, GetEnumerator[], string, string) sdSearch()
        {
            return (
                "/{0}cs_c{1}.bnk",
                new GetEnumerator[] {
                    () => new string[] {"", "enus/"},
                    Range0Padded(0, 10000, 4),
                },
                "/",
                "wwise:/"
            );
        }
        public static (string, GetEnumerator[], string, string) sdSearch2()
        {
            return (
                "/aeg{0}_{1}.bnk",
                new GetEnumerator[] {
                    Range0Padded(0, 1000, 3),
                    Range0Padded(0, 1000, 3),
                },
                "/",
                "wwise:/"
            );
        }
        public static (string, GetEnumerator[], string, string) sdSearch3()
        {
            return (
                "/cs_{0}m{1}{2}.bnk",
                new GetEnumerator[] {
                    () => new string[] {"", "s"},
                    Range0Padded(0, 100, 2),
                    () => (new string[] { "" }).AsEnumerable().Union(Range0Padded(0, 1000, 3)().Select(s => "_"+s)),
                },
                "/",
                "wwise:/"
            );
        }
        public static (string, GetEnumerator[], string, string) sdSearch4()
        {
            return (
                "/{0}.bnk",
                new GetEnumerator[] {
                    () => new string[] {"cs_main", "cs_smain", "init", "vc700", "enus/vcmain"},
                },
                "/enus",
                "wwise:/enus"
            );
        }
        public static (string, GetEnumerator[], string, string) sdSearch5()
        {
            return (
                "/{0}s{1}_{2}_{3}.bnk",
                new GetEnumerator[] {
                    () => new string[] {"", "enus/"},
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 100, 2),
                    Range0Padded(0, 10000, 4),
                },
                "/enus",
                "wwise:/enus"
            );
        }
        public static (string, GetEnumerator[], string, string) sdSearch6()
        {
            return (
                "/enus/vc{0}.bnk",
                new GetEnumerator[] {
                    Range0Padded(0, 1000, 3),
                },
                "/enus",
                "wwise:/enus"
            );
        }
        public static (string, GetEnumerator[], string, string, GetEnumerator[]) sdSearch7()
        {
            return (
                "/enus/wem/{0}/{0}{1}.wem",
                new GetEnumerator[] {
                    () => Enumerable.Range(0, 100000000).SelectMany(i => new string[] {i.ToString(), new string(i.ToString().Reverse().ToArray()) })
                },
                "/enus",
                "wwise:/enus",
                new GetEnumerator[] {
                    () => Enumerable.Range(10, 10).Select(i => i.ToString()),
                    () => Enumerable.Range(20, 10).Select(i => i.ToString()),
                    () => Enumerable.Range(30, 10).Select(i => i.ToString()),
                    () => Enumerable.Range(40, 10).Select(i => i.ToString()),
                    () => Enumerable.Range(50, 10).Select(i => i.ToString()),
                    () => Enumerable.Range(60, 10).Select(i => i.ToString()),
                    () => Enumerable.Range(70, 10).Select(i => i.ToString()),
                    () => Enumerable.Range(80, 10).Select(i => i.ToString()),
                    () => Enumerable.Range(90, 10).Select(i => i.ToString()),
                }
            );
        }
        public static (string, GetEnumerator[], string, string, GetEnumerator[]) sdSearch8()
        {
            return (
                "/wem/{0}/{0}{1}.wem",
                new GetEnumerator[] {
                    () => Enumerable.Range(0, 100000000).SelectMany(i => new string[] {i.ToString(), new string(i.ToString().Reverse().ToArray()) })
                },
                "/wem",
                "wwise:/wem",
                new GetEnumerator[] {
                    () => Enumerable.Range(10, 10).Select(i => i.ToString()),
                    () => Enumerable.Range(20, 10).Select(i => i.ToString()),
                    () => Enumerable.Range(30, 10).Select(i => i.ToString()),
                    () => Enumerable.Range(40, 10).Select(i => i.ToString()),
                    () => Enumerable.Range(50, 10).Select(i => i.ToString()),
                    () => Enumerable.Range(60, 10).Select(i => i.ToString()),
                    () => Enumerable.Range(70, 10).Select(i => i.ToString()),
                    () => Enumerable.Range(80, 10).Select(i => i.ToString()),
                    () => Enumerable.Range(90, 10).Select(i => i.ToString()),
                }
            );
        }
        public static (string, GetEnumerator[], string, string) data0MenuSearch5()
        {
            return (
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
                "/menu",
                "menu:"
            );
        }
        public static (string, GetEnumerator[]) devpatchSearch()
        {
            return (
                "{0}",
                new GetEnumerator[] {
                    () => GetFiles()
                    .SelectMany(f => f.EndsWith(".dcx") ? new string[] {f, f.Replace(".dcx", "")} : new string[] {f})
                    .SelectMany(f => new string[] {f+".devpatch", f+".devpatch.dcx"})
                }
            );
        }
        public static IEnumerable<string> GetMaps()
        {
            return File.ReadLines("ERMaps.csv");
        }
        public static IEnumerable<string> GetFiles()
        {
            return File.ReadLines("DictionaryER.csv").Select(FileNameDictionary.MakeHashable);
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
        public static void CreateHashList(string outputPath, params string[] bhds)
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
        }
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
