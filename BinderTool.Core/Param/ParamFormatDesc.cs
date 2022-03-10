using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinderTool.Core.Param
{
    public enum ParamType
    {
        Byte, Short, Int, Long, UByte, UShort, UInt, ULong,
        Float, Double
    }
    public class ParamFormatDesc
    {
        public List<(string, ParamType)> ParamInfo = new List<(string, ParamType)>();
        public static ParamFormatDesc Read(string paramFilename)
        {
            var filename = Path.GetFileNameWithoutExtension(paramFilename) + ".csv";
            if (!File.Exists(filename)) return null;
            ParamFormatDesc ans = new ParamFormatDesc();
            var lines = File.ReadAllLines(filename);
            foreach (var line in lines) {
                var sp = line.Split(',');
                var name = sp[0];
                var tyName = sp[1];
                ParamType ty;
                switch(tyName.Trim()) {
                    case "Byte":
                        ty = ParamType.Byte;
                        break;
                    case "Short":
                        ty = ParamType.Short;
                        break;
                    case "Int":
                        ty = ParamType.Int;
                        break;
                    case "Long":
                        ty = ParamType.Long;
                        break;
                    case "UByte":
                        ty = ParamType.UByte;
                        break;
                    case "UShort":
                        ty = ParamType.UShort;
                        break;
                    case "UInt":
                        ty = ParamType.UInt;
                        break;
                    case "ULong":
                        ty = ParamType.ULong;
                        break;
                    case "Float":
                        ty = ParamType.Float;
                        break;
                    case "Double":
                        ty = ParamType.Double;
                        break;
                    default:
                        throw new Exception($"Invalid param type {tyName}");
                }
                ans.ParamInfo.Add((name, ty));
            }
            return ans;
        }
    }
}
