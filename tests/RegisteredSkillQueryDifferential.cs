using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

class RegisteredSkillQueryDifferential {
 [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] static extern IntPtr LoadLibrary(string path);
 [DllImport("kernel32.dll", CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr module,string name);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Open(byte[] data,int count,ref ulong handle);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Prep(int mode,ref ulong handle);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int User(ulong prep,int member,int[] data,int count,int seed,byte[] output,int capacity,ref int written);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Card(ulong prep,int member,int slot,byte[] data,int count);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Query(ulong catalog,ulong prep,ulong battle,int member,byte[] skill,int count,ref int result);
 class Lib { public Open open; public Prep prep; public User user; public Card card; public Query query;
  T Bind<T>(IntPtr m,string n){return (T)(object)Marshal.GetDelegateForFunctionPointer(GetProcAddress(m,n),typeof(T));}
  public Lib(string path){var m=LoadLibrary(Path.GetFullPath(path));if(m==IntPtr.Zero)throw new Exception("load");open=Bind<Open>(m,"battlecore_catalog_open_tables");prep=Bind<Prep>(m,"battlecore_preparation_open");user=Bind<User>(m,"battlecore_preparation_user");card=Bind<Card>(m,"battlecore_preparation_card");query=Bind<Query>(m,"battlecore_skill_extend_index");}
 }
 static void Put(byte[] b,int at,int value){Buffer.BlockCopy(BitConverter.GetBytes(value),0,b,at,4);}
 static byte[] MakeCard(int cardID,int skillID,int functionID,int power){var card=new byte[4960];Put(card,0,cardID);Put(card,8,1);Put(card,28,skillID);Put(card,28+228,functionID);Put(card,28+232,1);Put(card,28+248,power);Put(card,28+2432,1);return card;}
 static string Family(string name,params string[] values){var parts=new string[values.Length];for(int i=0;i<values.Length;i++)parts[i]="{\"name\":\""+values[i]+"\",\"value\":"+i+"}";return "{\"name\":\""+name+"\",\"values\":["+string.Join(",",parts)+"]}";}
 static void Main(string[] args){
  var a=new Lib(args[0]);var b=new Lib(args[1]);string text=File.ReadAllText(args[2]).TrimEnd();
  string[] families={Family("SKILL_EXTEND_EXEC_ORDER","NONE","SELF_MAIN_DECK_ATTR","SELF_MAIN_DECK_SKILL_KIND"),Family("RARITY","NORMAL"),Family("SKILL_TYPE","ATTACK"),Family("SKILL_KIND","ATTACK","HEAL","NULL"),Family("ATTR","FIRE","ICE","NULL"),Family("JOB_TYPE","NONE"),Family("ARTHUR_TYPE","NULL"),Family("SKILL_PHYSICS_TYPE","PHYSICS"),Family("SKILL_TARGET","ENEMY_ONE"),Family("SKILL_EXEC_TIMING","NORMAL"),Family("SKILL_ROLE","NONE","ATTACK_AA"),Family("SKILL_ROLE_TARGET","SELECT"),Family("SKILL_ROLE_BATTLE_PARAM","ATK")};
  text=text.Substring(0,text.Length-1)+",\"enums\":["+string.Join(",",families)+"]}";var tables=Encoding.UTF8.GetBytes(text);ulong ac=0,bc=0;if(a.open(tables,tables.Length,ref ac)!=0||b.open(tables,tables.Length,ref bc)!=0)throw new Exception("catalog");
  ulong ap=0,bp=0;if(a.prep(1,ref ap)!=0||b.prep(1,ref bp)!=0)throw new Exception("prep");var user=new int[18];user[1]=50;user[2]=100;var ao=new byte[4096];var bo=new byte[4096];int aw=0,bw=0;if(a.user(ap,1,user,18,0,ao,ao.Length,ref aw)!=0||b.user(bp,1,user,18,0,bo,bo.Length,ref bw)!=0)throw new Exception("user");
  int checks=0,selected=0,missing=0;
  foreach(int skillAttr in new[]{0,1})foreach(int skillKind in new[]{0,1})foreach(int slot in new[]{1,3,11}){
   foreach(int clear in new[]{1,3,11}){var empty=new byte[4960];if(a.card(ap,1,clear,empty,empty.Length)!=0||b.card(bp,1,clear,empty,empty.Length)!=0)throw new Exception("clear slot");}
   var card=new byte[4960];Put(card,0,1);Put(card,8,1);int baseSkill=28;Put(card,baseSkill,1001);Put(card,baseSkill+8,skillKind);Put(card,baseSkill+12,skillAttr);Put(card,baseSkill+228,777);Put(card,baseSkill+232,1);Put(card,baseSkill+2432,1);
   int ca=a.card(ap,1,slot,card,card.Length),cb=b.card(bp,1,slot,card,card.Length);if(ca!=0||cb!=ca)throw new Exception("card registration");
   foreach(int cond in new[]{1,2})foreach(int want in new[]{0,1})foreach(int threshold in new[]{1,2,3}){
    var wire=new byte[2436];Put(wire,228,1);Put(wire,132,cond);Put(wire,136,want);Put(wire,144,2);Put(wire,152,threshold);
    int x=12345,y=12345;int left=a.query(ac,ap,0,1,wire,wire.Length,ref x),right=b.query(bc,bp,0,1,wire,wire.Length,ref y);
    if(left!=right||x!=y)throw new Exception("attr="+skillAttr+" kind="+skillKind+" slot="+slot+" cond="+cond+" want="+want+" threshold="+threshold+" original="+left+":"+x+" rebuilt="+right+":"+y);
    if(left==0&&x==0)selected++;if(left==0&&x==-1)missing++;checks++;
   }
  }
  foreach(int kind in new[]{0,1,2,3,4,5,6}){
   foreach(int clear in new[]{1,3,11}){var empty=new byte[4960];if(a.card(ap,1,clear,empty,empty.Length)!=0||b.card(bp,1,clear,empty,empty.Length)!=0)throw new Exception("clear slot");}
   var card=MakeCard(1,1001,777,1);
   switch(kind){case 0:Put(card,28+8,99);break;case 1:Put(card,28+228,0);break;case 2:Put(card,28+232,99);break;case 3:Put(card,28+236,99);break;case 4:Put(card,28+244,1);break;case 5:Put(card,28+132,99);break;case 6:Put(card,28+2432,-1);break;}
   if(a.card(ap,1,1,card,card.Length)!=0||b.card(bp,1,1,card,card.Length)!=0)throw new Exception("invalid skill registration");
   var wire=new byte[2436];Put(wire,228,1);int x=12345,y=12345;int left=a.query(ac,ap,0,1,wire,wire.Length,ref x),right=b.query(bc,bp,0,1,wire,wire.Length,ref y);
   if(left!=right||x!=y||left==0||x!=12345)throw new Exception("invalid kind="+kind+" original="+left+":"+x+" rebuilt="+right+":"+y);checks++;
  }
  foreach(bool sameSkill in new[]{true,false})foreach(bool changed in new[]{false,true}){
   foreach(int clear in new[]{1,3,11}){var empty=new byte[4960];if(a.card(ap,1,clear,empty,empty.Length)!=0||b.card(bp,1,clear,empty,empty.Length)!=0)throw new Exception("clear slot");}
   var one=MakeCard(1,1001,777,1);var two=MakeCard(2,sameSkill?1001:1002,777,changed?2:1);
   if(a.card(ap,1,1,one,one.Length)!=0||b.card(bp,1,1,one,one.Length)!=0||a.card(ap,1,3,two,two.Length)!=0||b.card(bp,1,3,two,two.Length)!=0)throw new Exception("duplicate setup");
   var wire=new byte[2436];Put(wire,228,1);int x=12345,y=12345;int left=a.query(ac,ap,0,1,wire,wire.Length,ref x),right=b.query(bc,bp,0,1,wire,wire.Length,ref y);
   if(left!=right||x!=y||(!changed&&left!=0)||(changed&&(left==0||x!=12345)))throw new Exception("shared sameSkill="+sameSkill+" changed="+changed+" original="+left+":"+x+" rebuilt="+right+":"+y);checks++;
  }
  if(selected==0||missing==0)throw new Exception("both branch outcomes required");Console.WriteLine("{\"checks\":"+checks+",\"selected\":"+selected+",\"notSelected\":"+missing+"}");
 }
}
