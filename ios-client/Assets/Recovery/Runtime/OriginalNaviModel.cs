using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Kairisei.Recovery.Cubism2;

namespace Kairisei.Recovery
{
    public sealed class OriginalNaviModel : MonoBehaviour
    {
        public const string BundleName="recovery/world.dat";
        public bool Ready { get; private set; }
        private Live2DModelUnity model;
        private Camera targetCamera;
        private Matrix4x4 modelMatrix;
        private Live2DMotion idle;
        private readonly List<Live2DMotion> touchMotions = new List<Live2DMotion>();
        private readonly List<int> hitAreas = new List<int>();
        private int nextTouch;
        private MotionQueueManager motionQueue;
        private PartPose pose;
        private long started;

        private sealed class PartPose
        {
            private sealed class Part
            {
                public int Index;
                public int Visibility;
                public readonly List<int> Links = new List<int>();
            }

            private readonly List<Part[]> groups = new List<Part[]>();
            private readonly float fadeMilliseconds;
            private long? lastUpdate;

            public PartPose(Live2DModelUnity model, JObject definition)
            {
                fadeMilliseconds = (float?)definition["fade_in"] ?? 500f;
                if (!(definition["parts_visible"] is JArray rows) || rows.Count == 0)
                    throw new FormatException("Original Live2D pose has no part groups.");
                foreach (JObject row in rows)
                {
                    if (!(row["group"] is JArray members) || members.Count == 0)
                        throw new FormatException("Original Live2D pose has an empty part group.");
                    var group = new Part[members.Count];
                    for (int i = 0; i < members.Count; i++)
                    {
                        string id = (string)members[i]["id"];
                        var part = new Part { Index = ResolvePart(model, id), Visibility = model.getParamIndex("VISIBLE:" + id) };
                        if (members[i]["link"] is JArray links)
                            foreach (JToken link in links) part.Links.Add(ResolvePart(model, (string)link));
                        group[i] = part;
                    }
                    groups.Add(group);
                }
            }

            private static int ResolvePart(Live2DModelUnity model, string id)
            {
                int index = String.IsNullOrEmpty(id) ? -1 : model.getPartsDataIndex(id);
                if (index < 0) throw new FormatException("Original Live2D pose references a missing part: " + id);
                return index;
            }

            public void Update(Live2DModelUnity model, long now)
            {
                float fadeStep = lastUpdate.HasValue && fadeMilliseconds > 0
                    ? Mathf.Clamp01((now - lastUpdate.Value) / fadeMilliseconds) : 1f;
                lastUpdate = now;
                foreach (Part[] group in groups)
                {
                    Part visible = null;
                    foreach (Part part in group)
                    {
                        if (model.getParamFloat(part.Visibility) == 0) continue;
                        visible = part;
                        break;
                    }
                    float opacity = visible == null ? 1f : Mathf.Clamp01(model.getPartsOpacity(visible.Index) + fadeStep);
                    if (visible == null) visible = group[0];
                    model.setPartsOpacity(visible.Index, opacity);

                    // Cubism 2 limits the outgoing part during the pose crossfade.
                    float outgoingLimit = 1f - opacity;
                    if (opacity * outgoingLimit > 0.15f)
                        outgoingLimit = 1f - 0.15f / (1f - opacity);
                    foreach (Part part in group)
                    {
                        if (part != visible) model.setPartsOpacity(part.Index, Mathf.Min(model.getPartsOpacity(part.Index), outgoingLimit));
                    }
                    foreach (Part part in group)
                        foreach (int link in part.Links) model.setPartsOpacity(link, model.getPartsOpacity(part.Index));
                }
            }
        }

        public static void ValidateContent(RecoveryContentLoader content)
        {
            JObject definition=JObject.Parse(content.Load<TextAsset>(BundleName,"originalworld").text);
            if((int?)definition["schemaVersion"]!=1||(int?)definition["naviType"]!=0)throw new InvalidOperationException("Unsupported original navi model.");
            string background=(string)definition["backgroundAsset"];
            if(String.IsNullOrEmpty(background)||content.Load<GameObject>(BundleName,background)==null)throw new InvalidOperationException("Original home background is absent.");
            foreach(JObject shader in (JArray)definition["shaderAssets"])
            {
                string original=(string)shader["originalName"],address=(string)shader["address"];
                if(String.IsNullOrEmpty(original)||String.IsNullOrEmpty(address)||content.Load<Shader>(BundleName,address)==null)
                    throw new InvalidOperationException("Original Live2D shader is absent.");
            }
            JObject modelDefinition=(JObject)definition["model"];
            string moc=((string)modelDefinition["model"]??"").ToLowerInvariant();
            if(String.IsNullOrEmpty(moc))throw new InvalidOperationException("Original Live2D model is absent.");
            Live2DModelUnity probe=null;
            try
            {
                probe=Live2DModelUnity.loadModel(content.Load<TextAsset>(BundleName,moc).bytes);
                if(probe==null||probe.getCanvasWidth()<=0||probe.getCanvasHeight()<=0)throw new InvalidOperationException("Original Live2D model could not be decoded.");
                LoadPose(content, probe, modelDefinition);
                JArray textures=(JArray)modelDefinition["textures"];
                if(textures==null||textures.Count==0)throw new InvalidOperationException("Original Live2D textures are absent.");
                foreach(JToken texture in textures)content.Load<Texture2D>(BundleName,((string)texture).ToLowerInvariant());
                JObject motions = (JObject)modelDefinition["motions"];
                LoadMotion(content, (JObject)motions["idle"][0]);
                foreach (JObject motion in (JArray)motions["tap_body"]) LoadMotion(content, motion);
            }
            finally{if(probe!=null)probe.releaseModel();}
        }
        public void Initialize(RecoveryContentLoader content,Camera camera)
        {
            targetCamera=camera;
            JObject definition=JObject.Parse(content.Load<TextAsset>(BundleName,"originalworld").text);
            if((int?)definition["schemaVersion"]!=1||(int?)definition["naviType"]!=0)throw new InvalidOperationException("Unsupported original navi model.");
            foreach(JObject shader in (JArray)definition["shaderAssets"])
                OriginalLive2DShaders.Register((string)shader["originalName"],content.Load<Shader>(BundleName,(string)shader["address"]));
            JObject layout=(JObject)definition["model"]["layout"];
            string moc=((string)definition["model"]["model"]).ToLowerInvariant();
            model=Live2DModelUnity.loadModel(content.Load<TextAsset>(BundleName,moc).bytes);
            pose = LoadPose(content, model, (JObject)definition["model"]);
            model.setRenderMode(0);
            JArray textures=(JArray)definition["model"]["textures"];
            for(int i=0;i<textures.Count;i++)model.setTexture(i,content.Load<Texture2D>(BundleName,((string)textures[i]).ToLowerInvariant()));
            float width=model.getCanvasWidth(),height=model.getCanvasHeight();
            float scale=layout["height"]!=null?(float)layout["height"]/height:2f/width;
            float centerX=(float?)layout["center_x"]??0,centerY=(float?)layout["center_y"]??0;
            modelMatrix=Matrix4x4.TRS(new Vector3(centerX-width*scale/2,centerY+height*scale/2,0),Quaternion.identity,new Vector3(scale,-scale,-1));
            JObject motions = (JObject)definition["model"]["motions"];
            idle = LoadMotion(content, (JObject)motions["idle"][0]);
            foreach (JObject motion in (JArray)motions["tap_body"]) touchMotions.Add(LoadMotion(content, motion));
            foreach (JObject hitArea in (JArray)definition["model"]["hit_areas"])
            {
                int index = model.getDrawDataIndex((string)hitArea["id"]);
                if (index >= 0) hitAreas.Add(index);
            }
            motionQueue=new MotionQueueManager();started=UtSystem.getUserTimeMSec();Ready=true;
            Update();
        }
        private static Live2DMotion LoadMotion(RecoveryContentLoader content, JObject definition)
        {
            string file = ((string)definition["file"]).ToLowerInvariant();
            Live2DMotion motion = Live2DMotion.loadMotion(content.Load<TextAsset>(BundleName, file).bytes);
            if (motion == null) throw new InvalidOperationException("Could not decode original navi motion: " + file);
            if ((int?)definition["fade_in"] is int fadeIn) motion.setFadeIn(fadeIn);
            if ((int?)definition["fade_out"] is int fadeOut) motion.setFadeOut(fadeOut);
            return motion;
        }

        private static PartPose LoadPose(RecoveryContentLoader content, Live2DModelUnity model, JObject definition)
        {
            string address = ((string)definition["pose"])?.ToLowerInvariant();
            if (String.IsNullOrEmpty(address)) throw new FormatException("Original Live2D pose asset is absent.");
            return new PartPose(model, JObject.Parse(content.Load<TextAsset>(BundleName, address).text));
        }

        private void Update()
        {
            if(!Ready)return;
            if(motionQueue.isFinished())motionQueue.startMotion(idle,false);
            model.loadParam();motionQueue.updateParam(model);model.saveParam();
            long now = UtSystem.getUserTimeMSec();
            double seconds=(now-started)/1000.0;
            model.setParamFloat("PARAM_BREATH",(float)(0.5+0.5*Math.Sin(seconds*2.0)));
            pose.Update(model, now);
            model.update();
        }
        private void OnRenderObject()
        {
            if(!Ready||Camera.current!=targetCamera)return;
            // The original canvas rotates -90 degrees; LAppModel.draw applies +90.
            model.setMatrix(ModelToWorld());
            model.draw();
        }

        private Matrix4x4 ModelToWorld()
        {
            // Live2D_Canvas_0 has local extents (10, 0, 10), in the canvas's XZ plane.
            var canvasScale = Matrix4x4.Scale(new Vector3(10, 1, 10));
            return transform.localToWorldMatrix * canvasScale * Matrix4x4.Rotate(Quaternion.Euler(90, 0, 0)) * modelMatrix;
        }

        public bool TryPlayTouch(Ray ray)
        {
            if (!Ready || touchMotions.Count == 0) return false;
            Matrix4x4 worldToModel = ModelToWorld().inverse;
            var localRay = new Ray(worldToModel.MultiplyPoint3x4(ray.origin), worldToModel.MultiplyVector(ray.direction));
            if (!new Plane(Vector3.forward, Vector3.zero).Raycast(localRay, out float distance)) return false;
            Vector3 point = localRay.GetPoint(distance);
            foreach (int index in hitAreas)
            {
                float[] vertices = model.getTransformedPoints(index);
                if (vertices == null || vertices.Length < 2) continue;
                Vector2 min = new Vector2(vertices[0], vertices[1]), max = min;
                for (int i = 2; i + 1 < vertices.Length; i += 2)
                {
                    var vertex = new Vector2(vertices[i], vertices[i + 1]);
                    min = Vector2.Min(min, vertex);
                    max = Vector2.Max(max, vertex);
                }
                if (!Rect.MinMaxRect(min.x, min.y, max.x, max.y).Contains(point)) continue;
                motionQueue.startMotion(touchMotions[nextTouch], false);
                nextTouch = (nextTouch + 1) % touchMotions.Count;
                return true;
            }
            return false;
        }
        private void OnDestroy(){Ready=false;if(model!=null)model.releaseModel();model=null;}
    }
}
