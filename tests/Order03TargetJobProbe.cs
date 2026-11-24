using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

// Synthetic registered battle that selects a real hand card and targets member 2.
class Order03TargetJobProbe {
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr LoadLibrary(string path);
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr module, string name);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Open(byte[] data, int count, ref ulong handle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Prep(int mode, ref ulong handle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int User(ulong prep, int member, int[] data, int count, int seed, byte[] output, int capacity, ref int written);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Register(ulong prep, int member, byte[] data, int count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Card(ulong prep, int member, int slot, byte[] data, int count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Create(ulong catalog, ulong prep, byte[] data, int count, ref ulong handle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Step(ulong battle, int operation, byte[] data, int count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Read(ulong battle, byte[] output, int capacity, ref int written);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Close(ulong handle);

    class Lib {
        public Open open; public Prep prep; public User user; public Register enemy, spheres; public Card card, buddy;
        public Create create; public Step step; public Read read, log; public Close close, prepClose, catalogClose;
        T Bind<T>(IntPtr module, string name) {
            IntPtr p = GetProcAddress(module, name);
            if (p == IntPtr.Zero) throw new Exception("Missing export: " + name);
            return (T)(object)Marshal.GetDelegateForFunctionPointer(p, typeof(T));
        }
        public Lib(string path) {
            IntPtr m = LoadLibrary(Path.GetFullPath(path));
            if (m == IntPtr.Zero) throw new Exception("Cannot load DLL: " + path);
            open = Bind<Open>(m, "battlecore_catalog_open_tables"); prep = Bind<Prep>(m, "battlecore_preparation_open");
            user = Bind<User>(m, "battlecore_preparation_user"); enemy = Bind<Register>(m, "battlecore_preparation_enemy");
            card = Bind<Card>(m, "battlecore_preparation_card"); spheres = Bind<Register>(m, "battlecore_preparation_spheres");
            buddy = Bind<Card>(m, "battlecore_preparation_buddy"); create = Bind<Create>(m, "battlecore_context_create_registered");
            step = Bind<Step>(m, "battlecore_step"); read = Bind<Read>(m, "battlecore_result_read");
            log = Bind<Read>(m, "battlecore_input_log"); close = Bind<Close>(m, "battlecore_context_close");
            prepClose = Bind<Close>(m, "battlecore_preparation_close"); catalogClose = Bind<Close>(m, "battlecore_catalog_close");
        }
    }

    static void Put(byte[] data, int offset, int value) { Buffer.BlockCopy(BitConverter.GetBytes(value), 0, data, offset, 4); }
    static void PutLong(byte[] data, int offset, long value) { Buffer.BlockCopy(BitConverter.GetBytes(value), 0, data, offset, 8); }
    static JObject Family(string name, params object[] pairs) {
        var values = new JArray();
        for (int i = 0; i < pairs.Length; i += 2)
            values.Add(new JObject { { "name", (string)pairs[i] }, { "value", Convert.ToInt32(pairs[i + 1]) } });
        return new JObject { { "name", name }, { "values", values } };
    }
    static byte[] Tables(string fixture) {
        var root = JObject.Parse(File.ReadAllText(fixture));
        foreach (JObject table in (JArray)root["tables"])
            if ((string)table["name"] == "skill_role_param_rule.csv")
                table["text"] = (string)table["text"] + "ATK_UP_FIXED,100,0,VALUE,SKILL_ROLE_BATTLE_PARAM,VALUE,VALUE,VALUE,VALUE,,,,\n";
        root["enums"] = new JArray(
            Family("SKILL_EXTEND_EXEC_ORDER", "NONE", 0, "TARGET_JOB_TYPE", 15),
            Family("RARITY", "NORMAL", 0), Family("ATTR", "FIRE", 0),
            Family("SKILL_TYPE", "SUPPORT", 0), Family("SKILL_KIND", "SUPPORT", 0),
            Family("JOB_TYPE", "NULL", 0, "MERCENARY", 1, "MILLIONAIRE", 2, "THIEF", 3, "SINGER", 4, "ALCHEMIST", 5),
            Family("ARTHUR_TYPE", "NULL", 0), Family("SKILL_PHYSICS_TYPE", "PHYSICS", 0),
            Family("SKILL_TARGET", "USER_ONE", 0), Family("SKILL_EXEC_TIMING", "NORMAL", 0),
            Family("SKILL_ROLE", "NONE", 0, "ATK_UP_FIXED", 1), Family("SKILL_ROLE_TARGET", "SELECT", 0),
            Family("SKILL_ROLE_BATTLE_PARAM", "ATK", 0));
        return Encoding.UTF8.GetBytes(root.ToString(Newtonsoft.Json.Formatting.None));
    }
    static byte[] Skill(long first, long second, int conditionSlot) {
        var wire = new byte[2436];
        Put(wire, 0, 1001); Put(wire, 7 * 4, 1); Put(wire, 11 * 4, 0);
        int normal = 132, boosted = 132 + 460;
        Put(wire, normal + 88, 0); Put(wire, normal + 96, 888);
        Put(wire, boosted + 88, 10); Put(wire, boosted + 96, 889);
        foreach (int variant in new int[] { normal, boosted }) {
            int role = variant + 100;
            Put(wire, role, 1); Put(wire, role + 4, 0);
            Put(wire, role + 16, 2); Put(wire, role + 20, 0);
            Put(wire, role + 24, 100); Put(wire, role + 28, 1000);
            Put(wire, role + 64, 100);
        }
        int condition = boosted + conditionSlot * 44;
        Put(wire, condition, 15); PutLong(wire, condition + 4, first); PutLong(wire, condition + 12, second);
        Put(wire, 2432, 1);
        return wire;
    }
    static void Require(int status, string label) { if (status != 0) throw new Exception(label + " status=" + status); }
    static string Advance(Lib lib, ulong battle, int operation, string json, JArray trace) {
        byte[] input = json == null ? null : Encoding.UTF8.GetBytes(json);
        Require(lib.step(battle, operation, input, input == null ? 0 : input.Length), "step " + operation);
        var output = new byte[1048576]; int written = 0;
        Require(lib.read(battle, output, output.Length, ref written), "read " + operation);
        string text = Encoding.UTF8.GetString(output, 0, written);
        trace.Add(new JObject { { "operation", operation }, { "result", text } });
        return text;
    }
    static JObject Run(Lib lib, byte[] tables, byte[] skill) {
        ulong catalog = 0, prep = 0, battle = 0;
        try {
            Require(lib.open(tables, tables.Length, ref catalog), "catalog");
            Require(lib.prep(1, ref prep), "preparation");
            for (int member = 1; member <= 4; member++) {
                var values = new int[18];
                values[1] = 1000; values[2] = 1000; values[3] = 100; values[4] = 100; values[5] = 100;
                values[6] = member; values[7] = member; values[8] = 10; values[17] = 5;
                var output = new byte[4096]; int written = 0;
                Require(lib.user(prep, member, values, values.Length, 12345, output, output.Length, ref written), "user");
                for (int slot = 1; slot <= 20; slot++) {
                    var card = new byte[4960];
                    if (slot <= 10) {
                        Put(card, 0, slot); Put(card, 8, 1); Put(card, 20, 0);
                        Buffer.BlockCopy(skill, 0, card, 28, skill.Length);
                    }
                    Require(lib.card(prep, member, slot, card, card.Length), "card");
                }
                Require(lib.spheres(prep, member, new byte[21984], 21984), "spheres");
                for (int slot = 1; slot <= 5; slot++) Require(lib.buddy(prep, member, slot, new byte[17096], 17096), "buddy");
            }
            var enemy = new byte[276]; Put(enemy, 0, 905); PutLong(enemy, 8, 10000); PutLong(enemy, 16, 10000);
            Require(lib.enemy(prep, 5, enemy, enemy.Length), "enemy");
            byte[] request = Encoding.UTF8.GetBytes("{\"mode\":1,\"enemy_type\":0,\"config\":{}}");
            int createStatus = lib.create(catalog, prep, request, request.Length, ref battle);
            var result = new JObject { { "createStatus", createStatus }, { "trace", new JArray() } };
            if (createStatus != 0) return result;
            var trace = (JArray)result["trace"];
            Advance(lib, battle, 1, null, trace); Advance(lib, battle, 2, null, trace);
            string dealt = Advance(lib, battle, 3, null, trace); int selected = 0;
            foreach (string line in dealt.Split('\n')) {
                string[] fields = line.TrimEnd('\r').Split(',');
                if (fields.Length >= 4 && fields[0] == "20" && fields[1] == "1") { selected = Int32.Parse(fields[2]); break; }
            }
            if (selected == 0) throw new Exception("member 1 received no card");
            Advance(lib, battle, 4, "{\"member_type\":1,\"card_types\":[" + selected + ",0,0,0,0],\"targets\":[2,0,0,0,0]}", trace);
            for (int member = 2; member <= 4; member++) Advance(lib, battle, 4, "{\"member_type\":" + member + "}", trace);
            string action = Advance(lib, battle, 5, null, trace); int functionID = -1, branchIndex = -1;
            foreach (string line in action.Split('\n')) {
                string[] fields = line.TrimEnd('\r').Split(',');
                if (fields.Length > 10 && fields[0] == "51" && fields[1] == "1" && fields[3] == "1001") {
                    functionID = Int32.Parse(fields[9]); branchIndex = Int32.Parse(fields[10]); break;
                }
            }
            if (functionID < 0) throw new Exception("player card skill command not found: " + action);
            var journal = new byte[1048576]; int count = 0; Require(lib.log(battle, journal, journal.Length, ref count), "journal");
            result["selectedCardType"] = selected; result["functionID"] = functionID; result["branchIndex"] = branchIndex;
            result["journal"] = Encoding.UTF8.GetString(journal, 0, count);
            return result;
        } finally {
            if (battle != 0) Require(lib.close(battle), "battle close");
            if (prep != 0) Require(lib.prepClose(prep), "preparation close");
            if (catalog != 0) Require(lib.catalogClose(catalog), "catalog close");
        }
    }
    static int Main(string[] args) {
        try {
            var original = new Lib(args[0]); var rebuilt = new Lib(args[1]); byte[] tables = Tables(args[2]);
            long[][] selectors = { new long[] { 2, 0, 1 }, new long[] { 0, 2, 1 }, new long[] { 1, 0, 0 },
                new long[] { 1, 2, 1 }, new long[] { 0, 0, 0 }, new long[] { 5, 4, 0 } };
            var cases = new JArray();
            foreach (int slot in new int[] { 0, 1 }) foreach (long[] selector in selectors) {
                byte[] skill = Skill(selector[0], selector[1], slot);
                cases.Add(new JObject { { "conditionSlot", slot }, { "first", selector[0] }, { "second", selector[1] },
                    { "expectedFunctionID", selector[2] == 1 ? 889 : 888 },
                    { "original", Run(original, tables, skill) }, { "rebuilt", Run(rebuilt, tables, skill) } });
            }
            File.WriteAllText(args[3], cases.ToString(Newtonsoft.Json.Formatting.None), new UTF8Encoding(false));
            Console.WriteLine("cases=" + cases.Count); return 0;
        } catch (Exception error) { Console.Error.WriteLine(error.ToString()); return 1; }
    }
}
