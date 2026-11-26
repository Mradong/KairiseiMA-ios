using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kairisei.Recovery
{
    public static class OriginalLive2DShaders
    {
        private static readonly Dictionary<string,Shader> shaders = new Dictionary<string,Shader>(StringComparer.Ordinal);
        public static void Register(string originalName, Shader shader)
        {
            if (String.IsNullOrEmpty(originalName) || shader == null) throw new ArgumentException("Live2D shader resource missing.");
            shaders[originalName] = shader;
        }
        public static Shader Resolve(string originalName)
        {
            Shader shader;
            if (!shaders.TryGetValue(originalName,out shader) || shader == null)
                throw new InvalidOperationException("Missing original Live2D rendering branch: " + originalName);
            return shader;
        }
        public static void Clear() { shaders.Clear(); }
    }
}
