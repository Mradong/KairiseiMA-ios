using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

class Order03ArthurTypeProbe {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr LoadLibrary(string p);
 [DllImport("kernel32.dll",CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr p,string n);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Open(byte[] b,int n,ref ulong h);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Create(ulong c,byte[] b,int n,ref ulong h);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Query(ulong c,ulong p,ulong b,int m,byte[] data,int n,ref int result);
 class Lib {
  public Open open; public Create create; public Query query;
  T Bind<T>(IntPtr m,string n){var p=GetProcAddress(m,n);if(p==IntPtr.Zero)throw new Exception("missing "+n);return (T)(object)Marshal.GetDelegateForFunctionPointer(p,typeof(T));}
  public Lib(string path){var m=LoadLibrary(Path.GetFullPath(path));if(m==IntPtr.Zero)throw new Exception("load");open=Bind<Open>(m,"battlecore_catalog_open_tables");create=Bind<Create>(m,"battlecore_context_create");query=Bind<Query>(m,"battlecore_skill_extend_index");}
 }
 static void Put32(byte[] b,int o,int v){Buffer.BlockCopy(BitConverter.GetBytes(v),0,b,o,4);}
 static void Put64(byte[] b,int o,long v){Buffer.BlockCopy(BitConverter.GetBytes(v),0,b,o,8);}
 static int Main(string[] args){try {
  var a=new Lib(args[0]);var b=new Lib(args[1]);var table=JObject.Parse(File.ReadAllText(args[2]));
  table["enums"]=JArray.Parse("[{\"name\":\"SKILL_EXTEND_EXEC_ORDER\",\"values\":[{\"name\":\"NONE\",\"value\":0},{\"name\":\"SELF_ARTHUR_TYPE\",\"value\":107}]},{\"name\":\"ARTHUR_TYPE\",\"values\":[{\"name\":\"NULL\",\"value\":0},{\"name\":\"MERCENARY\",\"value\":1},{\"name\":\"MILLIONAIRE\",\"value\":2},{\"name\":\"THIEF\",\"value\":3},{\"name\":\"SINGER\",\"value\":4},{\"name\":\"ALCHEMIST\",\"value\":5}]}]");
  byte[] tables=Encoding.UTF8.GetBytes(table.ToString(Newtonsoft.Json.Formatting.None));ulong ac=0,bc=0;if(a.open(tables,tables.Length,ref ac)!=0||b.open(tables,tables.Length,ref bc)!=0)throw new Exception("catalog");
  var fixture=JObject.Parse(File.ReadAllText(args[3]));var results=new JArray();
  foreach(int actor in new[]{1,2,3,4,5}) { ((JObject)fixture["members"][0])["arthur_type"]=actor;byte[] battle=Encoding.UTF8.GetBytes(fixture.ToString(Newtonsoft.Json.Formatting.None));ulong ab=0,bb=0;if(a.create(ac,battle,battle.Length,ref ab)!=0||b.create(bc,battle,battle.Length,ref bb)!=0)throw new Exception("create");
   foreach(long wanted in new long[]{0,1,2,3,4,5,6,2147483648L})foreach(int slot in new[]{0,1}) {byte[] wire=new byte[2436];int o=132+slot*460;Put32(wire,o+96,1);Put32(wire,o,107);Put64(wire,o+4,wanted);int x=12345,y=12345;int left=a.query(0,0,ab,1,wire,wire.Length,ref x),right=b.query(0,0,bb,1,wire,wire.Length,ref y);results.Add(new JObject{{"actor",actor},{"wanted",wanted},{"slot",slot},{"originalStatus",left},{"originalResult",x},{"rebuiltStatus",right},{"rebuiltResult",y}});}
  }
  Console.WriteLine(results.ToString(Newtonsoft.Json.Formatting.None));return 0;
 }catch(Exception e){Console.Error.WriteLine(e.GetType().Name+": "+e.Message);return 1;}}
}
