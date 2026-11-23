using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kairisei.Recovery
{
    // Resource contract for battlecore_catalog_open_tables. CSV semantics remain
    // owned by the native catalog parser; this checks transport completeness.
    public static class CombatTableSet
    {
        public const string BundleName = "recovery/combat.dat";
        public const string AssetName = "combat-tables";
        public const int MaximumBytes = 64 * 1024 * 1024;
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private static readonly string[] RequiredTables = {
            "card.csv", "buddy.csv", "sphr.csv", "burst_gauge.csv", "burst_gauge_reaction_rate.csv",
            "skill_role_param_rule.csv", "skill_player.csv", "skill_role_player.csv", "burst_skill.csv",
            "burst_skill_role.csv", "support_skill.csv", "support_skill_lvup.csv", "support_skill_role.csv",
            "enemy_party.csv", "enemy.csv", "enemy_lvup.csv", "enemy_ai_order.csv", "skill_enemy.csv",
            "skill_role_enemy.csv", "enemy_trance_gauge_reaction.csv"
        };

        public static void Validate(byte[] data)
        {
            if (data == null || data.Length == 0 || data.Length > MaximumBytes)
                throw new InvalidDataException("Combat table snapshot must contain 1..64 MiB of UTF-8 JSON.");
            JObject snapshot;
            using (StringReader text = new StringReader(Utf8.GetString(data)))
            using (JsonTextReader reader = new JsonTextReader(text) { MaxDepth = 32, DateParseHandling = DateParseHandling.None })
            {
                snapshot = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (reader.Read()) throw new InvalidDataException("Combat table snapshot has trailing input.");
            }
            Fields(snapshot, "tables", "enums");
            JArray tables = snapshot["tables"] as JArray;
            JArray enums = snapshot["enums"] as JArray;
            if (tables == null || tables.Count == 0 || tables.Count > 64 || enums == null || enums.Count == 0)
                throw new InvalidDataException("Registered combat requires tables and the matching native enum schema.");
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            foreach (JToken token in tables)
            {
                JObject table = token as JObject;
                if (table == null) throw new InvalidDataException("Combat table entry must be an object.");
                Fields(table, "name", "text");
                string name = StringField(table, "name"), value = StringField(table, "text");
                if (name.Length > 128 || !Regex.IsMatch(name, @"\A[a-z][a-z0-9_]*\.csv\z") || !names.Add(name))
                    throw new InvalidDataException("Invalid or duplicated combat table name: " + name);
                int limit = name == "card.csv" ? 32 * 1024 * 1024 : 16 * 1024 * 1024;
                if (value.Length == 0 || Utf8.GetByteCount(value) > limit)
                    throw new InvalidDataException("Combat table size is invalid: " + name);
            }
            foreach (string required in RequiredTables)
                if (!names.Contains(required)) throw new InvalidDataException("Required combat table is absent: " + required);
            CompleteGroup(names, new[] { "skill_player_alchemist.csv", "skill_role_player_alchemist.csv", "tower_skill_enemy.csv", "tower_skill_role_enemy.csv" });
            CompleteGroup(names, new[] { "npc_ai.csv", "npc_ai_situation_param_rule.csv" });
            ValidateEnums(enums);
            // CSV rows are validated by the native catalog when
            // NativeRegisteredBattle opens this exact byte snapshot.
        }

        private static void ValidateEnums(JArray families)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            foreach (JToken token in families)
            {
                JObject family = token as JObject;
                if (family == null) throw new InvalidDataException("Native enum family must be an object.");
                Fields(family, "name", "values");
                string name = StringField(family, "name");
                JArray values = family["values"] as JArray;
                if (name.Length == 0 || !names.Add(name) || values == null || values.Count == 0 || values.Count > 1024)
                    throw new InvalidDataException("Native enum family is invalid or duplicated: " + name);
                Dictionary<int, string> numbers = new Dictionary<int, string>();
                HashSet<string> labels = new HashSet<string>(StringComparer.Ordinal);
                foreach (JToken entry in values)
                {
                    JObject value = entry as JObject;
                    if (value == null) throw new InvalidDataException("Native enum value must be an object.");
                    Fields(value, "name", "value");
                    string label = StringField(value, "name");
                    JToken number = value["value"];
                    int id;
                    if (label.Length == 0 || label.StartsWith("sizeof_", StringComparison.Ordinal) || number == null ||
                        number.Type != JTokenType.Integer || !Int32.TryParse(number.ToString(Formatting.None), out id))
                        throw new InvalidDataException("Native enum value is invalid: " + name);
                    if (!labels.Add(label))
                        throw new InvalidDataException("Native enum name is duplicated: " + name + "." + label);
                    string previous;
                    if (numbers.TryGetValue(id, out previous) && previous != label)
                        throw new InvalidDataException("Native enum value is ambiguous: " + name);
                    numbers[id] = label;
                }
            }
        }

        private static string StringField(JObject value, string name)
        {
            JToken field = value[name];
            if (field == null || field.Type != JTokenType.String) throw new InvalidDataException("Combat field must be text: " + name);
            return (string)field;
        }

        private static void Fields(JObject value, params string[] allowed)
        {
            foreach (JProperty property in value.Properties())
                if (Array.IndexOf(allowed, property.Name) < 0) throw new InvalidDataException("Unknown combat field: " + property.Name);
        }

        private static void CompleteGroup(HashSet<string> names, string[] group)
        {
            int count = 0;
            foreach (string name in group) if (names.Contains(name)) count++;
            if (count != 0 && count != group.Length) throw new InvalidDataException("Combat table group is incomplete: " + String.Join(", ", group));
        }
    }
}
