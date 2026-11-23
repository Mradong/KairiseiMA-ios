// Runs the actual exported 1.4.0 CSV implementation. Global/Unity stubs below
// provide only dependencies of that class; this is not a modern-client test.
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace UnityEngine {
    public class TextAsset { public string text; }
    public struct Color { public Color(float r,float g,float b,float a) {} }
    public static class Mathf { public static float Clamp(float v,float a,float b) { return Math.Max(a,Math.Min(b,v)); } }
}
public static class Global {
    public static string[] splitString(string str,char separator,char close) {
        var list=new List<string>(); bool quoted=false; int start=0;
        for(int i=0;i<str.Length;i++){char c=str[i];if(c==close)quoted=!quoted;if(!quoted&&c==separator){list.Add(i!=start?str.Substring(start,i-start):string.Empty);start=i+1;}}
        list.Add(str.Substring(start)); return list.ToArray();
    }
    public static string[] bisectString(string str,char separator,char close) { int i=str.IndexOf(separator); return i<0?new[]{str}:new[]{str.Substring(0,i),str.Substring(i+1)}; }
    public static int parseInt(string s,int def=0) { int n; return int.TryParse(s,out n)?n:def; }
    public static uint parseUInt(string s,uint def=0) { uint n; return uint.TryParse(s,out n)?n:def; }
    public static long parseLong(string s,long def=0) { long n; return long.TryParse(s,out n)?n:def; }
    public static float parseFloat(string s,float def=0) { float n; return float.TryParse(s,out n)?n:def; }
    public static double parseDouble(string s,double def=0) { double n; return double.TryParse(s,out n)?n:def; }
    public static T parseEnum<T>(string s,T def) where T:struct,IConvertible { T n; return Enum.TryParse<T>(s,out n)?n:def; }
    public static int parseEnum2Int<T>(string s,int def=0) where T:struct,IConvertible { T n; return Enum.TryParse<T>(s,out n)?Convert.ToInt32(n):def; }
}
class Order03LegacyCompatibility {
    static int Main(string[] args) {
        try {
            var warnings=(JArray)JObject.Parse(File.ReadAllText(args[0]))["additional_registry_checks"]["raw_ai_shape_warnings"];
            var problems=(JArray)JObject.Parse(File.ReadAllText(args[1]))["numeric_problems"];
            var ids=new HashSet<string>();foreach(var w in warnings) if(!ids.Add((string)w["id"]))throw new Exception("duplicate warning ID");
            int rows=0,missing=0;
            foreach(string row in File.ReadLines(args[2])) {
                string id=row.Split(',')[0]; if(!ids.Contains(id))continue;
                string[] fields=Global.splitString(row,',','"');
                var raw=new CSV();var padded=new CSV();raw.setData(row);padded.setData(row+new string(',',Math.Max(0,38-fields.Length)));
                for(int i=0;i<38;i++){string a=raw.popString(""),b=padded.popString("");if(a!=b)throw new Exception("field mismatch "+id+" index "+i);if(i>=fields.Length)missing++;}
                rows++;
            }
            if(rows!=warnings.Count||rows!=36)throw new Exception("AI coverage mismatch");
            int values=0; foreach(var p in problems) { string s=(string)p["raw_value"]; int result=s=="ALL"?-1:Global.parseInt(s);if(result!=int.Parse((string)p["reviewed_go_bound_value"]))throw new Exception("VALUE mismatch");values++; }
            if(values!=25)throw new Exception("VALUE coverage mismatch");
            Console.WriteLine("legacy_csv_short_rows="+rows+" missing_fields_compared="+missing+" legacy_value_cases="+values);
            return 0;
        } catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
