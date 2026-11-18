using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

class PreparedEnemyQueryDifferential {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr LoadLibrary(string path);
 [DllImport("kernel32.dll",CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr module,string name);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Open(byte[] data,int count,ref ulong handle);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Prep(int mode,ref ulong handle);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int User(ulong prep,int member,int[] data,int count,int seed,byte[] output,int capacity,ref int written);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Enemy(ulong prep,int member,byte[] data,int count);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Clear(ulong prep);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Query(ulong catalog,ulong prep,ulong battle,int member,byte[] skill,int count,ref int result);
 class Lib { public Open open;public Prep prep;public User user;public Enemy enemy;public Clear clear;public Query query;
  T Bind<T>(IntPtr m,string n){return (T)(object)Marshal.GetDelegateForFunctionPointer(GetProcAddress(m,n),typeof(T));}
  public Lib(string path){var m=LoadLibrary(Path.GetFullPath(path));if(m==IntPtr.Zero)throw new Exception("load");open=Bind<Open>(m,"battlecore_catalog_open_tables");prep=Bind<Prep>(m,"battlecore_preparation_open");user=Bind<User>(m,"battlecore_preparation_user");enemy=Bind<Enemy>(m,"battlecore_preparation_enemy");clear=Bind<Clear>(m,"battlecore_preparation_enemy_clear");query=Bind<Query>(m,"battlecore_skill_extend_index");}
 }
 static void Put(byte[] b,int at,int value){Buffer.BlockCopy(BitConverter.GetBytes(value),0,b,at,4);}
 static void Put64(byte[] b,int at,long value){Buffer.BlockCopy(BitConverter.GetBytes(value),0,b,at,8);}
 static void Main(string[] args){
  var a=new Lib(args[0]);var b=new Lib(args[1]);string text=File.ReadAllText(args[2]).TrimEnd();
  text=text.Substring(0,text.Length-1)+",\"enums\":[{\"name\":\"SKILL_EXTEND_EXEC_ORDER\",\"values\":[{\"name\":\"NONE\",\"value\":0},{\"name\":\"ENEMY_TRIBAL\",\"value\":1}]},{\"name\":\"ATTR\",\"values\":[{\"name\":\"FIRE\",\"value\":0},{\"name\":\"ICE\",\"value\":1}]}]}";var tables=Encoding.UTF8.GetBytes(text);ulong ac=0,bc=0;if(a.open(tables,tables.Length,ref ac)!=0||b.open(tables,tables.Length,ref bc)!=0)throw new Exception("catalog");
  int checks=0,selected=0,missing=0;
  foreach(int mode in new[]{1,3,4,7})foreach(int slot in new[]{5,6,7,8}){
   ulong ap=0,bp=0;if(a.prep(mode,ref ap)!=0||b.prep(mode,ref bp)!=0)throw new Exception("prep");var user=new int[18];user[1]=50;user[2]=100;var ao=new byte[4096];var bo=new byte[4096];int aw=0,bw=0;if(a.user(ap,1,user,18,0,ao,ao.Length,ref aw)!=0||b.user(bp,1,user,18,0,bo,bo.Length,ref bw)!=0)throw new Exception("user");
   foreach(int tribe in new[]{0,1,2})foreach(int hp in new[]{0,1,50})foreach(int rate in new[]{0,50,100}){
    if(a.clear(ap)!=0||b.clear(bp)!=0)throw new Exception("clear");var data=new byte[276];Put(data,0,901);Put(data,4,tribe);Put64(data,8,hp);Put64(data,16,100);Put(data,24,rate);
    if(a.enemy(ap,slot,data,data.Length)!=0||b.enemy(bp,slot,data,data.Length)!=0)throw new Exception("enemy registration");
    foreach(int want in new[]{0,1,2,3}){var wire=new byte[2436];Put(wire,228,1);Put(wire,132,1);for(int i=0;i<5;i++)Put64(wire,136+i*8,want);
     int x=12345,y=12345;int left=a.query(ac,ap,0,1,wire,wire.Length,ref x),right=b.query(bc,bp,0,1,wire,wire.Length,ref y);
     if(left!=right||x!=y)throw new Exception("mode="+mode+" slot="+slot+" tribe="+tribe+" hp="+hp+" rate="+rate+" want="+want+" original="+left+":"+x+" rebuilt="+right+":"+y);
     if(left==0&&x==0)selected++;if(left==0&&x==-1)missing++;checks++;
    }
   }
   foreach(int kind in new[]{0,1,2,3}){
    if(a.clear(ap)!=0||b.clear(bp)!=0)throw new Exception("clear invalid");var data=new byte[276];Put(data,0,901);Put(data,4,1);Put64(data,8,50);Put64(data,16,100);
    if(kind==0)Put(data,112,99);
    if(kind==1)Put(data,152,slot-4);
    if(kind==2)Put(data,152,slot==5?4:1);
    if(kind==3){Put(data,176,1);Put(data,180,-1);Put(data,184,1);}
    if(a.enemy(ap,slot,data,data.Length)!=0||b.enemy(bp,slot,data,data.Length)!=0)throw new Exception("invalid enemy registration");
    var wire=new byte[2436];Put(wire,228,1);Put(wire,132,1);int x=12345,y=12345;int left=a.query(ac,ap,0,1,wire,wire.Length,ref x),right=b.query(bc,bp,0,1,wire,wire.Length,ref y);
    if(left!=right||x!=y||left==0||x!=12345)throw new Exception("invalid kind="+kind+" mode="+mode+" slot="+slot+" original="+left+":"+x+" rebuilt="+right+":"+y);checks++;
   }
   if(a.clear(ap)!=0||b.clear(bp)!=0)throw new Exception("clear multi");
   var one=new byte[276];var two=new byte[276];Put(one,0,901);Put(two,0,902);Put(one,4,2);Put(two,4,4);Put64(one,16,100);Put64(two,16,100);Put(two,152,1);
   if(a.enemy(ap,5,one,one.Length)!=0||b.enemy(bp,5,one,one.Length)!=0||a.enemy(ap,8,two,two.Length)!=0||b.enemy(bp,8,two,two.Length)!=0)throw new Exception("multi registration");
   foreach(int want in new[]{0,2,4,9}){var wire=new byte[2436];Put(wire,228,1);Put(wire,132,1);for(int i=0;i<5;i++)Put64(wire,136+i*8,want);
    int x=12345,y=12345;int left=a.query(ac,ap,0,1,wire,wire.Length,ref x),right=b.query(bc,bp,0,1,wire,wire.Length,ref y);
    if(left!=right||x!=y)throw new Exception("multi mode="+mode+" want="+want+" original="+left+":"+x+" rebuilt="+right+":"+y);checks++;
   }
  }
  if(selected==0||missing==0)throw new Exception("both branch outcomes required");Console.WriteLine("{\"checks\":"+checks+",\"selected\":"+selected+",\"notSelected\":"+missing+"}");
 }
}
