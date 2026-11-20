using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kairisei.Recovery;

partial class RegisteredBattleDifferential
{
    private static int sphereCases;

    private static void PlaySphereCardWave(Lib reference,ulong battle,NativeRegisteredBattle adapter,int sphereSlot,int expectedCount,string evidence)
    {
        ManagedStep(reference,battle,adapter,2,null);
        string dealt=ManagedStep(reference,battle,adapter,3,null);
        File.WriteAllText(evidence,dealt);
        if(!dealt.Contains("301,1,"+sphereSlot+","+expectedCount+",2"))throw new Exception("Unexpected sphere use count at turn boundary");
        int[] cards=new int[4];
        foreach(string line in dealt.Split('\n'))
        {
            string[] row=line.Split(',');
            // Passing a turn or a sphere-only victory can retain a full hand,
            // so there may be no new-card (20) event. Read hand state too.
            if(row.Length>=4 && (row[0]=="20" || row[0]=="25"))
            {
                int member=Int32.Parse(row[1]);
                if(member>=1 && member<=4 && cards[member-1]==0)cards[member-1]=Int32.Parse(row[2]);
            }
        }
        for(int member=1;member<=4;member++)
        {
            if(cards[member-1]==0)throw new Exception("Sphere wave has no playable hand card");
            ManagedStep(reference,battle,adapter,4,"{\"member_type\":"+member+",\"card_types\":["+cards[member-1]+",0,0,0,0],\"targets\":[5,0,0,0,0]}");
        }
        string hit=ManagedStep(reference,battle,adapter,5,null);
        if(!("\n"+hit+"\n").Contains("\n2,1\n") || !hit.Contains("60,5,0,-9,"))throw new Exception("Sphere wave card damage did not win");
    }

    private static void RunSphereCases(string original,byte[] tables,string dir)
    {
        JObject snapshot=JObject.Parse(Encoding.UTF8.GetString(tables));
        ((JArray)snapshot["enums"]).Add(JObject.Parse(Family("SPHR_TYPE","NONE","NORMAL","CHALICE","EVOLUTION")));
        ((JArray)snapshot["enums"]).Add(JObject.Parse(Family("SPHR_PLAY_CONDITION","NONE","TURN_AFTER")));
        foreach(JObject family in (JArray)snapshot["enums"])
            if((string)family["name"]=="SKILL_TARGET")((JArray)family["values"]).Add(new JObject{{"name","SELF"},{"value",2}});
        foreach(JObject table in (JArray)snapshot["tables"])
            if((string)table["name"]=="sphr.csv") {string[] row=((string)table["text"]).TrimEnd().Split(',');row[7]="10";table["text"]=String.Join(",",row)+"\n";}
        byte[] sphereTables=Encoding.UTF8.GetBytes(snapshot.ToString(Formatting.None));
        IntPtr module=LoadLibrary(Path.GetFullPath(original));
        CloseHandle closeCatalog=(CloseHandle)Marshal.GetDelegateForFunctionPointer(GetProcAddress(module,"battlecore_catalog_close"),typeof(CloseHandle));
        CloseHandle closePreparation=(CloseHandle)Marshal.GetDelegateForFunctionPointer(GetProcAddress(module,"battlecore_preparation_close"),typeof(CloseHandle));
        CloseHandle closeBattle=(CloseHandle)Marshal.GetDelegateForFunctionPointer(GetProcAddress(module,"battlecore_context_close"),typeof(CloseHandle));
        Lib reference=new Lib(original);
        for(int scenario=1;scenario<=5;scenario++)
        {
            int sphereSlot=scenario<=3?scenario:1;
            bool delayed=scenario==4,chalice=scenario==5;
            ulong catalog=0,prep=0,battle=0;NativeRegisteredBattle adapter=null;
            try
            {
                if(reference.open(sphereTables,sphereTables.Length,ref catalog)!=0 || reference.prep(1,ref prep)!=0)throw new Exception("Sphere catalog/preparation");
                adapter=new NativeRegisteredBattle(sphereTables);
                for(int member=1;member<=4;member++)
                {
                    int[] user=new int[18];user[1]=1000;user[2]=1000;user[3]=100;user[4]=100;user[5]=100;user[7]=member;user[8]=10;user[17]=5;
                    byte[] output=new byte[4096];int written=0;
                    if(reference.user(prep,member,user,user.Length,54321,output,output.Length,ref written)!=0)throw new Exception("Sphere user");
                    ManagedText(Encoding.UTF8.GetString(output,0,written),adapter.RegisterUser(member,user,54321),"sphere user");
                    for(int slot=1;slot<=20;slot++)
                    {
                        byte[] card=new byte[4960];
                        if(slot<=10) {Put(card,0,slot);Put(card,8,1);Buffer.BlockCopy(BuddyAttack(1001,9,1),0,card,28,2436);}
                        if(reference.card(prep,member,slot,card,card.Length)!=0)throw new Exception("Original sphere card");
                        adapter.RegisterCard(member,slot,card);
                    }
                    for(int slot=1;slot<=5;slot++)
                    {
                        byte[] buddy=new byte[17096];
                        if(reference.buddy(prep,member,slot,buddy,buddy.Length)!=0)throw new Exception("Original sphere buddy");
                        adapter.RegisterBuddy(member,slot,buddy);
                    }
                    byte[] spheres=new byte[21984];int start=(sphereSlot-1)*7328;
                    Put(spheres,start,1);Put(spheres,start+4,2);Put(spheres,start+8,chalice?2:1);
                    byte[] skill=BuddyAttack(7001,11,3);Put(skill,28,2);
                    if(chalice) {Put(skill,44,2);Put(skill,244,512);}
                    Buffer.BlockCopy(skill,0,spheres,start+12,skill.Length);
                    if(delayed) {Put(spheres,start+7320,1);Put(spheres,start+7324,1);}
                    if(reference.spheres(prep,member,spheres,spheres.Length)!=0)throw new Exception("Original nonzero sphere registration");
                    adapter.RegisterSpheres(member,spheres);
                }
                byte[] foe=ManagedFoe(940,chalice?25:40);
                if(reference.enemy(prep,5,foe,foe.Length)!=0)throw new Exception("Original sphere enemy");adapter.RegisterEnemy(5,foe);
                byte[] request=Encoding.UTF8.GetBytes("{\"mode\":1,\"enemy_type\":0,\"config\":{}}");
                if(reference.create(catalog,prep,request,request.Length,ref battle)!=0)throw new Exception("Original sphere create");
                string started=ReferenceStep(reference,battle,1,null),actual=adapter.Start(0,0);
                ManagedText(started,actual,"sphere start");ManagedJournal(reference,battle,adapter);
                File.WriteAllText(Path.Combine(dir,"registered-sphere-case-"+scenario+"-start.txt"),started);
                string turn=ManagedStep(reference,battle,adapter,2,null);
                string dealt=ManagedStep(reference,battle,adapter,3,null);
                if(delayed)
                {
                    File.WriteAllText(Path.Combine(dir,"registered-sphere-delayed-unavailable.txt"),turn);
                    if(!turn.Contains("309,1,1,1,0,1"))throw new Exception("Delayed sphere should be unavailable on first turn");
                    for(int member=1;member<=4;member++)ManagedStep(reference,battle,adapter,4,"{\"member_type\":"+member+"}");
                    ManagedStep(reference,battle,adapter,5,null);ManagedStep(reference,battle,adapter,8,null);ManagedStep(reference,battle,adapter,6,null);ManagedStep(reference,battle,adapter,9,null);
                    turn=ManagedStep(reference,battle,adapter,2,null);dealt=ManagedStep(reference,battle,adapter,3,null);
                    if(!turn.Contains("309,1,1,1,1,0"))throw new Exception("Delayed sphere should become available on second turn");
                    File.WriteAllText(Path.Combine(dir,"registered-sphere-delayed-available.txt"),turn);
                }
                for(int member=1;member<=4;member++)
                    ManagedStep(reference,battle,adapter,4,"{\"member_type\":"+member+(member==1 && !chalice?",\"sphere_slot\":"+sphereSlot+",\"sphere_target\":5":"")+"}");
                string hit=ManagedStep(reference,battle,adapter,5,null);
                if(chalice)
                {
                    ManagedStep(reference,battle,adapter,12,"{\"member_type\":1,\"slot\":"+sphereSlot+"}");
                    hit=ManagedStep(reference,battle,adapter,8,null);
                }
                if(!hit.Contains("56,1,"+sphereSlot+",7001,"+(chalice?0:5)+",3,") || !hit.Contains("60,"+(chalice?1:5)+",0,-11,"))throw new Exception("Registered sphere skill/level/damage missing");
                File.WriteAllText(Path.Combine(dir,"registered-sphere-case-"+scenario+"-action.txt"),hit);
                if(!chalice)ManagedStep(reference,battle,adapter,8,null);
                ManagedStep(reference,battle,adapter,6,null);ManagedStep(reference,battle,adapter,9,null);
                PlaySphereCardWave(reference,battle,adapter,sphereSlot,1,Path.Combine(dir,"registered-sphere-case-"+scenario+"-remaining.txt"));
                foe=ManagedFoe(941,25);
                if(reference.enemy(prep,5,foe,foe.Length)!=0)throw new Exception("Original next sphere enemy");adapter.RegisterEnemy(5,foe);
                if(reference.next(battle,prep,0)!=0)throw new Exception("Original next sphere wave");
                string next=adapter.NextWave(0);
                ManagedText(ReferenceRead(reference,battle),next,"next sphere wave");ManagedJournal(reference,battle,adapter);
                File.WriteAllText(Path.Combine(dir,"registered-sphere-case-"+scenario+"-next.txt"),next);
                PlaySphereCardWave(reference,battle,adapter,sphereSlot,2,Path.Combine(dir,"registered-sphere-case-"+scenario+"-next-turn.txt"));
                sphereCases++;
            }
            finally
            {
                if(adapter!=null)adapter.Dispose();
                if(battle!=0)closeBattle(battle);if(prep!=0)closePreparation(prep);if(catalog!=0)closeCatalog(catalog);
            }
        }
    }
}
