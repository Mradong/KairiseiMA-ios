using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
class Order03RoleAliasProbe {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr LoadLibrary(string path);
 [DllImport("kernel32.dll",CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr module,string name);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Open(byte[] b,int n,ref ulong h);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Prep(int mode,ref ulong h);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int User(ulong h,int member,int[] values,int n,int seed,byte[] output,int cap,ref int written);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Card(ulong h,int member,int slot,byte[] data,int n);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Query(ulong catalog,ulong prep,ulong battle,int member,byte[] skill,int n,ref int result);
 static T Bind<T>(IntPtr module,string name){var p=GetProcAddress(module,name);if(p==IntPtr.Zero)throw new Exception("missing export");return (T)(object)Marshal.GetDelegateForFunctionPointer(p,typeof(T));}
 static void Put(byte[] b,int offset,int n){Buffer.BlockCopy(BitConverter.GetBytes(n),0,b,offset,4);}
 static int[] Run(string dll,byte[] tables){
  var m=LoadLibrary(Path.GetFullPath(dll));if(m==IntPtr.Zero)throw new Exception("load failed");
  var open=Bind<Open>(m,"battlecore_catalog_open_tables");var prep=Bind<Prep>(m,"battlecore_preparation_open");var user=Bind<User>(m,"battlecore_preparation_user");var card=Bind<Card>(m,"battlecore_preparation_card");var query=Bind<Query>(m,"battlecore_skill_extend_index");
  ulong cat=0,p=0;if(open(tables,tables.Length,ref cat)!=0||prep(1,ref p)!=0)throw new Exception("catalog or preparation failed");
  var u=new int[18];u[1]=50;u[2]=100;var buf=new byte[4096];int written=0;if(user(p,1,u,u.Length,0,buf,buf.Length,ref written)!=0)throw new Exception("user failed");
  var c=new byte[4960];Put(c,0,1);Put(c,8,1);Put(c,28,1001);Put(c,28+8,1);Put(c,28+12,1);Put(c,28+44,3);Put(c,28+228,777);Put(c,28+232,1);Put(c,28+248,1);Put(c,28+2432,1);
  if(card(p,1,1,c,c.Length)!=0)throw new Exception("card failed");var skill=new byte[2436];Put(skill,228,1);int result=12345;int status=query(cat,p,0,1,skill,skill.Length,ref result);return new[]{status,result};
 }
 static int Main(string[] args){try{var t=File.ReadAllBytes(args[2]);var a=Run(args[0],t);var b=Run(args[1],t);Console.WriteLine("{\"originalStatus\":"+a[0]+",\"originalResult\":"+a[1]+",\"rebuiltStatus\":"+b[0]+",\"rebuiltResult\":"+b[1]+"}");return 0;}catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}
}
