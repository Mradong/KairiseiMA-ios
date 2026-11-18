using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

class PreparedSkillQueryDifferential {
 [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] static extern IntPtr LoadLibrary(string path);
 [DllImport("kernel32.dll", CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr module, string name);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Open(byte[] data, int count, ref ulong handle);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Prep(int mode, ref ulong handle);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int User(ulong handle, int member, int[] data, int count, int seed, byte[] output, int capacity, ref int written);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Query(ulong catalog, ulong prep, ulong battle, int member, byte[] skill, int count, ref int result);
 class Lib {
  public Open open; public Prep prep; public User user; public Query query;
  T Bind<T>(IntPtr m,string n) { return (T)(object)Marshal.GetDelegateForFunctionPointer(GetProcAddress(m,n),typeof(T)); }
  public Lib(string path) { var m=LoadLibrary(Path.GetFullPath(path)); if(m==IntPtr.Zero) throw new Exception("load failed");
   open=Bind<Open>(m,"battlecore_catalog_open_tables"); prep=Bind<Prep>(m,"battlecore_preparation_open"); user=Bind<User>(m,"battlecore_preparation_user"); query=Bind<Query>(m,"battlecore_skill_extend_index"); }
 }
 static void Put(byte[] b,int offset,int value){Buffer.BlockCopy(BitConverter.GetBytes(value),0,b,offset,4);}
 static void Main(string[] args) {
  var a=new Lib(args[0]); var b=new Lib(args[1]); string text=File.ReadAllText(args[2]).TrimEnd();
  text=text.Substring(0,text.Length-1)+",\"enums\":[{\"name\":\"SKILL_EXTEND_EXEC_ORDER\",\"values\":[{\"name\":\"NONE\",\"value\":0},{\"name\":\"SELF_HP_PER\",\"value\":8}]}]}";
  var tables=Encoding.UTF8.GetBytes(text); ulong ac=0,bc=0;
  if(a.open(tables,tables.Length,ref ac)!=0||b.open(tables,tables.Length,ref bc)!=0) throw new Exception("catalog setup");
  int checks=0, selected=0, missing=0;
  for(int mode=0;mode<8;mode++) {
   ulong ap=0,bp=0; if(a.prep(mode,ref ap)!=0||b.prep(mode,ref bp)!=0)throw new Exception("preparation setup");
   foreach(int member in (mode==3||mode==4 ? new[]{1,4,5,8} : new[]{1,4})) {
    foreach(int hp in new[]{0,1,25,50,75,100,150}) {
     int[] user=new int[18];user[1]=hp;user[2]=100;user[3]=123;user[4]=456;user[5]=789;user[6]=2;user[7]=3;
     var ao=new byte[4096];var bo=new byte[4096];int aw=0,bw=0;
     int sa=a.user(ap,member,user,18,7,ao,ao.Length,ref aw),sb=b.user(bp,member,user,18,7,bo,bo.Length,ref bw);
     if(sa!=0||sb!=sa||aw!=bw)throw new Exception("user setup mode="+mode+" member="+member);
     foreach(int min in new[]{0,25,50,100,150}) foreach(int max in new[]{25,50,100,150}) {
      var wire=new byte[2436]; Put(wire,228,11); Put(wire,132,8); Put(wire,136,min); Put(wire,144,max);
      int x=12345,y=12345;int left=a.query(ac,ap,0,member,wire,wire.Length,ref x),right=b.query(bc,bp,0,member,wire,wire.Length,ref y);
      if(left!=right||x!=y)throw new Exception("mode="+mode+" member="+member+" hp="+hp+" range="+min+".."+max+" original="+left+":"+x+" rebuilt="+right+":"+y);
      if(left==0 && x==0)selected++; if(left==0 && x==-1)missing++; checks++;
     }
    }
   }
  }
  if(selected==0||missing==0)throw new Exception("both selected and nonselected outcomes required");
  Console.WriteLine("{\"checks\":"+checks+",\"selected\":"+selected+",\"notSelected\":"+missing+"}");
 }
}
