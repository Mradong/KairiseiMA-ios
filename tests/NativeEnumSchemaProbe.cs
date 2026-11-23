using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class NativeEnumSchemaProbe
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr LoadLibrary(string path);
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr module, string name);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Open(byte[] data, int length, ref ulong handle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Close(ulong handle);
    static T Bind<T>(IntPtr m, string name) { IntPtr p = GetProcAddress(m, name); if (p == IntPtr.Zero) throw new Exception("missing export"); return (T)(object)Marshal.GetDelegateForFunctionPointer(p, typeof(T)); }
    static void Main(string[] args)
    {
        try
        {
            IntPtr module = LoadLibrary(Path.GetFullPath(args[0])); if (module == IntPtr.Zero) throw new Exception("load failed");
            Open open = Bind<Open>(module, "battlecore_catalog_open_tables"); Close close = Bind<Close>(module, "battlecore_catalog_close");
            JObject fixture = JObject.Parse(File.ReadAllText(args[1]));
            foreach (string variant in new[] { "legacy", "modern", "mixed", "duplicate" })
            {
                JArray values = new JArray();
                if (variant == "legacy") values.Add(new JObject { { "name", "SELF_ENEMY_AI_VAR" }, { "value", 110 } });
                if (variant == "modern") { values.Add(new JObject { { "name", "SELF_STYLE" }, { "value", 110 } }); values.Add(new JObject { { "name", "SELF_ENEMY_AI_VAR" }, { "value", 111 } }); }
                if (variant == "mixed" || variant == "duplicate") { values.Add(new JObject { { "name", "SELF_ENEMY_AI_VAR" }, { "value", 110 } }); values.Add(new JObject { { "name", "SELF_ENEMY_AI_VAR" }, { "value", variant == "mixed" ? 111 : 110 } }); }
                fixture["enums"] = new JArray(new JObject { { "name", "SKILL_EXTEND_EXEC_ORDER" }, { "values", values } });
                byte[] data = Encoding.UTF8.GetBytes(fixture.ToString(Formatting.None)); ulong handle = 0; int status = open(data, data.Length, ref handle);
                int expected = variant == "legacy" || variant == "modern" ? 0 : 2;
                if (status != expected || (status == 0 && (handle == 0 || close(handle) != 0)) || (status != 0 && handle != 0)) throw new Exception("unexpected result " + variant + ": " + status);
                Console.WriteLine(variant + ": " + status);
            }
        }
        catch (Exception e) { Console.Error.WriteLine(e.GetType().Name + ": " + e.Message); Environment.ExitCode = 1; }
    }
}
