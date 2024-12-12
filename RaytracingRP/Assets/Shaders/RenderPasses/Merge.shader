Shader "Hidden/Merge"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MipBlendPower ("Mip Blend Power", Float) = 1
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            int _MipCount;
            float _MipBlendPower;

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = 0;
                float prevCascadeMask = 1;
                for (int m = 0; m < _MipCount; m++)
                {
                    fixed4 c = tex2Dlod(_MainTex, float4(i.uv, 0, m));
                    float weight = m + 1;
                    if (m > 0) weight = pow(weight, 1/_MipBlendPower);
                    col += c / weight;
                }
                return col;
            }
            ENDCG
        }
    }
}
