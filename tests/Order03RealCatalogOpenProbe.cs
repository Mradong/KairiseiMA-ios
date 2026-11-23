using System;
using System.IO;
using System.Runtime.InteropServices;
class Order03RealCatalogOpenProbe {
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr LoadLibrary(string path);
 [DllImport("kernel32.dll",CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr module,string name);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Open(byte[] data,int length,ref ulong handle);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Close(ulong handle);
 static int[] Run(string dll,byte[] data) {
  var m=LoadLibrary(Path.GetFullPath(dll));if(m==IntPtr.Zero)throw new Exception("DLL load failed");
  var open=(Open)Marshal.GetDelegateForFunctionPointer(GetProcAddress(m,"battlecore_catalog_open_tables"),typeof(Open));
  var close=(Close)Marshal.GetDelegateForFunctionPointer(GetProcAddress(m,"battlecore_catalog_close"),typeof(Close));
  ulong h=0;int status=open(data,data.Length,ref h);int closed=-1;if(status==0)closed=close(h);return new[]{status,closed};
 }
 static int Main(string[] args){try {byte[] tables=File.ReadAllBytes(args[2]);var a=Run(args[0],tables);var b=Run(args[1],tables);Console.WriteLine("{\"originalOpen\":"+a[0]+",\"originalClose\":"+a[1]+",\"rebuiltOpen\":"+b[0]+",\"rebuiltClose\":"+b[1]+"}");return 0;}catch(Exception e){Console.Error.WriteLine(e.GetType().Name);return 1;}}
}
