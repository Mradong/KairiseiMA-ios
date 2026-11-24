using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

class Order03SelectedCountsProbe {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr LoadLibrary(string p);
 [DllImport("kernel32.dll",CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr p,string n);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Open(byte[] b,int n,ref ulong h);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Create(ulong c,byte[] b,int n,ref ulong h);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Step(ulong b,int op,byte[] input,int count);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Read(ulong b,byte[] output,int capacity,ref int written);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Query(ulong c,ulong p,ulong b,int member,byte[] data,int count,ref int result);
 class Lib { public Open open; public Create create; public Step step; public Read read; public Query query;
  T Bind<T>(IntPtr m,string n){var p=GetProcAddress(m,n);if(p==IntPtr.Zero)throw new Exception("missing "+n);return (T)(object)Marshal.GetDelegateForFunctionPointer(p,typeof(T));}
  public Lib(string p){var m=LoadLibrary(Path.GetFullPath(p));if(m==IntPtr.Zero)throw new Exception("load");open=Bind<Open>(m,"battlecore_catalog_open_tables");create=Bind<Create>(m,"battlecore_context_create");step=Bind<Step>(m,"battlecore_step");read=Bind<Read>(m,"battlecore_result_read");query=Bind<Query>(m,"battlecore_skill_extend_index");}
 }
 static void I32(byte[] b,int o,int v){Buffer.BlockCopy(BitConverter.GetBytes(v),0,b,o,4);} static void I64(byte[] b,int o,long v){Buffer.BlockCopy(BitConverter.GetBytes(v),0,b,o,8);}
 static byte[] Tables(string path){var root=JObject.Parse(File.ReadAllText(path));root["enums"]=new JArray(new JObject{{"name","SKILL_EXTEND_EXEC_ORDER"},{"values",new JArray(new JObject{{"name","NONE"},{"value",0}},new JObject{{"name","FRIEND_PLAY_NUM"},{"value",14}},new JObject{{"name","SELF_PLAY_COST_NUM"},{"value",66}},new JObject{{"name","SELF_NOT_PLAY_HAND_NUM"},{"value",67}})}});return Encoding.UTF8.GetBytes(root.ToString(Newtonsoft.Json.Formatting.None));}
 static string Advance(Lib lib,ulong battle,int op,string json){byte[] input=json==null?null:Encoding.UTF8.GetBytes(json);int s=lib.step(battle,op,input,input==null?0:input.Length);if(s!=0)throw new Exception("step "+op+" status="+s);byte[] output=new byte[1048576];int n=0;if(lib.read(battle,output,output.Length,ref n)!=0)throw new Exception("read");return Encoding.UTF8.GetString(output,0,n);}
 static void Prepare(Lib lib,ulong battle){Advance(lib,battle,1,null);Advance(lib,battle,2,null);string dealt=Advance(lib,battle,3,null);int first=0,second=0;foreach(string line in dealt.Split('\n')){string[] f=line.Split(',');if(f.Length>=4&&f[0]=="20"&&f[1]=="1"){int slot=Int32.Parse(f[2]);if(first==0)first=slot;else if(second==0&&slot!=first)second=slot;}}if(first==0||second==0)throw new Exception("two member-1 cards not dealt");Advance(lib,battle,4,"{\"member_type\":1,\"card_types\":["+first+","+second+",0,0,0],\"targets\":[5,5,0,0,0]}");}
 static byte[] Skill(int ordinal,long[] parameters,int conditionSlot){var wire=new byte[2436];I32(wire,132+96,1);int variant=132+460;I32(wire,variant+88,10);I32(wire,variant+96,2);int condition=variant+conditionSlot*44;I32(wire,condition,ordinal);for(int i=0;i<parameters.Length;i++)I64(wire,condition+4+i*8,parameters[i]);return wire;}
 static JObject Case(string name,int ordinal,bool expected,params long[] parameters){return new JObject{{"name",name},{"ordinal",ordinal},{"expected",expected?1:0},{"parameters",new JArray(parameters)}};}
 static int Main(string[] args){try{var a=new Lib(args[0]);var b=new Lib(args[1]);byte[] tables=Tables(args[2]),battle=File.ReadAllBytes(args[3]);ulong ac=0,bc=0,ab=0,bb=0;if(a.open(tables,tables.Length,ref ac)!=0||b.open(tables,tables.Length,ref bc)!=0||a.create(ac,battle,battle.Length,ref ab)!=0||b.create(bc,battle,battle.Length,ref bb)!=0)throw new Exception("setup");Prepare(a,ab);Prepare(b,bb);
  var inputs=new JArray(
   Case("friend-below",14,false,0,1),Case("friend-exact",14,true,2,2),Case("friend-above",14,false,3,4),Case("friend-wide",14,true,0,3),
   Case("cost-count-below",66,false,1,1,0,1),Case("cost-count-exact",66,true,1,1,2,2),Case("cost-count-above",66,false,1,1,3,4),Case("cost-miss-zero",66,true,2,2,0,0),Case("cost-miss-nonzero",66,false,2,2,1,2),
   Case("hand-below",67,false,0,2),Case("hand-exact",67,true,3,3),Case("hand-above",67,false,4,5),Case("hand-wide",67,true,0,3));
  var results=new JArray();foreach(JObject input in inputs)foreach(int slot in new[]{0,1}){var values=new long[5];JArray p=(JArray)input["parameters"];for(int i=0;i<p.Count;i++)values[i]=(long)p[i];byte[] wire=Skill((int)input["ordinal"],values,slot);int x=12345,y=12345;int left=a.query(0,0,ab,1,wire,wire.Length,ref x),right=b.query(0,0,bb,1,wire,wire.Length,ref y);results.Add(new JObject{{"name",(string)input["name"]},{"ordinal",(int)input["ordinal"]},{"parameters",p.DeepClone()},{"conditionSlot",slot},{"expected",(int)input["expected"]},{"originalStatus",left},{"originalResult",x},{"rebuiltStatus",right},{"rebuiltResult",y}});}
  Console.WriteLine(results.ToString(Newtonsoft.Json.Formatting.None));return 0;
 }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}}
}
