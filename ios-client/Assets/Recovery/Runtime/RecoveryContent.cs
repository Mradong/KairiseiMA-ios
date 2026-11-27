using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Kairisei.Recovery
{
    [Serializable]
    public sealed class ContentBundle
    {
        public string name;
        public string file;
        public long bytes;
        public string sha256;
        public string crc32;
        public string[] dependencies;
    }

    [Serializable]
    public sealed class ContentManifest
    {
        public const int MaximumManifestBytes = 8 * 1024 * 1024;

        [JsonProperty(Required = Required.Always)] public int format = 1;
        [JsonProperty(Required = Required.Always)] public string platform = "iOS";
        [JsonProperty(Required = Required.Always)] public string unityVersion;
        [JsonProperty(Required = Required.Always)] public string contentId;
        [JsonProperty(Required = Required.Always)] public ContentBundle[] bundles;

        public static ContentManifest Parse(string json, string expectedUnity)
        {
            if (json == null || json.Length > MaximumManifestBytes || Encoding.UTF8.GetByteCount(json) > MaximumManifestBytes)
                throw new FormatException("Resource manifest size is invalid.");
            ContentManifest result = JsonConvert.DeserializeObject<ContentManifest>(json);
            if (result == null || result.format != 1 || result.platform != "iOS" || result.unityVersion != expectedUnity ||
                result.bundles == null || result.bundles.Length == 0 || result.bundles.Length > 16384)
                throw new FormatException("Resource platform or Unity version does not match this client.");
            Dictionary<string, ContentBundle> names = new Dictionary<string, ContentBundle>(StringComparer.Ordinal);
            foreach (ContentBundle item in result.bundles)
            {
                if (item == null || !ValidName(item.name) || !ContentIntegrity.ValidHash(item.sha256) ||
                    item.file != item.sha256 + ".bundle" || item.bytes < 1 || item.bytes > 512L * 1024 * 1024 ||
                    item.crc32 == null || !Regex.IsMatch(item.crc32, @"\A[0-9A-F]{8}\z") || names.ContainsKey(item.name))
                    throw new FormatException("Resource bundle identity is invalid or duplicated.");
                item.dependencies = item.dependencies ?? new string[0];
                names.Add(item.name, item);
            }
            result.DependencyOrder(); // Reject unknown references and cycles before accessing files.
            if (result.contentId != ComputeId(result.bundles)) throw new FormatException("Resource manifest identity differs.");
            return result;
        }

        public static bool ValidName(string name)
        {
            return name != null && Regex.IsMatch(name, @"\A[a-z0-9_-]+(?:/[a-z0-9_.-]+)*\z") && !name.Contains("..");
        }

        public static string ComputeId(ContentBundle[] items)
        {
            ContentBundle[] sorted = (ContentBundle[])items.Clone();
            Array.Sort(sorted, delegate(ContentBundle a, ContentBundle b) { return StringComparer.Ordinal.Compare(a.name, b.name); });
            StringBuilder input = new StringBuilder("iOS\n" + CnProtocol.UnityVersion + "\n");
            foreach (ContentBundle item in sorted)
            {
                string[] deps = (string[])(item.dependencies ?? new string[0]).Clone();
                Array.Sort(deps, StringComparer.Ordinal);
                input.Append(item.name).Append('\n').Append(item.sha256).Append('\n')
                    .Append(item.bytes.ToString(CultureInfo.InvariantCulture)).Append('\n').Append(item.crc32).Append('\n')
                    .Append(String.Join(",", deps)).Append('\n');
            }
            return ContentIntegrity.Hash(Encoding.UTF8.GetBytes(input.ToString()));
        }

        public ContentBundle[] DependencyOrder()
        {
            Dictionary<string, ContentBundle> names = new Dictionary<string, ContentBundle>(StringComparer.Ordinal);
            foreach (ContentBundle item in bundles) names.Add(item.name, item);
            Dictionary<string, int> state = new Dictionary<string, int>(StringComparer.Ordinal);
            List<ContentBundle> ordered = new List<ContentBundle>();
            foreach (ContentBundle item in bundles) Visit(item, names, state, ordered);
            return ordered.ToArray();
        }

        private static void Visit(ContentBundle item, Dictionary<string, ContentBundle> names, Dictionary<string, int> state, List<ContentBundle> ordered)
        {
            int current;
            if (state.TryGetValue(item.name, out current))
            {
                if (current == 1) throw new FormatException("Resource dependency cycle.");
                return;
            }
            state[item.name] = 1;
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string dependency in item.dependencies ?? new string[0])
            {
                ContentBundle target;
                if (!ValidName(dependency) || !seen.Add(dependency) || !names.TryGetValue(dependency, out target))
                    throw new FormatException("Unknown or duplicated resource dependency.");
                Visit(target, names, state, ordered);
            }
            state[item.name] = 2;
            ordered.Add(item);
        }
    }

    public static class ContentIntegrity
    {
        public static bool ValidHash(string value) { return value != null && Regex.IsMatch(value, @"\A[0-9a-f]{64}\z"); }
        public static string Hash(byte[] data) { using (SHA256 hash = SHA256.Create()) return Hex(hash.ComputeHash(data)); }
        public static string HashFile(string file) { using (Stream input = File.OpenRead(file)) using (SHA256 hash = SHA256.Create()) return Hex(hash.ComputeHash(input)); }
        private static string Hex(byte[] bytes) { return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant(); }
        private static readonly uint[] CrcTable = CreateCrcTable();

        // Use the reflected IEEE CRC-32 table for both publication and cache checks.
        private static uint[] CreateCrcTable()
        {
            var table = new uint[256];
            for (uint value = 0; value < table.Length; value++)
            {
                uint crc = value;
                for (int bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0 : 0xedb88320);
                table[value] = crc;
            }
            return table;
        }

        private static uint UpdateCrc(uint crc, byte[] data, int count)
        {
            for (int i = 0; i < count; i++) crc = (crc >> 8) ^ CrcTable[(crc ^ data[i]) & 255];
            return crc;
        }

        public static uint FileCrc(string file)
        {
            uint crc = 0xffffffff;
            byte[] buffer = new byte[65536];
            using (Stream input = File.OpenRead(file))
            {
                int count;
                while ((count = input.Read(buffer, 0, buffer.Length)) > 0) crc = UpdateCrc(crc, buffer, count);
            }
            return ~crc;
        }

        // Check both digests in one read so a full cache verification reads the data once.
        public static bool Matches(string file, ContentBundle item)
        {
            if (!File.Exists(file)) return false;
            using (FileStream input = File.OpenRead(file))
            using (SHA256 sha = SHA256.Create())
            {
                if (input.Length != item.bytes) return false;
                byte[] buffer = new byte[65536];
                uint crc = 0xffffffff;
                int count;
                while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    sha.TransformBlock(buffer, 0, count, null, 0);
                    crc = UpdateCrc(crc, buffer, count);
                }
                sha.TransformFinalBlock(buffer, 0, 0);
                return input.Position == item.bytes && Hex(sha.Hash) == item.sha256 &&
                    (~crc).ToString("X8", CultureInfo.InvariantCulture) == item.crc32;
            }
        }
    }

    // Hash-addressed content survives restart and application overwrite; a failed
    // download never replaces a verified version or modifies player preferences.
    public sealed class ContentCache
    {
        private readonly string root;
        public ContentCache(string directory) { root = Path.GetFullPath(directory); Directory.CreateDirectory(root); }
        public string PathFor(ContentBundle item)
        {
            if (item == null || !ContentIntegrity.ValidHash(item.sha256)) throw new ArgumentException("Invalid resource hash.");
            return Path.Combine(root, item.sha256 + ".bundle");
        }
        public bool Contains(ContentBundle item) { return ContentIntegrity.Matches(PathFor(item), item); }
        public string CreateStagingPath() { return Path.Combine(root, Guid.NewGuid().ToString("N") + ".partial"); }
        public string Commit(ContentBundle item, string stagingPath)
        {
            string fullPath = Path.GetFullPath(stagingPath);
            if (Path.GetDirectoryName(fullPath) != root || Path.GetExtension(fullPath) != ".partial") throw new ArgumentException("Staging file must belong to this resource cache.");
            if (!ContentIntegrity.Matches(fullPath, item)) throw new InvalidDataException("Downloaded resource failed size, SHA-256 or CRC verification.");
            string target = PathFor(item);
            if (Contains(item)) { File.Delete(fullPath); return target; }
            if (File.Exists(target)) File.Delete(target); // Only this hash's corrupt cache entry.
            File.Move(fullPath, target);
            return target;
        }
    }
}
