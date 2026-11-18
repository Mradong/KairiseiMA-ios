using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Kairisei.Recovery;

partial class RegisteredBattleDifferential
{
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] private static extern bool SetDllDirectory(string directory);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int CloseHandle(ulong handle);
    private static int managedScenarios, managedOperations;

    private static void ManagedText(string reference,string actual,string label)
    {
        managedOperations++;
        if(reference!=actual)throw new Exception("Unity-consumer adapter "+label+" differs from original DLL");
    }

    private static void RunManagedCases(string original,string rebuilt,byte[] tables,string dir)
    {
        if(!SetDllDirectory(Path.GetDirectoryName(Path.GetFullPath(rebuilt))))throw new Exception("Cannot set local test DLL search path");
        IntPtr module=LoadLibrary(Path.GetFullPath(original));
        CloseHandle closeCatalog=(CloseHandle)Marshal.GetDelegateForFunctionPointer(GetProcAddress(module,"battlecore_catalog_close"),typeof(CloseHandle));
        CloseHandle closePreparation=(CloseHandle)Marshal.GetDelegateForFunctionPointer(GetProcAddress(module,"battlecore_preparation_close"),typeof(CloseHandle));
        CloseHandle closeBattle=(CloseHandle)Marshal.GetDelegateForFunctionPointer(GetProcAddress(module,"battlecore_context_close"),typeof(CloseHandle));
        Lib reference=new Lib(original);
        for(int scenario=0;scenario<3;scenario++)
        {
            ulong catalog=0,prep=0,battle=0;
            NativeRegisteredBattle adapter=null;
            try
            {
                if(reference.open(tables,tables.Length,ref catalog)!=0 || reference.prep(1,ref prep)!=0)throw new Exception("Original managed reference preparation");
                adapter=new NativeRegisteredBattle(tables);
                for(int member=1;member<=4;member++)
                {
                    int[] user=new int[18];user[1]=1000;user[2]=1000;user[3]=100;user[4]=100;user[5]=100;user[7]=member;user[8]=10;user[17]=5;
                    byte[] output=new byte[4096];int written=0;
                    if(reference.user(prep,member,user,user.Length,54321,output,output.Length,ref written)!=0)throw new Exception("original user");
                    ManagedText(Encoding.UTF8.GetString(output,0,written),adapter.RegisterUser(member,user,54321),"user CSV");
                    for(int slot=1;slot<=20;slot++)
                    {
                        byte[] card=new byte[4960];
                        if(slot<=10)
                        {
                            Put(card,0,slot);Put(card,8,1);Put(card,28,1001);Put(card,28+228,777);Put(card,28+2432,1);
                            Put(card,28+232,1);Put(card,28+244,2);Put(card,28+248,9);Put(card,28+264,1);
                        }
                        if(reference.card(prep,member,slot,card,card.Length)!=0)throw new Exception("original card");
                        adapter.RegisterCard(member,slot,card);
                    }
                    byte[] spheres=new byte[21984];if(reference.spheres(prep,member,spheres,spheres.Length)!=0)throw new Exception("original spheres");adapter.RegisterSpheres(member,spheres);
                    for(int slot=1;slot<=5;slot++) {byte[] buddy=new byte[17096];if(reference.buddy(prep,member,slot,buddy,buddy.Length)!=0)throw new Exception("original buddy");adapter.RegisterBuddy(member,slot,buddy);}
                }
                byte[] foe=ManagedFoe(920,1);
                if(reference.enemy(prep,5,foe,foe.Length)!=0)throw new Exception("original enemy");adapter.RegisterEnemy(5,foe);
                byte[] request=Encoding.UTF8.GetBytes("{\"mode\":1,\"enemy_type\":0,\"config\":{}}");
                if(reference.create(catalog,prep,request,request.Length,ref battle)!=0)throw new Exception("original create");
                ManagedText(ReferenceStep(reference,battle,1,null),adapter.Start(0,0),"start");
                ManagedJournal(reference,battle,adapter);
                PlayManagedWave(reference,battle,adapter);
                foe=ManagedFoe(921,25);if(reference.enemy(prep,5,foe,foe.Length)!=0)throw new Exception("original next enemy");adapter.RegisterEnemy(5,foe);
                if(reference.next(battle,prep,scenario)!=0)throw new Exception("original next");
                ManagedText(ReferenceRead(reference,battle),adapter.NextWave(scenario),"next start");ManagedJournal(reference,battle,adapter);
                PlayManagedWave(reference,battle,adapter);
                adapter.Dispose();adapter.Dispose();
                bool rejected=false;try {adapter.InputJournal();}catch(ObjectDisposedException){rejected=true;}
                if(!rejected)throw new Exception("Disposed adapter accessed native handles");
                managedScenarios++;
            }
            finally
            {
                if(adapter!=null)adapter.Dispose();
                if(battle!=0)closeBattle(battle);
                if(prep!=0)closePreparation(prep);
                if(catalog!=0)closeCatalog(catalog);
            }
        }
    }

    private static byte[] ManagedFoe(int id,int hp)
    {
        byte[] data=new byte[276];Put(data,0,id);Put(data,4,3);
        Buffer.BlockCopy(BitConverter.GetBytes((long)hp),0,data,8,8);Buffer.BlockCopy(BitConverter.GetBytes((long)hp*2),0,data,16,8);return data;
    }
    private static string ReferenceRead(Lib reference,ulong battle)
    {
        byte[] output=new byte[1048576];int written=0;
        if(reference.read(battle,output,output.Length,ref written)!=0)throw new Exception("original adapter result read");return Encoding.UTF8.GetString(output,0,written);
    }
    private static string ReferenceStep(Lib reference,ulong battle,int op,string json)
    {
        byte[] input=json==null?null:Encoding.UTF8.GetBytes(json);
        if(reference.step(battle,op,input,input==null?0:input.Length)!=0)throw new Exception("original adapter step "+op);
        return ReferenceRead(reference,battle);
    }
    private static void ManagedJournal(Lib reference,ulong battle,NativeRegisteredBattle adapter)
    {
        byte[] output=new byte[1048576];int written=0;
        if(reference.log(battle,output,output.Length,ref written)!=0)throw new Exception("original adapter journal");
        ManagedText(Encoding.UTF8.GetString(output,0,written),adapter.InputJournal(),"journal");
    }
    private static string ManagedStep(Lib reference,ulong battle,NativeRegisteredBattle adapter,int op,string json)
    {
        string output=adapter.Advance(op,json);ManagedText(ReferenceStep(reference,battle,op,json),output,"step "+op);ManagedJournal(reference,battle,adapter);return output;
    }
    private static void PlayManagedWave(Lib reference,ulong battle,NativeRegisteredBattle adapter)
    {
        ManagedStep(reference,battle,adapter,2,null);
        string dealt=ManagedStep(reference,battle,adapter,3,null);int[] cards=new int[4];
        foreach(string line in dealt.Split('\n')) {string[] row=line.Split(',');if(row.Length>=4&&row[0]=="20"){int member=Int32.Parse(row[1]);if(member>=1&&member<=4&&cards[member-1]==0)cards[member-1]=Int32.Parse(row[2]);}}
        for(int member=1;member<=4;member++)
        {
            if(cards[member-1]==0)throw new Exception("Adapter deal missing card");
            ManagedStep(reference,battle,adapter,4,"{\"member_type\":"+member+",\"card_types\":["+cards[member-1]+",0,0,0,0],\"targets\":[5,0,0,0,0]}");
        }
        string attack=ManagedStep(reference,battle,adapter,5,null);
        if(!("\n"+attack+"\n").Contains("\n2,1\n")||!attack.Contains("60,5,0,-9,"))throw new Exception("Adapter did not consume actual winning damage");
    }
}
