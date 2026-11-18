using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

class RegisteredBattleDifferential {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr LoadLibrary(string path);
 [DllImport("kernel32.dll",CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr module,string name);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Open(byte[] data,int count,ref ulong handle);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Prep(int mode,ref ulong handle);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int User(ulong prep,int member,int[] data,int count,int seed,byte[] output,int capacity,ref int written);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Card(ulong prep,int member,int slot,byte[] data,int count);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Spheres(ulong prep,int member,byte[] data,int count);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Enemy(ulong prep,int member,byte[] data,int count);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Create(ulong catalog,ulong prep,byte[] data,int count,ref ulong handle);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Step(ulong battle,int operation,byte[] data,int count);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Read(ulong battle,byte[] output,int capacity,ref int written);
 class Lib {public Open open;public Prep prep;public User user;public Card card,buddy;public Spheres spheres;public Enemy enemy;public Create create;public Step step;public Read read,log;
  T Bind<T>(IntPtr m,string n){return (T)(object)Marshal.GetDelegateForFunctionPointer(GetProcAddress(m,n),typeof(T));}
  public Lib(string path){var m=LoadLibrary(Path.GetFullPath(path));if(m==IntPtr.Zero)throw new Exception("load");open=Bind<Open>(m,"battlecore_catalog_open_tables");prep=Bind<Prep>(m,"battlecore_preparation_open");user=Bind<User>(m,"battlecore_preparation_user");card=Bind<Card>(m,"battlecore_preparation_card");buddy=Bind<Card>(m,"battlecore_preparation_buddy");spheres=Bind<Spheres>(m,"battlecore_preparation_spheres");enemy=Bind<Enemy>(m,"battlecore_preparation_enemy");create=Bind<Create>(m,"battlecore_context_create_registered");step=Bind<Step>(m,"battlecore_step");read=Bind<Read>(m,"battlecore_result_read");log=Bind<Read>(m,"battlecore_input_log");}
 }
 static void Put(byte[] b,int at,int value){Buffer.BlockCopy(BitConverter.GetBytes(value),0,b,at,4);}
 static string Family(string name,params string[] values){var parts=new string[values.Length];for(int i=0;i<values.Length;i++)parts[i]="{\"name\":\""+values[i]+"\",\"value\":"+i+"}";return "{\"name\":\""+name+"\",\"values\":["+string.Join(",",parts)+"]}";}
 static void Equal(int a,int b,string label,int expected){if(a!=b||a!=expected)throw new Exception(label+" original="+a+" rebuilt="+b+" expected="+expected);}
 static string Advance(Lib a,Lib b,ulong ah,ulong bh,int op,string json,string outputDir){var input=json==null?null:Encoding.UTF8.GetBytes(json);Equal(a.step(ah,op,input,input==null?0:input.Length),b.step(bh,op,input,input==null?0:input.Length),"step "+op,0);var x=new byte[1048576];var y=new byte[1048576];int n=0,m=0;Equal(a.read(ah,x,x.Length,ref n),b.read(bh,y,y.Length,ref m),"read "+op,0);string left=Encoding.UTF8.GetString(x,0,n),right=Encoding.UTF8.GetString(y,0,m);if(left!=right){File.WriteAllText(Path.Combine(outputDir,"registered-original-step.txt"),left);File.WriteAllText(Path.Combine(outputDir,"registered-rebuilt-step.txt"),right);throw new Exception("step "+op+" CSV differs original="+n+" rebuilt="+m);}return left;}
 static void Main(string[] args){try{Run(args);}catch(Exception e){Console.Error.WriteLine(e.Message);Environment.Exit(1);}}
 static void Run(string[] args){
  var a=new Lib(args[0]);var b=new Lib(args[1]);string text=File.ReadAllText(args[2]).TrimEnd();string[] families={Family("SKILL_EXTEND_EXEC_ORDER","NONE"),Family("RARITY","NORMAL"),Family("SKILL_TYPE","ATTACK"),Family("SKILL_KIND","ATTACK"),Family("ATTR","FIRE"),Family("JOB_TYPE","NONE"),Family("ARTHUR_TYPE","NULL"),Family("SKILL_PHYSICS_TYPE","PHYSICS"),Family("SKILL_TARGET","ENEMY_ONE"),Family("SKILL_EXEC_TIMING","NORMAL"),Family("SKILL_ROLE","NONE","ATTACK_AA"),Family("SKILL_ROLE_TARGET","SELECT"),Family("SKILL_ROLE_BATTLE_PARAM","ATK")};text=text.Substring(0,text.Length-1)+",\"enums\":["+string.Join(",",families)+"]}";
  var tables=Encoding.UTF8.GetBytes(text);ulong ac=0,bc=0;Equal(a.open(tables,tables.Length,ref ac),b.open(tables,tables.Length,ref bc),"catalog",0);ulong ap=0,bp=0;Equal(a.prep(1,ref ap),b.prep(1,ref bp),"preparation",0);
  var request=Encoding.UTF8.GetBytes("{\"mode\":1,\"enemy_type\":0,\"config\":{\"enemy_party_id\":1}}" );ulong ah=999,bh=999;
  Equal(a.create(999999,ap,request,request.Length,ref ah),b.create(999999,bp,request,request.Length,ref bh),"unknown catalog",4);
  Equal(a.create(ac,999999,request,request.Length,ref ah),b.create(bc,999999,request,request.Length,ref bh),"unknown preparation",4);
  Equal(a.create(ac,0,request,request.Length,ref ah),b.create(bc,0,request,request.Length,ref bh),"no preparation",4);
  Equal(a.create(ac,ap,request,request.Length,ref ah),b.create(bc,bp,request,request.Length,ref bh),"missing party",2);if(ah!=999||bh!=999)throw new Exception("failure changed handle");
  for(int member=1;member<=4;member++){
   var user=new int[18];user[1]=1000;user[2]=1000;user[3]=100;user[4]=100;user[5]=100;user[7]=1;user[8]=10;user[17]=5;var ao=new byte[4096];var bo=new byte[4096];int aw=0,bw=0;
   Equal(a.user(ap,member,user,18,12345,ao,ao.Length,ref aw),b.user(bp,member,user,18,12345,bo,bo.Length,ref bw),"user",0);
   for(int slot=1;slot<=20;slot++){var card=new byte[4960];if(slot<=10){Put(card,0,slot);Put(card,8,1);Put(card,28,1001);Put(card,28+228,777);Put(card,28+232,1);Put(card,28+248,10);Put(card,28+2432,1);}Equal(a.card(ap,member,slot,card,card.Length),b.card(bp,member,slot,card,card.Length),"card",0);}
   var spheres=new byte[21984];Equal(a.spheres(ap,member,spheres,spheres.Length),b.spheres(bp,member,spheres,spheres.Length),"spheres",0);
   for(int slot=1;slot<=5;slot++){var buddy=new byte[17096];Equal(a.buddy(ap,member,slot,buddy,buddy.Length),b.buddy(bp,member,slot,buddy,buddy.Length),"buddy",0);}
  }
  ah=0;bh=0;int left=a.create(ac,ap,request,request.Length,ref ah),right=b.create(bc,bp,request,request.Length,ref bh);Equal(left,right,"registered create",0);if(ah==0||bh==0)throw new Exception("no handles");
  {var x=new byte[4096];var y=new byte[4096];int n=0,m=0;int original=a.log(ah,x,x.Length,ref n),rebuilt=b.log(bh,y,y.Length,ref m);if(original!=0||rebuilt!=2)throw new Exception("expected known journal difference original="+original+" rebuilt="+rebuilt);}
  Equal(a.step(ah,1,null,0),b.step(bh,1,null,0),"start",0);var ar=new byte[1048576];var br=new byte[1048576];int an=0,bn=0;Equal(a.read(ah,ar,ar.Length,ref an),b.read(bh,br,br.Length,ref bn),"read",0);string ast=Encoding.UTF8.GetString(ar,0,an),bst=Encoding.UTF8.GetString(br,0,bn);if(ast!=bst){File.WriteAllText(Path.Combine(args[3],"registered-original-start.txt"),ast);File.WriteAllText(Path.Combine(args[3],"registered-rebuilt-start.txt"),bst);throw new Exception("start CSV differs original="+an+" rebuilt="+bn);}
  Advance(a,b,ah,bh,2,null,args[3]);string phase=Advance(a,b,ah,bh,3,null,args[3]);var hand=new int[4];foreach(string line in phase.Split('\n')){var fields=line.Split(',');if(fields.Length>=4&&fields[0]=="20"){int m=Int32.Parse(fields[1]);if(m>=1&&m<=4&&hand[m-1]==0)hand[m-1]=Int32.Parse(fields[2]);}}
  for(int m=1;m<=4;m++){if(hand[m-1]==0)throw new Exception("missing dealt card");Advance(a,b,ah,bh,4,"{\"member_type\":"+m+",\"card_types\":["+hand[m-1]+",0,0,0,0],\"targets\":[5,0,0,0,0]}",args[3]);}
  string attack=Advance(a,b,ah,bh,5,null,args[3]);Advance(a,b,ah,bh,8,null,args[3]);Advance(a,b,ah,bh,6,null,args[3]);Advance(a,b,ah,bh,9,null,args[3]);Advance(a,b,ah,bh,7,null,args[3]);Advance(a,b,ah,bh,14,"{}",args[3]);
  var foe=new byte[276];Put(foe,0,901);Put(foe,4,2);Buffer.BlockCopy(BitConverter.GetBytes(600L),0,foe,8,8);Buffer.BlockCopy(BitConverter.GetBytes(1200L),0,foe,16,8);Put(foe,24,50);Equal(a.enemy(ap,5,foe,foe.Length),b.enemy(bp,5,foe,foe.Length),"registered enemy",0);
  var req2=Encoding.UTF8.GetBytes("{\"mode\":1,\"enemy_type\":0,\"config\":{}}");ulong ax=0,bx=0;Equal(a.create(ac,ap,req2,req2.Length,ref ax),b.create(bc,bp,req2,req2.Length,ref bx),"create with registered enemy",0);string enemyStart=Advance(a,b,ax,bx,1,null,args[3]);if(!("\n"+enemyStart+"\n").Contains("\n6,5,300,600,"))throw new Exception("registered enemy HP scaling missing");
  Console.WriteLine("{\"checks\":19,\"startBytes\":"+an+",\"attackBytes\":"+Encoding.UTF8.GetByteCount(attack)+",\"steps\":14,\"registeredEnemyStartBytes\":"+Encoding.UTF8.GetByteCount(enemyStart)+",\"knownJournalDifference\":true}");
 }
}
