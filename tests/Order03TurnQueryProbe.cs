using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
class Order03TurnQueryProbe {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr LoadLibrary(string p);
 [DllImport("kernel32.dll",CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr p,string n);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Open(byte[] b,int n,ref ulong h);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Create(ulong c,byte[] b,int n,ref ulong h);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Prep(int mode,ref ulong h);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int User(ulong h,int member,int[] values,int n,int seed,byte[] output,int cap,ref int written);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Query(ulong c,ulong p,ulong b,int m,byte[] data,int n,ref int result);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Step(ulong b,int op,byte[] input,int count);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Read(ulong b,byte[] output,int count,ref int written);
 class Lib {public Open open;public Create create;public Prep prep;public User user;public Query query;public Step step;public Read read;
  T Bind<T>(IntPtr m,string n){var p=GetProcAddress(m,n);if(p==IntPtr.Zero)throw new Exception("missing export");return (T)(object)Marshal.GetDelegateForFunctionPointer(p,typeof(T));}
  public Lib(string p){var m=LoadLibrary(Path.GetFullPath(p));if(m==IntPtr.Zero)throw new Exception("load");open=Bind<Open>(m,"battlecore_catalog_open_tables");create=Bind<Create>(m,"battlecore_context_create");prep=Bind<Prep>(m,"battlecore_preparation_open");user=Bind<User>(m,"battlecore_preparation_user");query=Bind<Query>(m,"battlecore_skill_extend_index");step=Bind<Step>(m,"battlecore_step");read=Bind<Read>(m,"battlecore_result_read");}
 }
 static void I32(byte[] b,int o,int v){Buffer.BlockCopy(BitConverter.GetBytes(v),0,b,o,4);}static void I64(byte[] b,int o,long v){Buffer.BlockCopy(BitConverter.GetBytes(v),0,b,o,8);}
 static int Main(string[] args){try{
  var a=new Lib(args[0]);var b=new Lib(args[1]);var text=File.ReadAllText(args[2]).TrimEnd();text=text.Substring(0,text.Length-1)+",\"enums\":[{\"name\":\"SKILL_EXTEND_EXEC_ORDER\",\"values\":[{\"name\":\"NONE\",\"value\":0},{\"name\":\"TURN\",\"value\":10}]}]}";var tables=Encoding.UTF8.GetBytes(text);var battle=File.ReadAllBytes(args[3]);ulong ac=0,bc=0,ab=0,bb=0,ap=0,bp=0;if(a.open(tables,tables.Length,ref ac)!=0||b.open(tables,tables.Length,ref bc)!=0||a.create(ac,battle,battle.Length,ref ab)!=0||b.create(bc,battle,battle.Length,ref bb)!=0||a.prep(1,ref ap)!=0||b.prep(1,ref bp)!=0)throw new Exception("setup");
  var u=new int[18];u[1]=50;u[2]=100;var buf=new byte[4096];int n=0;if(a.user(ap,1,u,u.Length,0,buf,buf.Length,ref n)!=0||b.user(bp,1,u,u.Length,0,buf,buf.Length,ref n)!=0)throw new Exception("user");
  var results=new JArray();foreach(string phase in new[]{"prepared","before_start","after_start","turn_phase"}){
   if(phase=="after_start"||phase=="turn_phase"){int op=phase=="after_start"?1:2;int sa=a.step(ab,op,null,0),sb=b.step(bb,op,null,0);if(sa!=0||sb!=0)throw new Exception("step "+op);byte[] x=new byte[1048576],y=new byte[1048576];int nx=0,ny=0;if(a.read(ab,x,x.Length,ref nx)!=0||b.read(bb,y,y.Length,ref ny)!=0||Encoding.UTF8.GetString(x,0,nx)!=Encoding.UTF8.GetString(y,0,ny))throw new Exception("result differs");}
   foreach(long[] bounds in new[]{new long[]{0,0},new long[]{1,0},new long[]{2,3},new long[]{-1,1},new long[]{1,1},new long[]{2,1},new long[]{2147483648L,0},new long[]{-2147483649L,2147483648L}})foreach(int slot in new[]{0,1}){
    byte[] wire=new byte[2436];int o=132+slot*460;I32(wire,o+96,1);I32(wire,o,10);I64(wire,o+4,bounds[0]);I64(wire,o+12,bounds[1]);int x=12345,y=12345;bool prepared=phase=="prepared";int l=a.query(prepared?ac:0,prepared?ap:0,prepared?0:ab,1,wire,wire.Length,ref x),r=b.query(prepared?bc:0,prepared?bp:0,prepared?0:bb,1,wire,wire.Length,ref y);results.Add(new JObject{{"phase",phase},{"lower",bounds[0]},{"upper",bounds[1]},{"slot",slot},{"originalStatus",l},{"originalResult",x},{"rebuiltStatus",r},{"rebuiltResult",y}});
   }
  }
  Console.WriteLine(results.ToString(Newtonsoft.Json.Formatting.None));return 0;
 }catch(Exception e){Console.Error.WriteLine(e.GetType().Name+": "+e.Message);return 1;}}
}
