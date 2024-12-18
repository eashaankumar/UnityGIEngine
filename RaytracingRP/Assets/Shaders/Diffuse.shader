Shader "Unlit/RayTracing/Diffuse"
{
    Properties
    {
        _Color("Main Color", Color) = (1, 1, 1, 1)
        _UseEmission("Use Emission", Float) = 0
        [HDR] _Emission("Emission", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Metallic("Metallic", Range(0.0, 1.0)) = 0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _Color;

                return col;
            }
            ENDCG
        }
    }

    SubShader
    {
        Pass
        {
            Name "Test"
            Tags{ "LightMode" = "RayTracing" }

            HLSLPROGRAM

            #include "UnityShaderVariables.cginc"
            #include "UnityRaytracingMeshUtils.cginc"
            #include "RayPayload.hlsl"
            #include "Utils.hlsl"

            #pragma raytracing test

            float4 _Color;    
            float4 _Emission;
            float _UseEmission;
            float _Metallic;
            float _Smoothness;

            Texture2D _MainTex;
            float4 _MainTex_ST;

            SamplerState sampler_linear_repeat;

            struct AttributeData
            {
                float2 barycentrics;
            };

            struct Vertex
            {
                float3 position;
                float3 normal;
                float2 uv;
            };

            Vertex FetchVertex(uint vertexIndex)
            {
                Vertex v;
                v.position  = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributePosition);
                v.normal    = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributeNormal);
                v.uv        = UnityRayTracingFetchVertexAttribute2(vertexIndex, kVertexAttributeTexCoord0);
                return v;
            }

            Vertex InterpolateVertices(Vertex v0, Vertex v1, Vertex v2, float3 barycentrics)
            {
                Vertex v;
                #define INTERPOLATE_ATTRIBUTE(attr) v.attr = v0.attr * barycentrics.x + v1.attr * barycentrics.y + v2.attr * barycentrics.z
                INTERPOLATE_ATTRIBUTE(position);
                INTERPOLATE_ATTRIBUTE(normal);
                INTERPOLATE_ATTRIBUTE(uv);
                return v;
            }

            void HandlePrimateRay(inout RayPayload payload, AttributeData attribs)
            {
                uint3 triangleIndices = UnityRayTracingFetchTriangleIndices(PrimitiveIndex());

                Vertex v0, v1, v2;
                v0 = FetchVertex(triangleIndices.x);
                v1 = FetchVertex(triangleIndices.y);
                v2 = FetchVertex(triangleIndices.z);

                float3 barycentricCoords = float3(1.0 - attribs.barycentrics.x - attribs.barycentrics.y, attribs.barycentrics.x, attribs.barycentrics.y);
                Vertex v = InterpolateVertices(v0, v1, v2, barycentricCoords);

                float3 worldPosition = mul(ObjectToWorld(), float4(v.position, 1));

                float3 e0 = v1.position - v0.position;
                float3 e1 = v2.position - v0.position;

                //      float3 faceNormal = normalize(mul(cross(e0, e1), (float3x3)WorldToObject()));
                float3 faceNormal = normalize(mul(v.normal, (float3x3)WorldToObject()));

                bool isFrontFace = (HitKind() == HIT_KIND_TRIANGLE_FRONT_FACE);
                faceNormal = (isFrontFace == false) ? -faceNormal : faceNormal;

                float3 texColor = _MainTex.SampleLevel(sampler_linear_repeat, v.uv * _MainTex_ST.xy, 0).rgb;

                float3 albedo = texColor * _Color.xyz;


                if (_UseEmission > 0)
                {
                    payload.energy = _Emission;
                    payload.color = payload.primateColor = _Emission;
                }
                else
                {
                    payload.energy = 0;
                    payload.color = payload.primateColor = float4(albedo, 1);
                }

                payload.worldPos = float4(worldPosition, 1);
                payload.primateNormal = faceNormal;
                payload.bounceIndex += 1;
            }

            void HandleDirectDiffuseRay(inout RayPayload payload, AttributeData attribs)
            {
                uint3 triangleIndices = UnityRayTracingFetchTriangleIndices(PrimitiveIndex());

                Vertex v0, v1, v2;
                v0 = FetchVertex(triangleIndices.x);
                v1 = FetchVertex(triangleIndices.y);
                v2 = FetchVertex(triangleIndices.z);

                float3 barycentricCoords = float3(1.0 - attribs.barycentrics.x - attribs.barycentrics.y, attribs.barycentrics.x, attribs.barycentrics.y);
                Vertex v = InterpolateVertices(v0, v1, v2, barycentricCoords);

                float3 worldPosition = mul(ObjectToWorld(), float4(v.position, 1));

                float3 e0 = v1.position - v0.position;
                float3 e1 = v2.position - v0.position;

                //      float3 faceNormal = normalize(mul(cross(e0, e1), (float3x3)WorldToObject()));
                float3 faceNormal = normalize(mul(v.normal, (float3x3)WorldToObject()));

                bool isFrontFace = (HitKind() == HIT_KIND_TRIANGLE_FRONT_FACE);
                faceNormal = (isFrontFace == false) ? -faceNormal : faceNormal;

                float3 texColor = _MainTex.SampleLevel(sampler_linear_repeat, v.uv * _MainTex_ST.xy, 0).rgb;

                float3 albedo = texColor * _Color.xyz;

                if (_UseEmission > 0)
                {
                    payload.energy = _Emission;
                    payload.color = _Emission;
                }
                else
                {
                    payload.energy = 0;
                    payload.color = float4(albedo, 1);
                }

                float3 diffuseRayDir = normalize(faceNormal + RandomUnitVector(payload.rngState));
                //if (dot(v.normal, diffuseRayDir) < 0) diffuseRayDir *= -1;

                //float3 diffuseRayDir = SampleHemisphere(v.normal, payload.rngState);
                float3 specularRayDir = reflect(WorldRayDirection(), faceNormal);

                float fresnelFactor =1;
                float specularChance = lerp(_Metallic, 1, fresnelFactor * _Smoothness);
                float doSpecular = (RandomFloat01(payload.rngState) < specularChance) ? 1 : 0;
                float3 reflectedRayDir = lerp(diffuseRayDir, specularRayDir, doSpecular);

                payload.bounceRayOrigin = worldPosition + K_RAY_ORIGIN_PUSH_OFF * faceNormal;
                payload.bounceRayDir = reflectedRayDir;

                payload.bounceIndex += 1;
            }

            [shader("closesthit")]
            void ClosestHitMain(inout RayPayload payload : SV_RayPayload, AttributeData attribs : SV_IntersectionAttributes)
            {
                /*if (payload.bounceIndexOpaque > 1)
                {
                    payload.bounceIndexOpaque = -1;
                    return;
                }*/
                // primate
                if (payload.rayType == 0)
                {
                    HandlePrimateRay(payload, attribs);
                }
                else if (payload.rayType == 1)
                {
                    HandleDirectDiffuseRay(payload, attribs);
                }
                else if (payload.rayType == 2)
                {
                    HandleDirectDiffuseRay(payload, attribs);
                }
            }

            ENDHLSL
        }
    }
}
