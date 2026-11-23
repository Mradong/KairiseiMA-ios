using System;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
class Order03RealEnemyQueryProbe {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr LoadLibrary(string path);
 [DllImport("kernel32.dll",CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr module,string name);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Open(byte[] b,int n,ref ulong h);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Prep(int mode,ref ulong h);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int User(ulong h,int member,int[] values,int n,int seed,byte[] output,int cap,ref int written);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Enemy(ulong h,int member,byte[] data,int n);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Clear(ulong h);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Query(ulong catalog,ulong prep,ulong battle,int member,byte[] skill,int n,ref int result);
 class Lib {
  public Open open;public Prep prep;public User user;public Enemy enemy;public Clear clear;public Query query;
  T Bind<T>(IntPtr m,string n){return (T)(object)Marshal.GetDelegateForFunctionPointer(GetProcAddress(m,n),typeof(T));}
  public Lib(string p){var m=LoadLibrary(Path.GetFullPath(p));if(m==IntPtr.Zero)throw new Exception("DLL load");open=Bind<Open>(m,"battlecore_catalog_open_tables");prep=Bind<Prep>(m,"battlecore_preparation_open");user=Bind<User>(m,"battlecore_preparation_user");enemy=Bind<Enemy>(m,"battlecore_preparation_enemy");clear=Bind<Clear>(m,"battlecore_preparation_enemy_clear");query=Bind<Query>(m,"battlecore_skill_extend_index");}
 }
 static void I32(byte[] b,int o,int v){Buffer.BlockCopy(BitConverter.GetBytes(v),0,b,o,4);}static void I64(byte[] b,int o,long v){Buffer.BlockCopy(BitConverter.GetBytes(v),0,b,o,8);}static void I16(byte[] b,int o,short v){Buffer.BlockCopy(BitConverter.GetBytes(v),0,b,o,2);}
 static int Main(string[] args){try {
  var defs=JObject.Parse(File.ReadAllText(args[3]));var enemies=(JArray)defs["enemy_definitions"];var levels=(JArray)defs["enemy_level_definitions"];
  var original=new Lib(args[0]);var rebuilt=new Lib(args[1]);var input=File.ReadAllBytes(args[2]);ulong a=0,b=0,ap=0,bp=0;if(original.open(input,input.Length,ref a)!=0||rebuilt.open(input,input.Length,ref b)!=0||original.prep(1,ref ap)!=0||rebuilt.prep(1,ref bp)!=0)throw new Exception("open");
  int[] user=new int[18];user[1]=50;user[2]=100;byte[] buf=new byte[4096];int written=0;if(original.user(ap,1,user,user.Length,0,buf,buf.Length,ref written)!=0||rebuilt.user(bp,1,user,user.Length,0,buf,buf.Length,ref written)!=0)throw new Exception("user");
  var results=new JArray();foreach(JObject e in enemies){int id=(int)e["ID"];JObject lv=null;foreach(JObject x in levels)if((int)x["ID"]==id)lv=x;if(lv==null||(string)e["Attribute"]!="ICE")throw new Exception("candidate mismatch");
   byte[] raw=new byte[276];I32(raw,0,id);I32(raw,4,(int)e["TribeIDs"][0]);I64(raw,8,(long)e["HP"]);I64(raw,16,(long)e["HP"]);I32(raw,24,100);I64(raw,28,(long)e["Attack"]);I64(raw,36,(long)e["Magic"]);I64(raw,44,(long)e["Recovery"]);I64(raw,52,(long)e["Defense"]);I64(raw,60,(long)e["MagicDefense"]);I32(raw,68,(int)e["DamageReduction"]);I32(raw,112,2);I32(raw,116,(int)lv["ActionsPerTurn"]);I32(raw,156,(int)lv["InitialAwake"]);
   for(int i=0;i<9;i++)I16(raw,72+i*2,(short)lv["AttributeRates"][i]);
   if(original.clear(ap)!=0||rebuilt.clear(bp)!=0||original.enemy(ap,5,raw,raw.Length)!=0||rebuilt.enemy(bp,5,raw,raw.Length)!=0)throw new Exception("register");
   foreach(int want in new[]{0,1}){byte[] skill=new byte[2436];I32(skill,228,1);I32(skill,132,106);for(int i=0;i<5;i++)I64(skill,136+i*8,want);int left=12345,right=12345;int ls=original.query(a,ap,0,1,skill,skill.Length,ref left),rs=rebuilt.query(b,bp,0,1,skill,skill.Length,ref right);results.Add(new JObject{{"enemyId",id},{"wantedTribe",want},{"originalStatus",ls},{"originalResult",left},{"rebuiltStatus",rs},{"rebuiltResult",right}});}
  }
  Console.WriteLine(results.ToString(Newtonsoft.Json.Formatting.None));return 0;
 }catch(Exception e){Console.Error.WriteLine(e.GetType().Name+": "+e.Message);return 1;}}
}
