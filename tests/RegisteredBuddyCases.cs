using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Kairisei.Recovery;

partial class RegisteredBattleDifferential
{
    private static int buddyCases;

    private static byte[] BuddyAttack(int id,int damage,int level)
    {
        byte[] skill=new byte[2436];
        Put(skill,0,id);Put(skill,228,id+100);Put(skill,2432,level);
        Put(skill,232,1);Put(skill,244,2);Put(skill,248,damage);Put(skill,264,1);
        return skill;
    }

    private static void RunBuddyCases(string original,string rebuilt,byte[] tables,string dir)
    {
        IntPtr module=LoadLibrary(Path.GetFullPath(original));
        CloseHandle closeCatalog=(CloseHandle)Marshal.GetDelegateForFunctionPointer(GetProcAddress(module,"battlecore_catalog_close"),typeof(CloseHandle));
        CloseHandle closePreparation=(CloseHandle)Marshal.GetDelegateForFunctionPointer(GetProcAddress(module,"battlecore_preparation_close"),typeof(CloseHandle));
        CloseHandle closeBattle=(CloseHandle)Marshal.GetDelegateForFunctionPointer(GetProcAddress(module,"battlecore_context_close"),typeof(CloseHandle));
        ConfigureHandle configure=(ConfigureHandle)Marshal.GetDelegateForFunctionPointer(GetProcAddress(module,"battlecore_preparation_configure"),typeof(ConfigureHandle));
        Lib reference=new Lib(original);
        for(int scenario=1;scenario<=6;scenario++)
        {
            int equippedSlot=scenario==6?1:scenario;
            bool passiveCase=scenario==6;
            ulong catalog=0,prep=0,battle=0;
            NativeRegisteredBattle adapter=null;
            try
            {
                if(reference.open(tables,tables.Length,ref catalog)!=0 || reference.prep(1,ref prep)!=0)throw new Exception("Buddy catalog/preparation");
                adapter=new NativeRegisteredBattle(tables);
                for(int member=1;member<=4;member++)
                {
                    int[] user=new int[18];user[1]=1000;user[2]=1000;user[3]=100;user[4]=100;user[5]=100;user[7]=member;user[8]=10;user[17]=5;
                    byte[] output=new byte[4096];int written=0;
                    if(reference.user(prep,member,user,user.Length,54321,output,output.Length,ref written)!=0)throw new Exception("Buddy user");
                    ManagedText(Encoding.UTF8.GetString(output,0,written),adapter.RegisterUser(member,user,54321),"buddy user");
                    if(configure(prep,3,member,200)!=0)throw new Exception("Original buddy gauge");
                    adapter.SetInitialBurst(member,200);
                    for(int slot=1;slot<=20;slot++)
                    {
                        byte[] card=new byte[4960];
                        if(slot<=10)
                        {
                            Put(card,0,slot);Put(card,8,1);
                            Buffer.BlockCopy(BuddyAttack(1001,9,1),0,card,28,2436);
                        }
                        if(reference.card(prep,member,slot,card,card.Length)!=0)throw new Exception("Original buddy card");
                        adapter.RegisterCard(member,slot,card);
                    }
                    byte[] spheres=new byte[21984];
                    if(reference.spheres(prep,member,spheres,spheres.Length)!=0)throw new Exception("Original buddy spheres");
                    adapter.RegisterSpheres(member,spheres);
                    for(int slot=1;slot<=5;slot++)
                    {
                        byte[] buddy=new byte[17096];
                        if(slot==equippedSlot)
                        {
                            Put(buddy,0,1);Put(buddy,4,1);
                            if(slot==1)
                            {
                                for(int skill=0;skill<3;skill++)
                                {
                                    int start=8+skill*4884;
                                    Buffer.BlockCopy(BuddyAttack(4001+skill,13+skill,2+skill),0,buddy,start,2436);
                                    Put(buddy,start+4872,20+10*skill);Put(buddy,start+4876,3+skill);Put(buddy,start+4880,1);
                                }
                                if(passiveCase)
                                {
                                    byte[] passive=BuddyAttack(5001,3,7);
                                    Put(passive,44,1);Put(passive,244,512);
                                    Buffer.BlockCopy(passive,0,buddy,14660,passive.Length);
                                }
                            }
                        }
                        if(reference.buddy(prep,member,slot,buddy,buddy.Length)!=0)throw new Exception("Original nonzero buddy");
                        adapter.RegisterBuddy(member,slot,buddy);
                    }
                }
                byte[] foe=ManagedFoe(930,50);
                if(reference.enemy(prep,5,foe,foe.Length)!=0)throw new Exception("Original buddy enemy");
                adapter.RegisterEnemy(5,foe);
                byte[] request=Encoding.UTF8.GetBytes("{\"mode\":1,\"enemy_type\":0,\"config\":{}}");
                if(reference.create(catalog,prep,request,request.Length,ref battle)!=0)throw new Exception("Original buddy create");
                string started=ReferenceStep(reference,battle,1,null),actual=adapter.Start(0,0);
                ManagedText(started,actual,"buddy start");ManagedJournal(reference,battle,adapter);
                if(!started.Contains("321,1,"+equippedSlot+",1\n"))throw new Exception("Equipped buddy missing from start");
                File.WriteAllText(Path.Combine(dir,"registered-buddy-case-"+scenario+"-start.txt"),started);
                if(equippedSlot==1)
                {
                    string turn=ManagedStep(reference,battle,adapter,2,null);
                    if(passiveCase)
                    {
                        if(!turn.Contains("700,1,0,5001,1,7,") || !turn.Contains("60,1,0,-3,"))throw new Exception("Registered buddy passive not executed");
                        File.WriteAllText(Path.Combine(dir,"registered-buddy-passive.txt"),turn);
                    }
                    string dealt=ManagedStep(reference,battle,adapter,3,null);
                    string bursts="";
                    for(int skill=1;skill<=3;skill++)
                    {
                        string result=ManagedStep(reference,battle,adapter,15,"{\"member_type\":1,\"target\":5,\"card_types\":[0,0,0,0,0],\"skill_slot\":"+skill+"}");
                        if(!result.Contains("700,1,"+skill+","+(4000+skill)+",5,"+(1+skill)+",") || !result.Contains("60,5,0,-"+(12+skill)+","))throw new Exception("Registered buddy did not execute packed skill/level/damage");
                        if(!result.Contains("320,1,"+skill+",0,"+(2+skill)+","+(10+10*skill)))throw new Exception("Registered buddy cooldown/gauge cost missing");
                        bursts+=result+"\n";
                    }
                    File.WriteAllText(Path.Combine(dir,passiveCase?"registered-buddy-passive-bursts.txt":"registered-buddy-bursts.txt"),bursts);
                    int[] cards=new int[4];
                    foreach(string line in dealt.Split('\n'))
                    {
                        string[] row=line.Split(',');
                        if(row.Length>=4 && row[0]=="20") {int member=Int32.Parse(row[1]);if(member>=1 && member<=4 && cards[member-1]==0)cards[member-1]=Int32.Parse(row[2]);}
                    }
                    for(int member=1;member<=4;member++)
                    {
                        if(cards[member-1]==0)throw new Exception("Missing buddy battle hand");
                        ManagedStep(reference,battle,adapter,4,"{\"member_type\":"+member+",\"card_types\":["+cards[member-1]+",0,0,0,0],\"targets\":[5,0,0,0,0]}");
                    }
                    string victory=ManagedStep(reference,battle,adapter,5,null);
                    if(!("\n"+victory+"\n").Contains("\n2,1\n"))throw new Exception("Buddy wave did not win after actual damage");
                    foe=ManagedFoe(931,25);
                    if(reference.enemy(prep,5,foe,foe.Length)!=0)throw new Exception("Original next buddy enemy");
                    adapter.RegisterEnemy(5,foe);
                    if(reference.next(battle,prep,0)!=0)throw new Exception("Original next buddy wave");
                    string next=adapter.NextWave(0);
                    ManagedText(ReferenceRead(reference,battle),next,"next buddy wave");ManagedJournal(reference,battle,adapter);
                    File.WriteAllText(Path.Combine(dir,passiveCase?"registered-buddy-passive-next.txt":"registered-buddy-next.txt"),next);
                    PlayManagedWave(reference,battle,adapter);
                }
                buddyCases++;
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
}
