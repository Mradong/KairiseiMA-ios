using System;
using System.IO;
using System.Drawing;
using Kairisei.Recovery;

public static class RecoveryAtlasTests
{
    private static int checks;
    private static void Check(bool value,string name) { if(!value) throw new Exception(name); checks++; }
    private static void Reject(Action action,string name) { try { action(); } catch(FormatException) { checks++; return; } throw new Exception("accepted "+name); }
    public static void Main(string[] args)
    {
        string json=File.ReadAllText(args[0]);
        using (Image image=Image.FromFile(args[1]))
        {
            Check(image.Width==512 && image.Height==1024,"original atlas dimensions");
            RecoveryAtlas atlas=new RecoveryAtlas(json,image.Width,image.Height);
            int[] xs={121,121,224,224}, ys={458,428,397,367};
            for(int role=1;role<=4;role++)
            {
                float[] uv=atlas.Frame("BattleLo_Job0"+role+"_01.png");
                Check(Math.Abs(uv[0]-xs[role-1]/512f)<0.000001f && Math.Abs(uv[1]-(1024-ys[role-1]-29)/1024f)<0.000001f && Math.Abs(uv[2]-28/512f)<0.000001f && Math.Abs(uv[3]-29/1024f)<0.000001f,"role UV "+role);
            }
            Reject(delegate{atlas.Frame("missing.png");},"missing frame");
            Reject(delegate{new RecoveryAtlas(json,256,1024);},"dimension mismatch");
        }
        string prefix="{\"meta\":{\"size\":{\"w\":10,\"h\":10}},\"frames\":{\"a\":{\"frame\":{\"x\":9,\"y\":0,\"w\":2,\"h\":1},\"rotated\":false,\"trimmed\":false}}}";
        Reject(delegate{new RecoveryAtlas(prefix,10,10).Frame("a");},"out of bounds");
        Reject(delegate{new RecoveryAtlas(prefix.Replace("\"x\":9", "\"x\":0").Replace("\"rotated\":false", "\"rotated\":true"),10,10).Frame("a");},"rotated frame");
        Reject(delegate{new RecoveryAtlas(prefix.Replace("\"x\":9", "\"x\":0").Replace("\"trimmed\":false", "\"trimmed\":true"),10,10).Frame("a");},"trimmed frame");
        Console.WriteLine("PASS "+checks+" atlas checks");
    }
}
