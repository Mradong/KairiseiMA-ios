Shader "Recovery/OriginalSceneUI"
{
    Properties
    {
        _MainTex ("Original atlas", 2D) = "white" {}
        _ClipRect ("Panel bounds in world space", Vector) = (-10000, -10000, 10000, 10000)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Input
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct Output
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 world : TEXCOORD1;
                fixed4 color : COLOR;
            };
            sampler2D _MainTex;
            float4 _ClipRect;

            Output vert(Input v)
            {
                Output o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(Output i) : SV_Target
            {
                clip(i.world - _ClipRect.xy);
                clip(_ClipRect.zw - i.world);
                return tex2D(_MainTex, i.uv) * i.color;
            }
            ENDCG
        }
    }
}
