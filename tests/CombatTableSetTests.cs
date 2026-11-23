using System;
using System.IO;
using System.Text;
using Kairisei.Recovery;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class CombatTableSetTests
{
    private static int checks;
    private static byte[] Bytes(JObject value) { return Encoding.UTF8.GetBytes(value.ToString(Formatting.None)); }
    private static void Reject(Action action)
    {
        bool rejected = false;
        try { action(); }
        catch (InvalidDataException) { rejected = true; }
        catch (JsonException) { rejected = true; }
        catch (DecoderFallbackException) { rejected = true; }
        if (!rejected) throw new Exception("Invalid combat resource was accepted.");
        checks++;
    }
    public static void Main(string[] args)
    {
        try { Run(args); }
        catch (Exception error)
        {
            Console.Error.WriteLine("After " + checks + " checks: " + error.GetType().Name + ": " + error.Message);
            Environment.ExitCode = 1;
        }
    }
    private static void Run(string[] args)
    {
        byte[] input = File.ReadAllBytes(args[0]);
        JObject valid = JObject.Parse(Encoding.UTF8.GetString(input));
        // The base fixture has no schema; supply an explicitly synthetic enum
        // for these transport tests. Native differential tests supply all enums.
        valid["enums"] = new JArray(new JObject { { "name", "RARITY" }, { "values", new JArray(new JObject { { "name", "NORMAL" }, { "value", 0 } }) } });
        input = Bytes(valid);
        CombatTableSet.Validate(input); checks++;
        // Every base master is required, including tables without gameplay rows.
        foreach (JToken table in (JArray)valid["tables"])
        {
            JObject incomplete = (JObject)valid.DeepClone();
            JArray tables = (JArray)incomplete["tables"];
            int index = ((JArray)valid["tables"]).IndexOf(table);
            tables.RemoveAt(index);
            Reject(delegate { CombatTableSet.Validate(Bytes(incomplete)); });
        }
        JObject copy = (JObject)valid.DeepClone();
        ((JArray)copy["tables"]).Add(copy["tables"][0].DeepClone());
        Reject(delegate { CombatTableSet.Validate(Bytes(copy)); });
        copy = (JObject)valid.DeepClone(); copy["tables"][0]["name"] = "../card.csv";
        Reject(delegate { CombatTableSet.Validate(Bytes(copy)); });
        copy = (JObject)valid.DeepClone(); copy["tables"][0]["text"] = "";
        Reject(delegate { CombatTableSet.Validate(Bytes(copy)); });
        copy = (JObject)valid.DeepClone(); copy["tables"][0]["text"] = 123;
        Reject(delegate { CombatTableSet.Validate(Bytes(copy)); });
        copy = (JObject)valid.DeepClone(); copy["tables"][0]["unreviewed"] = 1;
        Reject(delegate { CombatTableSet.Validate(Bytes(copy)); });
        copy = (JObject)valid.DeepClone(); copy["enums"] = new JArray();
        Reject(delegate { CombatTableSet.Validate(Bytes(copy)); });
        copy = (JObject)valid.DeepClone(); copy.Remove("enums");
        Reject(delegate { CombatTableSet.Validate(Bytes(copy)); });
        copy = (JObject)valid.DeepClone(); copy["enums"][0]["values"][0]["value"] = 2147483648L;
        Reject(delegate { CombatTableSet.Validate(Bytes(copy)); });
        copy = (JObject)valid.DeepClone(); copy["enums"][0]["values"][0]["value"] = 0.5;
        Reject(delegate { CombatTableSet.Validate(Bytes(copy)); });
        copy = (JObject)valid.DeepClone(); ((JArray)copy["enums"][0]["values"]).Add(new JObject { { "name", "AMBIGUOUS" }, { "value", 0 } });
        Reject(delegate { CombatTableSet.Validate(Bytes(copy)); });
        copy = (JObject)valid.DeepClone(); ((JArray)copy["enums"][0]["values"]).Add(new JObject { { "name", "NORMAL" }, { "value", 1 } });
        Reject(delegate { CombatTableSet.Validate(Bytes(copy)); });
        copy = (JObject)valid.DeepClone(); ((JArray)copy["enums"][0]["values"]).Add(copy["enums"][0]["values"][0].DeepClone());
        Reject(delegate { CombatTableSet.Validate(Bytes(copy)); });
        copy = (JObject)valid.DeepClone(); ((JArray)copy["enums"]).Add(copy["enums"][0].DeepClone());
        Reject(delegate { CombatTableSet.Validate(Bytes(copy)); });
        copy = (JObject)valid.DeepClone(); copy["unexpected"] = true;
        Reject(delegate { CombatTableSet.Validate(Bytes(copy)); });
        copy = (JObject)valid.DeepClone(); ((JArray)copy["tables"]).Add(new JObject { { "name", "skill_player_alchemist.csv" }, { "text", "data" } });
        Reject(delegate { CombatTableSet.Validate(Bytes(copy)); });
        copy = (JObject)valid.DeepClone(); ((JArray)copy["tables"]).Add(new JObject { { "name", "npc_ai.csv" }, { "text", "data" } });
        Reject(delegate { CombatTableSet.Validate(Bytes(copy)); });
        // Bounds are UTF-8 bytes, not UTF-16 character counts.
        copy = (JObject)valid.DeepClone(); copy["tables"][1]["text"] = new string('\u4e00', 5600000);
        Reject(delegate { CombatTableSet.Validate(Bytes(copy)); });
        Reject(delegate { CombatTableSet.Validate(new byte[] { 0xff }); });
        Reject(delegate { CombatTableSet.Validate(new byte[CombatTableSet.MaximumBytes + 1]); });
        Reject(delegate { CombatTableSet.Validate(null); });
        Reject(delegate { CombatTableSet.Validate(Encoding.UTF8.GetBytes("{\"tables\":[],\"tables\":[],\"enums\":[]}")); });
        Reject(delegate { CombatTableSet.Validate(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(input) + "{}")); });
        Console.WriteLine("{\"checks\":" + checks + "}");
    }
}
