using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

// Independent, metadata-only v21/v39 enum reader. Never loads a game executable.
class Order03MetadataEnumsProbe {
    static byte[] b; static int strings, stringsEnd, types, typeBytes, fields, fieldBytes, defaults, defaultBytes, data, dataBytes;
    static int I(int offset) { return BitConverter.ToInt32(b, offset); }
    static int U(int offset, int width) { return width == 2 ? BitConverter.ToUInt16(b, offset) : I(offset); }
    // The selected enum constants use only the one- and two-byte compressed
    // UInt32 forms followed by signed zigzag decoding. Reject other forms.
    static int CompressedInt(int offset, int end) {
        if (offset >= end) throw new Exception("Compressed constant bounds");
        uint value = b[offset];
        if (value >= 0x80) {
            if (value >= 0xc0 || offset + 1 >= end) throw new Exception("Unsupported compressed constant");
            value = ((value & 0x3f) << 8) | b[offset + 1];
        }
        return (value & 1) != 0 ? -(int)(value >> 1) - 1 : (int)(value >> 1);
    }
    static string S(int index) {
        int at = strings + index, end = at;
        if (index < 0 || at >= stringsEnd) throw new Exception("String bounds");
        while (end < stringsEnd && b[end] != 0) end++;
        if (end == stringsEnd) throw new Exception("Unterminated string");
        return new UTF8Encoding(false, true).GetString(b, at, end - at);
    }
    static int Main(string[] args) {
        try {
            b = File.ReadAllBytes(args[0]);
            if (b.Length < 240 || BitConverter.ToUInt32(b, 0) != 0xfab11baf || (I(4) != 21 && I(4) != 39)) throw new Exception("Unsupported metadata");
            bool modern = I(4) == 39; int entry = modern ? 12 : 8;
            strings = I(8 + 2 * entry); stringsEnd = strings + I(12 + 2 * entry); types = I(8 + 19 * entry); typeBytes = I(12 + 19 * entry);
            fields = I(8 + 11 * entry); fieldBytes = I(12 + 11 * entry); defaults = I(8 + 7 * entry); defaultBytes = I(12 + 7 * entry); data = I(8 + 8 * entry); dataBytes = I(12 + 8 * entry);
            int typeWidth = modern ? I(12 + 18 * entry) / I(16 + 18 * entry) - 4 : 4;
            int genericWidth = modern ? (I(16 + 14 * entry) <= 255 ? 1 : I(16 + 14 * entry) <= 65535 ? 2 : 4) : 4;
            int typeSize = modern ? 68 + 3 * typeWidth + genericWidth : 120;
            int fieldSize = modern ? 8 + typeWidth : 16, defaultSize = modern ? 8 + typeWidth : 12;
            int startAt = modern ? 12 + 3 * typeWidth + genericWidth : 64;
            int countAt = modern ? startAt + 36 : 100, bitAt = modern ? typeSize - 8 : 112;
            if (typeBytes % typeSize != 0 || fieldBytes % fieldSize != 0 || defaultBytes % defaultSize != 0) throw new Exception("Invalid record sizes");
            int int32Type = -1;
            for (int t = types; t < types + typeBytes; t += typeSize) if (S(I(t)) == "Int32" && S(I(t + 4)) == "System") {
                if (int32Type != -1) throw new Exception("Ambiguous System.Int32");
                int32Type = U(t + (modern ? 8 : 12), typeWidth);
            }
            if (int32Type < 0) throw new Exception("Missing System.Int32");
            var output = new JArray();
            foreach (string family in args[1].Split(',')) {
                JObject found = null;
                for (int type = 0; type < typeBytes / typeSize; type++) {
                    int t = types + type * typeSize;
                    if (S(I(t)) != family || S(I(t + 4)) != "") continue;
                    if (found != null || (I(t + bitAt) & 3) != 3) throw new Exception("Enum identity mismatch");
                    int start = I(t + startAt), count = BitConverter.ToUInt16(b, t + countAt);
                    if (start < 0 || count < 2 || start + count > fieldBytes / fieldSize || S(I(fields + start * fieldSize)) != "value__") throw new Exception("Enum fields mismatch");
                    var values = new JArray();
                    for (int field = start + 1; field < start + count; field++) {
                        string name = S(I(fields + field * fieldSize));
                        int matched = 0, constant = 0, position = 0;
                        for (int d = defaults; d < defaults + defaultBytes; d += defaultSize) {
                            if (I(d) != field) continue;
                            int offset = I(d + 4 + typeWidth);
                            if (U(d + 4, typeWidth) != int32Type || offset < 0 || offset + (modern ? 1 : 4) > dataBytes) throw new Exception("Constant mismatch");
                            matched++; constant = modern ? CompressedInt(data + offset, data + dataBytes) : I(data + offset); position = data + offset;
                        }
                        if (matched != 1) throw new Exception("Default cardinality mismatch");
                        if (name != "sizeof_" + family) values.Add(new JObject { { "name", name }, { "value", constant }, { "dataOffset", position } });
                    }
                    found = new JObject { { "name", family }, { "values", values } };
                }
                if (found == null) throw new Exception("Missing enum: " + family);
                output.Add(found);
            }
            File.WriteAllText(args[2], output.ToString(Newtonsoft.Json.Formatting.None), new UTF8Encoding(false));
            return 0;
        } catch (Exception e) { Console.Error.WriteLine(e.GetType().Name + ": " + e.Message); return 1; }
    }
}
