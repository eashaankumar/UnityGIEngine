Shader "Hidden/GaussianBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            #define PI 3.1415925

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
            int _Width;
            int _Height;
            float _Strength;
            int _BlurRadius;

            float gaussian(float x, float y)
            {
                float var = _Strength * _Strength;
                float den = 2 * PI * var;
                float ex = -(x * x + y * y) / (2 * var);
                return 1 / den * exp(ex);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 d = 1 /float2(_Width, _Height);
                float2 dX = float2(d.x, 0);
                float2 dY = float2(0, d.y);

                fixed4 col = 0;
                /*col += tex2D(_MainTex, i.uv) * 1 / 4.0;
                col += tex2D(_MainTex, i.uv + dX) * 1 / 8.0;
                col += tex2D(_MainTex, i.uv + -dX) * 1 / 8.0;
                col += tex2D(_MainTex, i.uv + dY) * 1 / 8.0;
                col += tex2D(_MainTex, i.uv + -dY) * 1 / 8.0;

                col += tex2D(_MainTex, i.uv + dX + dY) * 1 / 16.0;
                col += tex2D(_MainTex, i.uv + dX - dY) * 1 / 16.0;
                col += tex2D(_MainTex, i.uv + -dY -dY) * 1 / 16.0;
                col += tex2D(_MainTex, i.uv + -dX + dY) * 1 / 16.0;*/

                float total = 0;
                for (int x = -_BlurRadius; x <= _BlurRadius; x++)
                {
                    for (int y = -_BlurRadius; y <= _BlurRadius; y++)
                    {
                        float gaus = gaussian(x, y);
                        col += tex2D(_MainTex, i.uv + dX * x + dY * y) * gaus;
                        total += gaus;
                    }
                }

                if (_BlurRadius == 0) col = tex2D(_MainTex, i.uv);
                else {
                    col /= total;
                    col = saturate(col);
                }


                // just invert the colors
                return col;
            }
            ENDCG
        }
    }
}
