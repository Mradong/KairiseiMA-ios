using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kairisei.Recovery
{
    internal sealed class OriginalSceneMotion
    {
        private readonly List<Action<float>> tracks = new List<Action<float>>();
        private readonly float duration;
        private readonly bool loop;
        private readonly float started;

        public OriginalSceneMotion(JObject clip, Transform origin, float started, Func<Transform, Action<float>> bindOpacity)
        {
            duration = (float)clip["duration"];
            loop = (bool)clip["loop"];
            this.started = started;
            foreach (JObject track in (JArray)clip["tracks"])
            {
                string path = (string)track["path"];
                Transform target = String.IsNullOrEmpty(path) ? origin : origin.Find(path);
                if (target == null) continue;
                JArray keys = (JArray)track["keys"];
                string property = (string)track["property"];
                if (property == "alpha")
                {
                    Action<float> apply = bindOpacity(target);
                    AnimationCurve alpha = Curve(keys, 1, 1);
                    tracks.Add(time => apply(Mathf.Clamp01(alpha.Evaluate(time))));
                    continue;
                }
                AnimationCurve x = Curve(keys, 1, 3), y = Curve(keys, 2, 3), z = Curve(keys, 3, 3);
                if (property == "position")
                    tracks.Add(time => target.localPosition = new Vector3(x.Evaluate(time), y.Evaluate(time), z.Evaluate(time)));
                if (property == "scale")
                    tracks.Add(time => target.localScale = new Vector3(x.Evaluate(time), y.Evaluate(time), z.Evaluate(time)));
            }
        }

        public bool Sample(float now)
        {
            float elapsed = Mathf.Max(0, now - started);
            float time = loop && duration > 0 ? Mathf.Repeat(elapsed, duration) : Mathf.Min(elapsed, duration);
            foreach (Action<float> sample in tracks) sample(time);
            return loop || elapsed < duration;
        }

        private static AnimationCurve Curve(JArray rows, int axis, int stride)
        {
            var keys = new Keyframe[rows.Count];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = new Keyframe((float)rows[i][0], (float)rows[i][axis], (float)rows[i][axis + stride], (float)rows[i][axis + 2 * stride]);
            return new AnimationCurve(keys);
        }
    }
}
