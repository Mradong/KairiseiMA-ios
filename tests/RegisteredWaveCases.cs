using System;
using System.IO;
using System.Text;

partial class RegisteredBattleDifferential
{
    private static int completedWaveCases;

    private static void RegisterWaveCards(Lib a, Lib b, ulong ap, ulong bp, int roleSlot)
    {
        for (int member=1; member<=4; member++)
        for (int slot=1; slot<=10; slot++)
        {
            var card=new byte[4960];
            Put(card,0,slot); Put(card,8,1); Put(card,28,1001);
            Put(card,28+228,777); Put(card,28+2432,1);
            // Native skill consumers stop at the first NONE role; use a
            // contiguous sequence to observe later role indices in results.
            for(int index=0;index<=roleSlot;index++)
            {
                int role=28+232+72*index;
                Put(card,role,1); Put(card,role+12,2); // ATTACK_AA, FIRE mask.
                Put(card,role+16,7); Put(card,role+32,1); // Fixed damage 7.
            }
            Equal(a.card(ap,member,slot,card,card.Length),b.card(bp,member,slot,card,card.Length),"wave card",0);
        }
    }

    private static void RegisterWaveEnemy(Lib a, Lib b, ulong ap, ulong bp, int id, int hp)
    {
        var foe=new byte[276]; Put(foe,0,id); Put(foe,4,3);
        Buffer.BlockCopy(BitConverter.GetBytes((long)hp),0,foe,8,8);
        Buffer.BlockCopy(BitConverter.GetBytes((long)hp*2),0,foe,16,8);
        Equal(a.enemy(ap,5,foe,foe.Length),b.enemy(bp,5,foe,foe.Length),"wave enemy",0);
    }

    private static string PlayWaveHand(Lib a, Lib b, ulong ah, ulong bh, string dealt, string dir, int roleSlot)
    {
        var hand=new int[4];
        foreach(string line in dealt.Split('\n'))
        {
            string[] row=line.Split(',');
            if(row.Length>=4 && row[0]=="20")
            {
                int member=Int32.Parse(row[1]);
                if(member>=1 && member<=4 && hand[member-1]==0) hand[member-1]=Int32.Parse(row[2]);
            }
        }
        for(int member=1;member<=4;member++)
        {
            if(hand[member-1]==0)throw new Exception("wave did not deal a playable card");
            Advance(a,b,ah,bh,4,"{\"member_type\":"+member+",\"card_types\":["+hand[member-1]+",0,0,0,0],\"targets\":[5,0,0,0,0]}",dir);
        }
        string attack=Advance(a,b,ah,bh,5,null,dir);
        bool damaged=false, lastRoleObserved=false;
        foreach(string line in attack.Split('\n'))
        {
            string[] row=line.Split(',');
            if(row.Length>4 && row[0]=="60")
            {
                damaged=true;
                int index=Int32.Parse(row[2]);
                if(index<0 || index>roleSlot || Int32.Parse(row[3])!=-7)throw new Exception("packed role slot/damage was not preserved");
                if(index==roleSlot) lastRoleObserved=true;
            }
        }
        if(!damaged || !lastRoleObserved || !("\n"+attack+"\n").Contains("\n2,1\n")){File.WriteAllText(Path.Combine(dir,"wave-matrix-attack.txt"),attack);throw new Exception("registered wave must deal actual damage and win: role="+roleSlot+" case="+completedWaveCases);}
        return attack;
    }

    private static string ReadNextWave(Lib a, Lib b, ulong ah, ulong bh, string dir)
    {
        var x=new byte[1048576];var y=new byte[x.Length];int n=0,m=0;
        Equal(a.read(ah,null,0,ref n),b.read(bh,null,0,ref m),"next null output",1);
        if(n!=m || n<=0)throw new Exception("next output size differs");
        int sx=0,sy=0;Equal(a.read(ah,x,n-1,ref sx),b.read(bh,y,n-1,ref sy),"next short output",1);
        if(sx!=n || sy!=m)throw new Exception("next short output size differs");
        Equal(a.read(ah,x,x.Length,ref n),b.read(bh,y,y.Length,ref m),"next full output",0);
        string left=Encoding.UTF8.GetString(x,0,n),right=Encoding.UTF8.GetString(y,0,m);
        if(left!=right){File.WriteAllText(Path.Combine(dir,"wave-next-original.txt"),left);File.WriteAllText(Path.Combine(dir,"wave-next-rebuilt.txt"),right);throw new Exception("wave next result differs");}
        return left;
    }

    private static void RunWaveCases(Lib a, Lib b, ulong ac, ulong bc, ulong ap, ulong bp, string dir)
    {
        foreach(int roleSlot in new[]{0,2,4})
        foreach(int enemyType in new[]{0,1,2,3})
        {
            RegisterWaveCards(a,b,ap,bp,roleSlot);
            RegisterWaveEnemy(a,b,ap,bp,910,1);
            var request=Encoding.UTF8.GetBytes("{\"mode\":1,\"enemy_type\":0,\"config\":{}}");
            ulong ah=0,bh=0;Equal(a.create(ac,ap,request,request.Length,ref ah),b.create(bc,bp,request,request.Length,ref bh),"matrix create",0);
            Advance(a,b,ah,bh,1,null,dir);Advance(a,b,ah,bh,2,null,dir);
            PlayWaveHand(a,b,ah,bh,Advance(a,b,ah,bh,3,null,dir),dir,roleSlot);
            RegisterWaveEnemy(a,b,ap,bp,911,21);
            Equal(a.next(ah,ap,-1),b.next(bh,bp,-1),"matrix invalid enemy type",2);
            Equal(a.next(ah,ap,4),b.next(bh,bp,4),"matrix awaken type without awaken end",2);
            Equal(a.next(ah,999999,enemyType),b.next(bh,999999,enemyType),"matrix missing preparation",4);
            CompareLog(a,b,ah,bh,300+completedWaveCases,dir);
            Equal(a.next(ah,ap,enemyType),b.next(bh,bp,enemyType),"matrix next",0);
            CompareLog(a,b,ah,bh,400+completedWaveCases,dir);
            Equal(a.next(ah,ap,enemyType),b.next(bh,bp,enemyType),"matrix unread next",5);
            string started=ReadNextWave(a,b,ah,bh,dir);
            if(!("\n"+started+"\n").Contains("\n6,5,21,42,"))throw new Exception("next wave must use new enemy HP");
            Advance(a,b,ah,bh,2,null,dir);
            PlayWaveHand(a,b,ah,bh,Advance(a,b,ah,bh,3,null,dir),dir,roleSlot);
            completedWaveCases++;
        }
    }
}
