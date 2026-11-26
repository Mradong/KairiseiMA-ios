using System;
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
        private MotionQueueManager motionQueue;
        private long started;
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
                JArray textures=(JArray)modelDefinition["textures"];
                if(textures==null||textures.Count==0)throw new InvalidOperationException("Original Live2D textures are absent.");
                foreach(JToken texture in textures)content.Load<Texture2D>(BundleName,((string)texture).ToLowerInvariant());
                JObject idle=(JObject)modelDefinition["motions"]["idle"][0];
                if(idle==null||Live2DMotion.loadMotion(content.Load<TextAsset>(BundleName,((string)idle["file"]).ToLowerInvariant()).bytes)==null)
                    throw new InvalidOperationException("Original Live2D idle motion could not be decoded.");
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
            model.setRenderMode(0);
            JArray textures=(JArray)definition["model"]["textures"];
            for(int i=0;i<textures.Count;i++)model.setTexture(i,content.Load<Texture2D>(BundleName,((string)textures[i]).ToLowerInvariant()));
            float width=model.getCanvasWidth(),height=model.getCanvasHeight();
            float scale=layout["height"]!=null?(float)layout["height"]/height:2f/width;
            float centerX=(float?)layout["center_x"]??0,centerY=(float?)layout["center_y"]??0;
            modelMatrix=Matrix4x4.Scale(new Vector3(10,1,10))*Matrix4x4.TRS(new Vector3(centerX-width*scale/2,centerY+height*scale/2,0),Quaternion.identity,new Vector3(scale,-scale,-1));
            JObject motion=(JObject)definition["model"]["motions"]["idle"][0];
            idle=Live2DMotion.loadMotion(content.Load<TextAsset>(BundleName,((string)motion["file"]).ToLowerInvariant()).bytes);
            motionQueue=new MotionQueueManager();started=UtSystem.getUserTimeMSec();Ready=true;
        }
        private void Update()
        {
            if(!Ready)return;
            if(motionQueue.isFinished())motionQueue.startMotion(idle,false);
            model.loadParam();motionQueue.updateParam(model);model.saveParam();
            double seconds=(UtSystem.getUserTimeMSec()-started)/1000.0;
            model.setParamFloat("PARAM_BREATH",(float)(0.5+0.5*Math.Sin(seconds*2.0)));
            model.update();
        }
        private void OnRenderObject()
        {
            if(!Ready||Camera.current!=targetCamera)return;
            // The original canvas rotates -90 degrees; LAppModel.draw applies +90.
            model.setMatrix(transform.localToWorldMatrix*Matrix4x4.Rotate(Quaternion.Euler(90,0,0))*modelMatrix);
            model.draw();
        }
        private void OnDestroy(){Ready=false;if(model!=null)model.releaseModel();model=null;}
    }
}
