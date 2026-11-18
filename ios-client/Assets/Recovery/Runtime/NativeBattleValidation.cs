using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kairisei.Recovery
{
    // Synthetic offline validation. Does not load a real deck, account or server.
    public static class NativeBattleValidation
    {
#if UNITY_IOS && !UNITY_EDITOR
        private const string Library = "__Internal";
#else
        private const string Library = "kairisei_battlecore_rebuilt";
#endif
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_catalog_open_tables(byte[] input, int count, out ulong handle);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_context_create(ulong catalog, byte[] input, int count, out ulong handle);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_step(ulong battle, int operation, byte[] input, int count);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_result_size(ulong battle, out int size);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_result_read(ulong battle, [Out] byte[] output, int count, out int written);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_score_damage(ulong battle, out long damage);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_context_close(ulong battle);
        [DllImport(Library, CallingConvention=CallingConvention.Cdecl)] private static extern int battlecore_catalog_close(ulong catalog);

        private static void Check(int status, string stage)
        {
            if (status != 0) throw new InvalidOperationException(stage + "失败，状态码 " + status);
        }
        private static byte[] Asset(string name)
        {
            TextAsset asset=Resources.Load<TextAsset>(name);
            if(asset==null)throw new InvalidOperationException("缺少合成验证数据："+name);
            return asset.bytes;
        }
        public static string Run()
        {
            ulong catalog=0, battle=0;
            try
            {
                byte[] tables=Asset("NativeValidationTables"), request=Asset("NativeValidationBattle");
                JObject trace=JObject.Parse(Encoding.UTF8.GetString(Asset("NativeValidationTrace")));
                JArray steps=(JArray)trace["steps"];
                Check(battlecore_catalog_open_tables(tables,tables.Length,out catalog),"加载战斗表");
                Check(battlecore_context_create(catalog,request,request.Length,out battle),"创建战斗");
                int verified=0;
                bool victory=false;
                foreach(JObject step in steps)
                {
                    JToken encoded=step["inputBase64"];
                    byte[] input=encoded.Type==JTokenType.Null?null:Convert.FromBase64String((string)encoded);
                    int status=battlecore_step(battle,(int)step["operation"],input,input==null?0:input.Length);
                    if(status!=(int)step["status"])throw new InvalidOperationException("战斗推进与参考结果不同，步骤 "+(verified+1)+"，状态码 "+status);
                    int size, written;
                    Check(battlecore_result_size(battle,out size),"读取结果长度");
                    if(size<0||size>1048576)throw new InvalidOperationException("结果长度超限");
                    byte[] result=new byte[size];
                    Check(battlecore_result_read(battle,result,result.Length,out written),"读取战斗结果");
                    if(written!=size)throw new InvalidOperationException("结果长度不一致");
                    string hash;
                    using(SHA256 sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(result)).Replace("-","").ToLowerInvariant();
                    if(hash!=(string)step["resultSHA256"])throw new InvalidOperationException("战斗结果校验不一致，步骤 "+(verified+1));
                    if(("\n"+Encoding.UTF8.GetString(result)+"\n").Contains("\n2,1\n"))victory=true;
                    verified++;
                }
                long damage;
                Check(battlecore_score_damage(battle,out damage),"读取伤害");
                if(!victory||damage<=0)throw new InvalidOperationException("合成战斗未完成胜利结算");
                return "离线战斗内核验证通过："+verified+" 步结果与 Windows 原版参考一致，已到胜利结算。此项使用合成数据，完整游戏界面与资源仍在恢复中。";
            }
            finally
            {
                if(battle!=0)battlecore_context_close(battle);
                if(catalog!=0)battlecore_catalog_close(catalog);
            }
        }
    }
}
