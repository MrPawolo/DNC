Shader "Hidden/ML/LerpTex"
{
    Properties
    {
        _First ("Texture", 2D) = "white" {}
        _Second ("Texture", 2D) = "white" {}
        _Val("Val",Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _First,_Second;
            float4 _First_ST;
            float _Val;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _First);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float4 first = tex2D(_First, i.uv);
                float4 second = tex2D(_Second, i.uv);
                return lerp(first,second,_Val);
            }
            ENDCG
        }
    }
}
