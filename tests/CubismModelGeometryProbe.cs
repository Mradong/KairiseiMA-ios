using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using SDK_NAMESPACE;

public static class CubismModelGeometryProbe
{
    sealed class Capture : DrawParam
    {
        public MemoryStream Bytes = new MemoryStream(); public int Draws;
        public Capture() { for(int i=0;i<4;i++)createTextureInfo(i); }
        public void Reset() { Bytes.Dispose(); Bytes=new MemoryStream(); Draws=0; }
        public override void drawTexture(int texture,int count,ushort[] indices,float[] points,float[] uv,float opacity,int flags)
        {
            Draws++;var w=new BinaryWriter(Bytes);w.Write(texture);w.Write(count);w.Write(opacity);w.Write(flags);
            w.Write(indices.Length);foreach(ushort v in indices)w.Write(v);
            w.Write(points.Length);foreach(float v in points)w.Write(v);
            w.Write(uv.Length);foreach(float v in uv)w.Write(v);w.Flush();
        }
        public override void drawLast() { }
    }
    sealed class CpuModel : ALive2DModel
    {
        public Capture Capture = new Capture();
        public override void deleteTextures() { throw new NotSupportedException("CPU-only probe allocates no texture."); }
        public override DrawParam getDrawParam() { return Capture; }
        public override void draw() { getModelContext().draw(Capture); }
    }
    public static int Main(string[] args)
    {
        try
        {
            foreach(FieldInfo field in typeof(DrawParam).GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))
                if(field.FieldType==typeof(DrawParam.ClippingMethod))field.SetValue(null,DrawParam.ClippingMethod.Ignore);
            var model=new CpuModel();ALive2DModel.loadModel_exe(model,new MemoryStream(File.ReadAllBytes(args[0])));
            if(model.getCanvasWidth()<=0||model.getCanvasHeight()<=0)throw new Exception("Model did not load");
            Console.WriteLine("canvas="+model.getCanvasWidth()+","+model.getCanvasHeight());
            int cases=0;
            foreach(float angle in new[]{-30f,0f,30f})foreach(float eye in new[]{0f,0.5f,1f})foreach(float mouth in new[]{0f,0.5f,1f})
            {
                model.setParamFloat("PARAM_ANGLE_X",angle);model.setParamFloat("PARAM_EYE_L_OPEN",eye);model.setParamFloat("PARAM_MOUTH_OPEN_Y",mouth);
                model.update();model.Capture.Reset();
                int drawCount=0,floatCount=0;
                foreach(var part in model.getModelImpl().getPartsDataList())drawCount+=part.getDrawData().Count;
                var writer=new BinaryWriter(model.Capture.Bytes);
                for(int index=0;index<drawCount;index++)
                {
                    object context=model.getModelContext().getDrawContext(index);
                    var arrays=new SortedDictionary<string,float[]>();
                    for(Type type=context.GetType();type!=null&&type!=typeof(object);type=type.BaseType)
                        foreach(var field in type.GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly))
                        {
                            Type fieldType;
                            try { fieldType=field.FieldType; } catch(TypeLoadException) { continue; }
                            if(fieldType!=typeof(float[]))continue;
                            var values=(float[])field.GetValue(context);if(values==null)continue;
                            string key=field.Name.StartsWith("CubismField_",StringComparison.Ordinal)?"id:"+field.Name.Substring(12)
                                : field.IsPublic&&Valid(field.Name)?"public:"+field.Name:"id:"+(field.MetadataToken&0xffffff);
                            arrays[key]=values;
                        }
                    writer.Write(index);writer.Write(arrays.Count);
                    foreach(var pair in arrays){writer.Write(pair.Key);writer.Write(pair.Value.Length);foreach(float value in pair.Value)writer.Write(value);floatCount+=pair.Value.Length;}
                }
                writer.Flush();if(drawCount<1||floatCount<1)throw new Exception("No real deformed model geometry");
                using(var hash=SHA256.Create()) Console.WriteLine(cases+":"+drawCount+":"+floatCount+":"+BitConverter.ToString(hash.ComputeHash(model.Capture.Bytes.ToArray())).Replace("-",""));
                cases++;
            }
            Console.WriteLine("cases="+cases);return 0;
        }
        catch(Exception error){Console.WriteLine("FAIL "+error.GetType().Name+": "+error.Message+"\n"+error.StackTrace);return 1;}
    }
    static bool Valid(string name){if(name.Length==0)return false;foreach(char c in name)if(!(Char.IsLetterOrDigit(c)||c=='_'))return false;return true;}
}
