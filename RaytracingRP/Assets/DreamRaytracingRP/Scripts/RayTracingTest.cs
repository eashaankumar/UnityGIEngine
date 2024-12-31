using UnityEngine;
using DreamRaytracingRP.DreamRP;
using DreamRaytracingRP.Rendering.Layers;

namespace DreamRaytracingRP.Rendering
{
    [ExecuteInEditMode]
    public class RayTracingTest : MonoBehaviour
    {
        public ProceduralSkybox procSkybox;
        public UnityEngine.Rendering.RayTracingShader rayTracingShader = null;
        public DreamRenderPass[] renderPasses;
        public Cubemap envMap = null;
        public GraphicsLayer graphicsLayer;

        private int cameraWidth = 0;
        private int cameraHeight = 0;

        [System.Serializable]
        public struct ProceduralSkybox
        {
            public Transform sunDir;
            public float sunIntensity;
            public float sunFocus;

            [SerializeField, ColorUsageAttribute(showAlpha: false, hdr: true)]
            public Color horizonColor;

            [SerializeField, ColorUsageAttribute(showAlpha: false, hdr: true)]
            public Color zenithColor;

            [SerializeField, ColorUsageAttribute(showAlpha: false, hdr: true)]
            public Color groundColor;
        }

        private RenderTexture primateRayOutput = null, primateSkyboxOutput = null, primateNormalDepth = null, 
            directDiffuse = null, indirectDiffuse = null, worldPosBuffer = null, motionBuffer = null,
            emissive = null;
        private RenderTexture result = null;

        public UnityEngine.Rendering.RayTracingAccelerationStructure raytracingAccelerationStructure = null;
        private DreamRenderPass.RenderData renderData;
        private void BuildRaytracingAccelerationStructure()
        {
            if (raytracingAccelerationStructure == null)
            {
                UnityEngine.Rendering.RayTracingAccelerationStructure.Settings settings = new UnityEngine.Rendering.RayTracingAccelerationStructure.Settings();
                settings.rayTracingModeMask = UnityEngine.Rendering.RayTracingAccelerationStructure.RayTracingModeMask.Everything;
                settings.managementMode = UnityEngine.Rendering.RayTracingAccelerationStructure.ManagementMode.Manual;
                settings.layerMask = 255;

                raytracingAccelerationStructure = new UnityEngine.Rendering.RayTracingAccelerationStructure(settings);

                raytracingAccelerationStructure.Build();
            }
        }

        public void RebuildRTAS()
        {
            if (raytracingAccelerationStructure != null) raytracingAccelerationStructure.Dispose();
            raytracingAccelerationStructure=null;
            CreateResources();
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

                result.Release();
                result = null;

                emissive.Release();
                emissive = null;

            }

            foreach (var rp in renderPasses) rp.Dispose();

            cameraWidth = 0;
            cameraHeight = 0;
        }

        private void CreateResources()
        {
            BuildRaytracingAccelerationStructure();



            if (cameraWidth != Camera.main.pixelWidth || cameraHeight != Camera.main.pixelHeight)
            {
                CreateRT(ref primateRayOutput);
                CreateRT(ref primateNormalDepth);
                CreateRT(ref directDiffuse);
                CreateRT(ref indirectDiffuse);
                CreateRT(ref primateSkyboxOutput);
                CreateRT(ref worldPosBuffer);
                CreateRT(ref motionBuffer);
                CreateRT(ref result);
                CreateRT(ref emissive);

                cameraWidth = Camera.main.pixelWidth;
                cameraHeight = Camera.main.pixelHeight;

                renderData = new DreamRenderPass.RenderData
                {
                    primateRayOutput = primateRayOutput,
                    primateSkyboxOutput = primateSkyboxOutput,
                    primateNormalDepth = primateNormalDepth,
                    directDiffuse = directDiffuse,
                    indirectDiffuse = indirectDiffuse,
                    worldPosBuffer = worldPosBuffer,
                    motionBuffer = motionBuffer,
                    emissive = emissive,
                    rtWidth = Camera.main.pixelWidth,
                    rtHeight = Camera.main.pixelHeight
                };

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

        /*private void Update()
        {
            CreateResources();
        }*/

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


            graphicsLayer.GetCameraFloatingOrigin(out var camPos, out var camRot);

            Matrix4x4 cameraFloatingOrigin = Camera.main.cameraToWorldMatrix;

            /*Matrix4x4 cameraFloatingOrigin = Matrix4x4.TRS(Vector3.zero, camRot, Vector3.one);
            cameraFloatingOrigin = Matrix4x4.Inverse(cameraFloatingOrigin);
            // https://docs.unity3d.com/ScriptReference/Camera-worldToCameraMatrix.html
            cameraFloatingOrigin.m20 *= -1f;
            cameraFloatingOrigin.m21 *= -1f;
            cameraFloatingOrigin.m22 *= -1f;
            cameraFloatingOrigin.m23 *= -1f;*/

            Shader.SetGlobalMatrix(Shader.PropertyToID("g_InvViewMatrix"), cameraFloatingOrigin);
            Shader.SetGlobalTexture(Shader.PropertyToID("g_EnvTex"), envMap);

            raytracingAccelerationStructure.Build();

            // Input
            rayTracingShader.SetAccelerationStructure(Shader.PropertyToID("g_SceneAccelStruct"), raytracingAccelerationStructure);
            rayTracingShader.SetMatrix(Shader.PropertyToID("g_InvViewMatrix"), cameraFloatingOrigin);
            rayTracingShader.SetFloat(Shader.PropertyToID("g_Zoom"), Mathf.Tan(Mathf.Deg2Rad * Camera.main.fieldOfView * 0.5f));
            rayTracingShader.SetFloat("g_dt", Time.deltaTime);
            rayTracingShader.SetFloat("g_SunIntensity", procSkybox.sunIntensity);
            rayTracingShader.SetFloat("g_SunFocus", procSkybox.sunFocus);
            rayTracingShader.SetVector("g_SunDir", procSkybox.sunDir.transform.forward);
            rayTracingShader.SetVector("g_SkyColorHorizon", procSkybox.horizonColor);
            rayTracingShader.SetVector("g_SkyColorZenith", procSkybox.zenithColor);
            rayTracingShader.SetVector("g_GroundColor", procSkybox.groundColor);

            //rayTracingShader.SetInt("g_seed", (int)UnityEngine.Random.Range(0, uint.MaxValue));

            // Output
            rayTracingShader.SetTexture("g_PrimateRayOutput", primateRayOutput);
            rayTracingShader.SetTexture("g_PrimateNormalDepth", primateNormalDepth);
            rayTracingShader.SetTexture("g_DirectDiffuse", directDiffuse);
            rayTracingShader.SetTexture("g_IndirectDiffuse", indirectDiffuse);
            rayTracingShader.SetTexture("g_PrimateSkyboxOutput", primateSkyboxOutput);
            rayTracingShader.SetTexture("g_WorldPosBuffer", worldPosBuffer);
            rayTracingShader.SetTexture("g_MotionBuffer", motionBuffer);
            rayTracingShader.SetTexture("g_Emissive", emissive);

            rayTracingShader.Dispatch("MainRayGenShader", cameraWidth, cameraHeight, 1);

            foreach (var rp in renderPasses)
            {
                rp.Render(renderData, result);
            }

            Graphics.Blit(result, dest);
        }
    }
}