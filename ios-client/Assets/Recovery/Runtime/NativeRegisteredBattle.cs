using System;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kairisei.Recovery
{
    // Owns the ABI handles used by the restored client. Callers supply actual
    // catalog/registered records; this adapter contains no gameplay fixtures.
    public sealed class NativeRegisteredBattle : IDisposable
    {
#if UNITY_IOS && !UNITY_EDITOR
        private const string Library = "__Internal";
#else
        private const string Library = "kairisei_battlecore_rebuilt";
#endif
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_catalog_open_tables(byte[] input,int count,out ulong handle);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_catalog_close(ulong handle);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_preparation_open(int mode,out ulong handle);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_preparation_close(ulong handle);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_preparation_configure(ulong prep,int operation,int member,int value);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_preparation_user(ulong prep,int member,int[] values,int count,int seed,[Out] byte[] output,int capacity,out int written);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_preparation_card(ulong prep,int member,int slot,byte[] data,int count);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_preparation_extra_skill(ulong prep,int member,int slot,int kind,byte[] data,int count);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_preparation_buddy(ulong prep,int member,int slot,byte[] data,int count);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_preparation_spheres(ulong prep,int member,byte[] data,int count);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_preparation_enemy(ulong prep,int member,byte[] data,int count);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_preparation_enemy_skill(ulong prep,int member,int kind,int slot,byte[] data,int count);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_context_create_registered(ulong catalog,ulong prep,byte[] input,int count,out ulong battle);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_context_next_registered(ulong battle,ulong prep,int enemyType);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_context_close(ulong battle);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_step(ulong battle,int operation,byte[] input,int count);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_result_size(ulong battle,out int count);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_result_read(ulong battle,[Out] byte[] output,int count,out int written);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_input_log(ulong battle,[Out] byte[] output,int count,out int written);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_last_error(ulong battle,[Out] byte[] output,int count,out int written);
        private const int MaximumOutput=1048576;
        private static readonly Encoding Utf8=new UTF8Encoding(false,true);
        private ulong catalog, preparation, battle;
        private bool disposed;

        public NativeRegisteredBattle(byte[] tables)
        {
            CombatTableSet.Validate(tables);
            try
            {
                Check(battlecore_catalog_open_tables(tables,tables.Length,out catalog),"catalog");
                Check(battlecore_preparation_open(1,out preparation),"preparation");
            }
            catch { Dispose(); throw; }
        }

        public string RegisterUser(int member,int[] values,int seed)
        {
            EnsureOpen();
            if(member<1 || member>4 || values==null || values.Length!=18)throw new ArgumentException("Expected player 1..4 and eighteen packed user fields.");
            byte[] output=new byte[MaximumOutput];int written;
            Check(battlecore_preparation_user(preparation,member,values,values.Length,seed,output,output.Length,out written),"register user");
            return Text(output,written);
        }

        public string RegisterSoloPartner(int member,CnSoloBattleStart start,int partnerIndex)
        {
            if(start==null)throw new ArgumentNullException("start");
            if(start.Seed<Int32.MinValue || start.Seed>Int32.MaxValue)throw new FormatException("Battle seed is outside native range.");
            return RegisterUser(member,CnProtocol.ProjectSoloPartnerUser(start,partnerIndex),(int)start.Seed);
        }

        public void SetContinueAllowed(bool allowed)
        {
            EnsureOpen();
            Check(battlecore_preparation_configure(preparation,1,0,allowed?1:0),"set continue");
        }

        public void SetEndTurn(int turn)
        {
            EnsureOpen();
            if(turn<0)throw new ArgumentOutOfRangeException("turn");
            Check(battlecore_preparation_configure(preparation,2,0,turn),"set end turn");
        }

        public void ApplySoloStartTurnLimit(CnSoloBattleStart start)
        {
            if(start==null)throw new ArgumentNullException("start");
            SetEndTurn(start.EndTurn);
        }

        public void SetInitialBurst(int member,int gauge)
        {
            Player(member);
            if(gauge<0)throw new ArgumentOutOfRangeException("gauge");
            Check(battlecore_preparation_configure(preparation,3,member,gauge),"set initial burst");
        }

        public void RegisterCard(int member,int slot,byte[] packed)
        {
            Player(member);Record(packed,4960);
            if(slot<1 || slot>20)throw new ArgumentOutOfRangeException("slot");
            Check(battlecore_preparation_card(preparation,member,slot,packed,packed.Length),"register card");
        }

        public void RegisterExtraSkill(int member,int slot,int kind,byte[] packed)
        {
            Player(member);Record(packed,2436);
            if(slot<1 || slot>20)throw new ArgumentOutOfRangeException("slot");
            if(kind<0 || kind>3)throw new ArgumentOutOfRangeException("kind");
            Check(battlecore_preparation_extra_skill(preparation,member,slot,kind,packed,packed.Length),"register extra skill");
        }

        public void RegisterSoloPartnerCard(int member,int slot,CnSoloBattleStart start,int partnerIndex,int cardIndex,
            int loveRate,int rarity,int rarityPlus,byte[] normalSkill,byte[] bonusSkill,int[] profile)
        {
            RegisterCard(member,slot,CnProtocol.ProjectSoloPartnerCard(start,partnerIndex,cardIndex,loveRate,rarity,rarityPlus,normalSkill,bonusSkill,profile));
        }

        public void RegisterBuddy(int member,int slot,byte[] packed)
        {
            Player(member);Record(packed,17096);
            if(slot<1 || slot>5)throw new ArgumentOutOfRangeException("slot");
            Check(battlecore_preparation_buddy(preparation,member,slot,packed,packed.Length),"register buddy");
        }

        public void RegisterSpheres(int member,byte[] packed)
        {
            Player(member);Record(packed,21984);
            Check(battlecore_preparation_spheres(preparation,member,packed,packed.Length),"register spheres");
        }

        public void RegisterEnemy(int member,byte[] packed)
        {
            EnsureOpen();Record(packed,276);
            if(member<5 || member>8)throw new ArgumentOutOfRangeException("member");
            Check(battlecore_preparation_enemy(preparation,member,packed,packed.Length),"register enemy");
        }

        public void RegisterEnemySkill(int member,int kind,int slot,byte[] packed)
        {
            EnsureOpen();
            if(member<5 || member>8)throw new ArgumentOutOfRangeException("member");
            int limit = kind==0 ? 21 : kind==1 ? 10 : kind==2 ? 1 : kind==3 ? 3 : 0;
            if(slot<0 || slot>=limit)throw new ArgumentOutOfRangeException("slot");
            Record(packed,(kind==0 || kind==3) ? 2656 : 2436);
            Check(battlecore_preparation_enemy_skill(preparation,member,kind,slot,packed,packed.Length),"register enemy skill");
        }

        public string Start(int enemyType,int enemyPartyId)
        {
            EnsureOpen();
            if(battle!=0)throw new InvalidOperationException("A registered battle is already active.");
            if(enemyType<0 || enemyType>4 || enemyPartyId<0)throw new ArgumentOutOfRangeException("enemyType");
            JObject config=new JObject();if(enemyPartyId!=0)config["enemy_party_id"]=enemyPartyId;
            byte[] request=Utf8.GetBytes(new JObject{{"mode",1},{"enemy_type",enemyType},{"config",config}}.ToString(Formatting.None));
            Check(battlecore_context_create_registered(catalog,preparation,request,request.Length,out battle),"create registered battle");
            return Advance(1,null);
        }

        public string NextWave(int enemyType)
        {
            EnsureBattle();
            Check(battlecore_context_next_registered(battle,preparation,enemyType),"next registered wave");
            return ReadResult();
        }

        public string Advance(int operation,string json)
        {
            EnsureBattle();
            byte[] input=json==null?null:Utf8.GetBytes(json);
            if(input!=null && input.Length>65536)throw new ArgumentException("Battle request exceeds ABI bounds.","json");
            Check(battlecore_step(battle,operation,input,input==null?0:input.Length),"battle step "+operation);
            return ReadResult();
        }

        public string InputJournal()
        {
            EnsureBattle();int needed;
            int status=battlecore_input_log(battle,null,0,out needed);
            if(status!=0 && status!=1)Check(status,"input journal size");
            if(needed<0 || needed>MaximumOutput)throw new InvalidOperationException("Input journal exceeds ABI bounds.");
            byte[] output=new byte[needed];int written;
            Check(battlecore_input_log(battle,output,output.Length,out written),"input journal");
            return Text(output,written);
        }

        private string ReadResult()
        {
            int count,written;Check(battlecore_result_size(battle,out count),"result size");
            if(count<0 || count>MaximumOutput)throw new InvalidOperationException("Battle result exceeds ABI bounds.");
            byte[] output=new byte[count];Check(battlecore_result_read(battle,output,output.Length,out written),"result read");
            if(written!=count)throw new InvalidOperationException("Battle result size changed while reading.");
            return Text(output,written);
        }

        private void Check(int status,string stage)
        {
            if(status==0)return;
            string detail="";
            if(battle!=0)
            {
                byte[] buffer=new byte[MaximumOutput];int written;
                if(battlecore_last_error(battle,buffer,buffer.Length,out written)==0 && written>0) detail=": "+Text(buffer,written);
            }
            throw new InvalidOperationException(stage+" failed (native status "+status+")"+detail);
        }
        private static string Text(byte[] data,int count) { if(count<0 || count>data.Length)throw new InvalidOperationException("Invalid native output length.");return Utf8.GetString(data,0,count); }
        private void Player(int member) { EnsureOpen();if(member<1 || member>4)throw new ArgumentOutOfRangeException("member"); }
        private static void Record(byte[] data,int bytes) { if(data==null || data.Length!=bytes)throw new ArgumentException("Unexpected packed registration record length.","data"); }
        private void EnsureOpen() { if(disposed)throw new ObjectDisposedException("NativeRegisteredBattle"); }
        private void EnsureBattle() { EnsureOpen();if(battle==0)throw new InvalidOperationException("A registered battle has not been created."); }

        public void Dispose()
        {
            if(disposed)return;
            disposed=true;
            try { if(battle!=0)battlecore_context_close(battle); }
            finally
            {
                battle=0;
                try { if(preparation!=0)battlecore_preparation_close(preparation); }
                finally { preparation=0;if(catalog!=0)battlecore_catalog_close(catalog);catalog=0; }
            }
        }
    }
}
