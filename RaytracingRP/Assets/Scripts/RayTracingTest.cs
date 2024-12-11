﻿using UnityEngine;
using DreamRaytracingRP.DreamRP;

namespace DreamRaytracingRP
{
    [ExecuteInEditMode]
    public class RayTracingTest : MonoBehaviour
    {
        public UnityEngine.Rendering.RayTracingShader rayTracingShader = null;
        public DreamRenderPass[] renderPasses;
        public Cubemap envMap = null;

        private int cameraWidth = 0;
        private int cameraHeight = 0;

        private RenderTexture primateRayOutput = null, primateSkyboxOutput = null, primateNormalDepth = null, directDiffuse = null, indirectDiffuse = null, worldPosBuffer = null, motionBuffer = null;

        private UnityEngine.Rendering.RayTracingAccelerationStructure raytracingAccelerationStructure = null;
        private DreamRenderPass.RenderData renderData;
        private void BuildRaytracingAccelerationStructure()
        {
            if (raytracingAccelerationStructure == null)
            {
                UnityEngine.Rendering.RayTracingAccelerationStructure.Settings settings = new UnityEngine.Rendering.RayTracingAccelerationStructure.Settings();
                settings.rayTracingModeMask = UnityEngine.Rendering.RayTracingAccelerationStructure.RayTracingModeMask.Everything;
                settings.managementMode = UnityEngine.Rendering.RayTracingAccelerationStructure.ManagementMode.Automatic;
                settings.layerMask = 255;

                raytracingAccelerationStructure = new UnityEngine.Rendering.RayTracingAccelerationStructure(settings);

                raytracingAccelerationStructure.Build();
            }
        }

        private void ReleaseResources()
        {
            if (raytracingAccelerationStructure != null)
            {
                raytracingAccelerationStructure.Release();
                raytracingAccelerationStructure = null;
            }

            if (primateRayOutput)
            {
                primateRayOutput.Release();
                primateRayOutput = null;

                primateNormalDepth.Release();
                primateNormalDepth = null;

                directDiffuse.Release();
                directDiffuse = null;

                indirectDiffuse.Release();
                indirectDiffuse = null;

                primateSkyboxOutput.Release();
                primateSkyboxOutput = null;

                worldPosBuffer.Release();
                worldPosBuffer = null;

                motionBuffer.Release();
                motionBuffer = null;
            }

            foreach (var rp in renderPasses) rp.Dispose();

            cameraWidth = 0;
            cameraHeight = 0;
        }

        private void CreateResources()
        {
            BuildRaytracingAccelerationStructure();

            renderData = new DreamRenderPass.RenderData
            {
                primateRayOutput = primateRayOutput,
                primateSkyboxOutput = primateSkyboxOutput,
                primateNormalDepth = primateNormalDepth,
                directDiffuse = directDiffuse,
                indirectDiffuse = indirectDiffuse,
                worldPosBuffer = worldPosBuffer,
                motionBuffer = motionBuffer,
                rtWidth = Camera.main.pixelWidth,
                rtHeight = Camera.main.pixelHeight
            };


            if (cameraWidth != renderData.rtWidth || cameraHeight != renderData.rtHeight)
            {
                CreateRT(ref primateRayOutput);
                CreateRT(ref primateNormalDepth);
                CreateRT(ref directDiffuse);
                CreateRT(ref indirectDiffuse);
                CreateRT(ref primateSkyboxOutput);
                CreateRT(ref worldPosBuffer);
                CreateRT(ref motionBuffer);

                cameraWidth = Camera.main.pixelWidth;
                cameraHeight = Camera.main.pixelHeight;

                foreach (var rp in renderPasses) rp.Init(renderData.rtWidth, renderData.rtHeight);

            }



        }

        void CreateRT(ref RenderTexture tex)
        {
            if (tex)
                tex.Release();

            tex = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, 0, RenderTextureFormat.ARGBHalf);
            tex.enableRandomWrite = true;
            tex.Create();
        }

        void OnDisable()
        {
            ReleaseResources();
        }

        private void Update()
        {
            CreateResources();
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (!SystemInfo.supportsRayTracing || !rayTracingShader)
            {
                Debug.Log("The RayTracing API is not supported by this GPU or by the current graphics API.");
                Graphics.Blit(src, dest);
                return;
            }

            if (raytracingAccelerationStructure == null)
                return;

            // Use Shader Pass "Test" in surface (material) shaders."
            rayTracingShader.SetShaderPass("Test");

            Shader.SetGlobalMatrix(Shader.PropertyToID("g_InvViewMatrix"), Camera.main.cameraToWorldMatrix);
            Shader.SetGlobalTexture(Shader.PropertyToID("g_EnvTex"), envMap);

            raytracingAccelerationStructure.Build();

            // Input
            rayTracingShader.SetAccelerationStructure(Shader.PropertyToID("g_SceneAccelStruct"), raytracingAccelerationStructure);
            rayTracingShader.SetMatrix(Shader.PropertyToID("g_InvViewMatrix"), Camera.main.cameraToWorldMatrix);
            rayTracingShader.SetFloat(Shader.PropertyToID("g_Zoom"), Mathf.Tan(Mathf.Deg2Rad * Camera.main.fieldOfView * 0.5f));
            rayTracingShader.SetFloat("g_dt", Time.deltaTime);

            // Output
            rayTracingShader.SetTexture("g_PrimateRayOutput", primateRayOutput);
            rayTracingShader.SetTexture("g_PrimateNormalDepth", primateNormalDepth);
            rayTracingShader.SetTexture("g_DirectDiffuse", directDiffuse);
            rayTracingShader.SetTexture("g_IndirectDiffuse", indirectDiffuse);
            rayTracingShader.SetTexture("g_PrimateSkyboxOutput", primateSkyboxOutput);
            rayTracingShader.SetTexture("g_WorldPosBuffer", worldPosBuffer);
            rayTracingShader.SetTexture("g_MotionBuffer", motionBuffer);

            rayTracingShader.Dispatch("MainRayGenShader", cameraWidth, cameraHeight, 1);

            //Graphics.Blit(primateRayOutput, dest);
            foreach (var rp in renderPasses)
            {
                rp.Render(renderData, dest);
            }
        }
    }
}