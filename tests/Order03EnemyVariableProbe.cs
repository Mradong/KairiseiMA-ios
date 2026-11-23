using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

// Every table, character and enemy in this probe is synthetic.
class Order03EnemyVariableProbe {
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr LoadLibrary(string path);
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr module, string name);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Open(byte[] data, int count, ref ulong handle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Prep(int mode, ref ulong handle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int User(ulong prep, int member, int[] data, int count, int seed, byte[] output, int capacity, ref int written);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Register(ulong prep, int member, byte[] data, int count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Card(ulong prep, int member, int slot, byte[] data, int count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int EnemySkill(ulong prep, int member, int kind, int slot, byte[] data, int count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Create(ulong catalog, ulong prep, byte[] data, int count, ref ulong handle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Step(ulong battle, int operation, byte[] data, int count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Read(ulong battle, byte[] output, int capacity, ref int written);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Close(ulong handle);
    class Lib {
        public Open open; public Prep prep; public User user; public Register enemy, spheres; public Card card, buddy; public EnemySkill skill;
        public Create create; public Step step; public Read read, log; public Close close, prepClose, catalogClose;
        T Bind<T>(IntPtr module, string name) {
            IntPtr p = GetProcAddress(module, name);
            if (p == IntPtr.Zero) throw new Exception("Missing export: " + name);
            return (T)(object)Marshal.GetDelegateForFunctionPointer(p, typeof(T));
        }
        public Lib(string path) {
            IntPtr m = LoadLibrary(Path.GetFullPath(path));
            if (m == IntPtr.Zero) throw new Exception("Cannot load DLL");
            open = Bind<Open>(m, "battlecore_catalog_open_tables"); prep = Bind<Prep>(m, "battlecore_preparation_open");
            user = Bind<User>(m, "battlecore_preparation_user"); enemy = Bind<Register>(m, "battlecore_preparation_enemy");
            card = Bind<Card>(m, "battlecore_preparation_card");
            spheres = Bind<Register>(m, "battlecore_preparation_spheres"); buddy = Bind<Card>(m, "battlecore_preparation_buddy");
            skill = Bind<EnemySkill>(m, "battlecore_preparation_enemy_skill"); create = Bind<Create>(m, "battlecore_context_create_registered");
            step = Bind<Step>(m, "battlecore_step"); read = Bind<Read>(m, "battlecore_result_read"); log = Bind<Read>(m, "battlecore_input_log");
            close = Bind<Close>(m, "battlecore_context_close"); prepClose = Bind<Close>(m, "battlecore_preparation_close");
            catalogClose = Bind<Close>(m, "battlecore_catalog_close");
        }
    }
    static void Put(byte[] b, int offset, int value) { Buffer.BlockCopy(BitConverter.GetBytes(value), 0, b, offset, 4); }
    static void Long(byte[] b, int offset, long value) { Buffer.BlockCopy(BitConverter.GetBytes(value), 0, b, offset, 8); }
    static JObject Family(string name, params string[] labels) {
        var values = new JArray();
        for (int i = 0; i < labels.Length; i++) values.Add(new JObject { { "name", labels[i] }, { "value", i } });
        return new JObject { { "name", name }, { "values", values } };
    }
    static byte[] Tables(string fixture, int ordinal) {
        var t = JObject.Parse(File.ReadAllText(fixture));
        foreach (JObject table in (JArray)t["tables"])
            if ((string)table["name"] == "skill_role_param_rule.csv") table["text"] = (string)table["text"] + "ENEMY_AI_VAR_ADD,100,0,VALUE,VALUE,,,,,,,,\n";
        var condition = Family("SKILL_EXTEND_EXEC_ORDER", "NONE");
        if (ordinal >= 0) ((JArray)condition["values"]).Add(new JObject { { "name", "SELF_ENEMY_AI_VAR" }, { "value", ordinal } });
        t["enums"] = new JArray(condition, Family("RARITY", "NORMAL"), Family("ATTR", "FIRE"), Family("SKILL_TYPE", "ATTACK"),
            Family("SKILL_KIND", "ATTACK"), Family("JOB_TYPE", "NONE"), Family("ARTHUR_TYPE", "NULL"),
            Family("SKILL_PHYSICS_TYPE", "PHYSICS"), Family("SKILL_TARGET", "ENEMY_ONE"), Family("SKILL_EXEC_TIMING", "NORMAL"),
            Family("SKILL_ROLE", "NONE", "ATTACK_AA", "ENEMY_AI_VAR_ADD"), Family("SKILL_ROLE_TARGET", "SELECT"),
            Family("SKILL_ROLE_BATTLE_PARAM", "ATK"), Family("ENEMY_AI_TARGET", "NULL", "RANDOM", "ENEMY1"),
            Family("ENEMY_AI_ORDER_PARTY", "NULL"), Family("ENEMY_AI_ORDER_TRANCE_GAUGE", "NULL"), Family("ENEMY_AI_ORDER_TRIGGER", "NULL"));
        return Encoding.UTF8.GetBytes(t.ToString(Newtonsoft.Json.Formatting.None));
    }
    static byte[] Skill(int ordinal, int slot, int variableIndex, int delta, long lower, long upper) {
        var b = new byte[2656]; Put(b, 0, 2001); Put(b, 2432, 1);
        for (int variant = 0; variant < 2; variant++) {
            int at = 132 + variant * 460;
            Put(b, at + 88, variant + 1); Put(b, at + 96, 888 + variant);
            int add = at + 100; Put(b, add, 2); Put(b, add + 12, 2); Put(b, add + 16, variableIndex); Put(b, add + 20, delta);
            int hit = add + 72; Put(b, hit, 1); Put(b, hit + 12, 2); Put(b, hit + 16, variant == 0 ? 7 : 13); Put(b, hit + 32, 1); Put(b, hit + 56, 1);
        }
        int condition = 132 + 460 + slot * 44;
        if (ordinal >= 0) { Put(b, condition, ordinal); Long(b, condition + 4, variableIndex); Long(b, condition + 12, lower); Long(b, condition + 20, upper); }
        Put(b, 2436, 1); Put(b, 2440, 2); Put(b, 2484, 1); Put(b, 2488, 10); Put(b, 2492, 100);
        for (int turn = 0; turn < 11; turn++) b[2540 + turn * 2] = 1;
        return b;
    }
    static void Require(int status, string label) { if (status != 0) throw new Exception(label + " status=" + status); }
    static string Advance(Lib lib, ulong battle, int op, string json, JArray trace) {
        byte[] input = json == null ? null : Encoding.UTF8.GetBytes(json);
        Require(lib.step(battle, op, input, input == null ? 0 : input.Length), "step " + op);
        byte[] output = new byte[1048576]; int written = 0;
        Require(lib.read(battle, output, output.Length, ref written), "read");
        string result = Encoding.UTF8.GetString(output, 0, written);
        trace.Add(new JObject { { "op", op }, { "result", result } });
        return result;
    }
    static JObject Run(Lib lib, byte[] tables, byte[] skill) {
        ulong catalog = 0, prep = 0, battle = 0;
        try {
            Require(lib.open(tables, tables.Length, ref catalog), "catalog"); Require(lib.prep(1, ref prep), "preparation");
            for (int m = 1; m <= 4; m++) {
                var u = new int[18]; u[1] = 1000; u[2] = 1000; u[7] = m; u[8] = 10; u[17] = 5;
                var output = new byte[4096]; int written = 0;
                Require(lib.user(prep, m, u, u.Length, 12345, output, output.Length, ref written), "user");
                for (int slot = 1; slot <= 20; slot++) {
                    var card = new byte[4960]; if (slot <= 10) { Put(card, 0, slot); Put(card, 8, 1); Put(card, 28, 1001);
                    Put(card, 28 + 228, 777); Put(card, 28 + 232, 1); Put(card, 28 + 244, 2);
                    Put(card, 28 + 248, 10); Put(card, 28 + 264, 1); Put(card, 28 + 2432, 1); }
                    Require(lib.card(prep, m, slot, card, card.Length), "card");
                }
                byte[] spheres = new byte[21984]; Require(lib.spheres(prep, m, spheres, spheres.Length), "spheres");
                for (int slot = 1; slot <= 5; slot++) { byte[] buddy = new byte[17096]; Require(lib.buddy(prep, m, slot, buddy, buddy.Length), "buddy"); }
            }
            var enemy = new byte[276]; Put(enemy, 0, 905); Long(enemy, 8, 10000); Long(enemy, 16, 10000); Put(enemy, 116, 1);
            Require(lib.enemy(prep, 5, enemy, enemy.Length), "enemy"); Require(lib.skill(prep, 5, 0, 0, skill, skill.Length), "skill");
            byte[] request = Encoding.UTF8.GetBytes("{\"mode\":1,\"enemy_type\":0,\"config\":{}}");
            int status = lib.create(catalog, prep, request, request.Length, ref battle);
            var report = new JObject { { "createStatus", status }, { "turns", new JArray() }, { "trace", new JArray() } };
            if (status != 0) return report;
            var trace = (JArray)report["trace"]; Advance(lib, battle, 1, null, trace);
            for (int turn = 0; turn < 3; turn++) {
                Advance(lib, battle, 2, null, trace); Advance(lib, battle, 3, null, trace);
                for (int m = 1; m <= 4; m++) Advance(lib, battle, 4, "{\"member_type\":" + m + "}", trace);
                Advance(lib, battle, 5, null, trace); Advance(lib, battle, 8, null, trace);
                string action = Advance(lib, battle, 6, null, trace);
                var commands = new JArray();
                foreach (string line in action.Split('\n')) if (line.StartsWith("94,") || line.StartsWith("60,")) commands.Add(line.TrimEnd('\r'));
                ((JArray)report["turns"]).Add(commands); Advance(lib, battle, 9, null, trace);
            }
            byte[] log = new byte[1048576]; int count = 0; Require(lib.log(battle, log, log.Length, ref count), "journal");
            report["journal"] = Encoding.UTF8.GetString(log, 0, count);
            return report;
        } finally {
            if (battle != 0) Require(lib.close(battle), "battle close");
            if (prep != 0) Require(lib.prepClose(prep), "preparation close");
            if (catalog != 0) Require(lib.catalogClose(catalog), "catalog close");
        }
    }
    static int Main(string[] args) {
        try {
            var original = new Lib(args[0]); var rebuilt = new Lib(args[1]); var results = new JArray();
            // index, increment, inclusive lower, inclusive upper. All bounds are explicit packed values.
            long[][] cases = {
                new long[] { 0, 1, 0, 0 }, new long[] { 0, 1, 1, 1 }, new long[] { 0, 1, 2, 1 },
                new long[] { 4, -2, -4, -2 }, new long[] { -1, 1, -1, -1 }, new long[] { 5, 1, -1, -1 },
                new long[] { 0, 2147483647, -2, -2 }, new long[] { 0, 1, -2147483648L, 2147483647L },
                new long[] { 0, 1, 2147483648L, 2147483649L }, new long[] { 0, 1, -2147483649L, 2147483648L },
                new long[] { 0, 1, Int64.MinValue, Int64.MaxValue }, new long[] { 0, -2, 0, 0 }
            };
            foreach (int ordinal in new int[] { -1, 110, 111, 900 }) foreach (int slot in new int[] { 0, 1 }) foreach (long[] c in cases) {
                byte[] tables = Tables(args[2], ordinal), skill = Skill(ordinal, slot, (int)c[0], (int)c[1], c[2], c[3]);
                results.Add(new JObject { { "ordinal", ordinal }, { "slot", slot }, { "index", c[0] }, { "delta", c[1] },
                    { "lower", c[2].ToString(System.Globalization.CultureInfo.InvariantCulture) }, { "upper", c[3].ToString(System.Globalization.CultureInfo.InvariantCulture) }, { "original", Run(original, tables, skill) }, { "rebuilt", Run(rebuilt, tables, skill) } });
            }
            File.WriteAllText(args[3], results.ToString(Newtonsoft.Json.Formatting.None), new UTF8Encoding(false));
            Console.WriteLine("cases=" + results.Count); return 0;
        } catch (Exception e) { Console.Error.WriteLine(e.ToString()); return 1; }
    }
}
