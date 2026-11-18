using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

class PreparedCardQueryDifferential {
 [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] static extern IntPtr LoadLibrary(string path);
 [DllImport("kernel32.dll", CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr module, string name);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Open(byte[] data,int count,ref ulong handle);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Prep(int mode,ref ulong handle);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int User(ulong prep,int member,int[] data,int count,int seed,byte[] output,int capacity,ref int written);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Card(ulong prep,int member,int slot,byte[] data,int count);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Query(ulong catalog,ulong prep,ulong battle,int member,byte[] skill,int count,ref int result);
 class Lib {
  public Open open;public Prep prep;public User user;public Card card;public Query query;
  T Bind<T>(IntPtr m,string n){return (T)(object)Marshal.GetDelegateForFunctionPointer(GetProcAddress(m,n),typeof(T));}
  public Lib(string path){var m=LoadLibrary(Path.GetFullPath(path));if(m==IntPtr.Zero)throw new Exception("load");open=Bind<Open>(m,"battlecore_catalog_open_tables");prep=Bind<Prep>(m,"battlecore_preparation_open");user=Bind<User>(m,"battlecore_preparation_user");card=Bind<Card>(m,"battlecore_preparation_card");query=Bind<Query>(m,"battlecore_skill_extend_index");}
 }
 static void Put(byte[] b,int at,int value){Buffer.BlockCopy(BitConverter.GetBytes(value),0,b,at,4);}
 static void Main(string[] args){
  var a=new Lib(args[0]);var b=new Lib(args[1]);string text=File.ReadAllText(args[2]).TrimEnd();
  text=text.Substring(0,text.Length-1)+",\"enums\":[{\"name\":\"SKILL_EXTEND_EXEC_ORDER\",\"values\":[{\"name\":\"NONE\",\"value\":0},{\"name\":\"SELF_MAIN_DECK_TAG\",\"value\":31},{\"name\":\"SELF_SUPPORT_DECK_TAG\",\"value\":32},{\"name\":\"SELF_ALL_DECK_TAG\",\"value\":33}]},{\"name\":\"RARITY\",\"values\":[{\"name\":\"NORMAL\",\"value\":0}]}]}";
  var tables=Encoding.UTF8.GetBytes(text);ulong ac=0,bc=0;if(a.open(tables,tables.Length,ref ac)!=0||b.open(tables,tables.Length,ref bc)!=0)throw new Exception("catalog");
  int checks=0,selected=0,missing=0;
  foreach(int slot in new[]{1,10,11,20}){
   ulong ap=0,bp=0;if(a.prep(1,ref ap)!=0||b.prep(1,ref bp)!=0)throw new Exception("prep");
   var user=new int[18];user[1]=50;user[2]=100;var ao=new byte[4096];var bo=new byte[4096];int aw=0,bw=0;
   if(a.user(ap,1,user,18,0,ao,ao.Length,ref aw)!=0||b.user(bp,1,user,18,0,bo,bo.Length,ref bw)!=0)throw new Exception("user");
   foreach(int tag in new[]{0,1,2}){
    var card=new byte[4960];Put(card,0,1);Put(card,8,1);Put(card,4928,tag);
    int sa=a.card(ap,1,slot,card,card.Length),sb=b.card(bp,1,slot,card,card.Length);if(sa!=0||sb!=sa)throw new Exception("card registration");
    foreach(int cond in new[]{31,32,33})foreach(int wanted in new[]{0,1,2,10})foreach(int threshold in new[]{1,2}){
     var wire=new byte[2436];Put(wire,228,1);Put(wire,132,cond);Put(wire,136,wanted);Put(wire,160,threshold);
     int x=12345,y=12345;int left=a.query(ac,ap,0,1,wire,wire.Length,ref x),right=b.query(bc,bp,0,1,wire,wire.Length,ref y);
     if(left!=right||x!=y)throw new Exception("slot="+slot+" tag="+tag+" cond="+cond+" wanted="+wanted+" threshold="+threshold+" original="+left+":"+x+" rebuilt="+right+":"+y);
     if(left==0&&x==0)selected++;if(left==0&&x==-1)missing++;checks++;
    }
   }
   foreach(int kind in new[]{0,1,2,3}){
    var card=new byte[4960];Put(card,0,kind==0?999:1);Put(card,8,1);
    if(kind==1)Put(card,20,99);
    if(kind==2)Put(card,28+2432,-1);
    if(kind==3)Put(card,28+2436+2432,-1);
    int sa=a.card(ap,1,slot,card,card.Length),sb=b.card(bp,1,slot,card,card.Length);
    if(sa!=0||sb!=sa)throw new Exception("invalid-query card registration");
    var wire=new byte[2436];Put(wire,228,1);Put(wire,132,31);Put(wire,136,1);Put(wire,160,1);
    int x=12345,y=12345;int left=a.query(ac,ap,0,1,wire,wire.Length,ref x),right=b.query(bc,bp,0,1,wire,wire.Length,ref y);
    if(left!=right||x!=y||left==0||x!=12345)throw new Exception("invalid card kind="+kind+" original="+left+":"+x+" rebuilt="+right+":"+y);checks++;
   }
   foreach(int secondTag in new[]{1,2}){
    var first=new byte[4960];var second=new byte[4960];Put(first,0,1);Put(second,0,1);Put(first,8,1);Put(second,8,1);Put(first,4928,1);Put(second,4928,secondTag);
    int other=slot==1?2:1;
    if(a.card(ap,1,slot,first,first.Length)!=0||b.card(bp,1,slot,first,first.Length)!=0||a.card(ap,1,other,second,second.Length)!=0||b.card(bp,1,other,second,second.Length)!=0)throw new Exception("duplicate setup");
    var wire=new byte[2436];Put(wire,228,1);Put(wire,132,33);Put(wire,136,1);Put(wire,160,1);
    int x=12345,y=12345;int left=a.query(ac,ap,0,1,wire,wire.Length,ref x),right=b.query(bc,bp,0,1,wire,wire.Length,ref y);
    if(left!=right||x!=y)throw new Exception("duplicate slot="+slot+" secondTag="+secondTag+" original="+left+":"+x+" rebuilt="+right+":"+y);checks++;
   }
  }
  if(selected==0||missing==0)throw new Exception("both branch outcomes required");
  Console.WriteLine("{\"checks\":"+checks+",\"selected\":"+selected+",\"notSelected\":"+missing+"}");
 }
}
