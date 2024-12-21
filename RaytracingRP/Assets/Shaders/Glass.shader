﻿Shader "Unlit/RayTracing/Glass"
{
    Properties
    {
        _Color("Main Color", Color) = (1, 1, 1, 1)
        _RefractiveIndex("Refractive Index", Range(1.0, 2.0)) = 1.55
		_MagicValue("Magic Value", Range(0.0, 1.0)) = 0
        _MainTex("Albedo (RGB)", 2D) = "white" {}
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
                fixed4 col = _Color * 2;
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
			
			float _MagicValue;

            RaytracingAccelerationStructure g_SceneAccelStruct;

            float _RefractiveIndex;

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
                v.position = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributePosition);
                v.normal = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributeNormal);
				v.uv = UnityRayTracingFetchVertexAttribute2(vertexIndex, kVertexAttributeTexCoord0);
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

            void fresnel(in float3 I, in float3 N, in float ior, out float kr)
            {
                float cosi = clamp(-1, 1, dot(I, N));
                float etai = 1, etat = ior;
                if (cosi > 0) 
                { 
                    float temp = etai;
                    etai = etat;
                    etat = temp;
                }
                // Compute sini using Snell's law
                float sint = etai / etat * sqrt(max(0.f, 1 - cosi * cosi));
                // Total internal reflection
                if (sint >= 1) 
                {
                    kr = 1;
                }
                else 
                {
                    float cost = sqrt(max(0, 1 - sint * sint));
                    cosi = abs(cosi);
                    float Rs = ((etat * cosi) - (etai * cost)) / ((etat * cosi) + (etai * cost));
                    float Rp = ((etai * cosi) - (etat * cost)) / ((etai * cosi) + (etat * cost));
                    kr = (Rs * Rs + Rp * Rp) / 2;
                }
                // As a consequence of the conservation of energy, transmittance is given by:
                // kt = 1 - kr;
            }

            float3 TraceGlassRay(in float3 faceNormal, in float refractiveIndex, in float3 worldPosition, inout RayPayload payload, inout RayDesc ray, inout float3 worldPosFinal)
            {
                float kr;
                fresnel(WorldRayDirection(), faceNormal, _RefractiveIndex, kr);

                float3 refractedRay = refract(WorldRayDirection(), faceNormal, refractiveIndex);
                float3 reflectedRay = reflect(WorldRayDirection(), faceNormal);

                ray.Origin = worldPosition + 0.01f * refractedRay;
                ray.Direction = refractedRay;
                ray.TMin = 0;
                ray.TMax = 1e20f;

                RayPayload refrRayPayload;
                refrRayPayload.color = float4(0, 0, 0, 0);
                refrRayPayload.worldPos = float4(0, 0, 0, 1);
                refrRayPayload.bounceIndex = payload.bounceIndex + 1;

                TraceRay(g_SceneAccelStruct, 0, 0xFF, 0, 1, 0, ray, refrRayPayload);

                ray.Origin = worldPosition + 0.01f * reflectedRay;
                ray.Direction = reflectedRay;
                ray.TMin = 0;
                ray.TMax = 1e20f;

                RayPayload reflRayPayload;
                reflRayPayload.rayType = payload.rayType;
                reflRayPayload.color = float4(0, 0, 0, 0);
                reflRayPayload.worldPos = float4(0, 0, 0, 1);
                reflRayPayload.bounceIndex = payload.bounceIndex + 1;

                TraceRay(g_SceneAccelStruct, 0, 0xFF, 0, 1, 0, ray, reflRayPayload);

                worldPosFinal = reflRayPayload.worldPos;
                float3 specColor = 0;
                return (lerp(refrRayPayload.color.xyz, reflRayPayload.color.xyz, kr) + specColor) * _Color.xyz;
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

                bool isFrontFace = (HitKind() == HIT_KIND_TRIANGLE_FRONT_FACE);

                float3 e0 = v1.position - v0.position;
                float3 e1 = v2.position - v0.position;

                float3 faceNormal = normalize(mul(lerp(v.normal, normalize(cross(e0, e1)), _MagicValue), (float3x3)WorldToObject()));

                faceNormal = isFrontFace ? faceNormal : -faceNormal;
                float refractiveIndex = isFrontFace ? (1.0f / _RefractiveIndex) : (_RefractiveIndex / 1.0f);

                RayDesc ray;

                if (payload.bounceIndex < 2)
                {
                    float3 worldPosFinal;
                    payload.color.xyz = TraceGlassRay(faceNormal, refractiveIndex, worldPosition, payload, ray, worldPosFinal);
                }

                
                payload.primateColor.xyz = payload.color.xyz;
                payload.primateNormal = faceNormal;
                payload.worldPos = float4(worldPosition, 1);
                payload.didHitSpecular = 0;

                payload.energy = 0;
                payload.color.xyz = _Color.xyz;
                payload.bounceRayOrigin = float4(worldPosition + K_RAY_ORIGIN_PUSH_OFF * faceNormal, 1);
                payload.bounceRayDir = faceNormal;
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

                bool isFrontFace = (HitKind() == HIT_KIND_TRIANGLE_FRONT_FACE);

                float3 e0 = v1.position - v0.position;
                float3 e1 = v2.position - v0.position;

                float3 faceNormal = normalize(mul(lerp(v.normal, normalize(cross(e0, e1)), _MagicValue), (float3x3)WorldToObject()));

                faceNormal = isFrontFace ? faceNormal : -faceNormal;

                //float refractiveIndex = isFrontFace ? (1.0f / _RefractiveIndex) : (_RefractiveIndex / 1.0f);                

                payload.energy = 0;
                payload.color.xyz = _Color.xyz;
                payload.bounceRayOrigin = float4(worldPosition + K_RAY_ORIGIN_PUSH_OFF * faceNormal, 1);
                payload.bounceRayDir = faceNormal;
                payload.bounceIndex += 1;
            }

          
            [shader("closesthit")]
            void ClosestHitMain(inout RayPayload payload : SV_RayPayload, AttributeData attribs : SV_IntersectionAttributes)
            {

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
