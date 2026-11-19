using System;
using Newtonsoft.Json.Linq;

namespace Kairisei.Recovery
{
    // TexturePacker JSON uses a top-left origin; Unity UVs use bottom-left.
    public sealed class RecoveryAtlas
    {
        private readonly JObject frames;
        private readonly int width, height;

        public RecoveryAtlas(string json, int textureWidth, int textureHeight)
        {
            JObject root = JObject.Parse(json);
            frames = root["frames"] as JObject;
            JObject size = root["meta"] == null ? null : root["meta"]["size"] as JObject;
            if (frames == null || size == null || Integer(size, "w") != textureWidth || Integer(size, "h") != textureHeight || textureWidth <= 0 || textureHeight <= 0)
                throw new FormatException("Atlas dimensions or frames are invalid.");
            width = textureWidth; height = textureHeight;
        }

        // x, y, width, height in Unity's normalized UV space.
        public float[] Frame(string name)
        {
            JObject item = frames[name] as JObject;
            JObject rect = item == null ? null : item["frame"] as JObject;
            if (rect == null || item["rotated"] == null || item["rotated"].Type != JTokenType.Boolean || (bool)item["rotated"] ||
                item["trimmed"] == null || item["trimmed"].Type != JTokenType.Boolean || (bool)item["trimmed"])
                throw new FormatException("Atlas frame is missing or unsupported: " + name);
            int x = Integer(rect, "x"), y = Integer(rect, "y"), w = Integer(rect, "w"), h = Integer(rect, "h");
            if (x < 0 || y < 0 || w <= 0 || h <= 0 || (long)x + w > width || (long)y + h > height)
                throw new FormatException("Atlas frame lies outside its texture: " + name);
            return new[] { (float)x / width, (float)(height - y - h) / height, (float)w / width, (float)h / height };
        }

        private static int Integer(JObject value, string name)
        {
            JToken field = value[name];
            if (field == null || field.Type != JTokenType.Integer) throw new FormatException("Invalid atlas integer: " + name);
            long n = field.Value<long>();
            if (n < Int32.MinValue || n > Int32.MaxValue) throw new FormatException("Atlas integer out of range: " + name);
            return (int)n;
        }
    }
}
