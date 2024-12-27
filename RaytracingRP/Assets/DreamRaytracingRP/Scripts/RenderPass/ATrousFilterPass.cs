using UnityEngine;

namespace DreamRaytracingRP.DreamRP
{
    [CreateAssetMenu(fileName = "ATrousFilterPass", menuName = "DreamVox/DreamRaytracingRP/Scriptable Objects/ATrousFilterPass")]
    public class ATrousFilterPass : DreamRenderPass
    {
        public ComputeShader atrousShader;
        public uint passes;
        public float phiNormal;
        RenderTexture temp;
        Camera camera;

        // https://research.nvidia.com/sites/default/files/pubs/2017-07_Spatiotemporal-Variance-Guided-Filtering%3A//svgf_preprint.pdf

        public override void Dispose()
        {
            if (temp != null)
            {
                temp.Release();
                temp = null;
            }
        }

        public override void Init(int rtWidth, int rtHeight)
        {
            this.CreateRT(ref temp, rtWidth, rtHeight);
        }

        public override void Render(RenderData renderData, RenderTexture dest)
        {
            if (renderData == null) return;
            if (renderData.directDiffuse == null) return;
            if (renderData.indirectDiffuse == null) return;
            if (renderData.primateNormalDepth == null) return;
            if (camera == null) camera = Camera.main;

            for (int i = 0; i < passes; i++)
            {
                int kernelStep = 1 << i;
                //kernelStep = Mathf.Min(kernelStep, 8);
                this.ClearOutRenderTexture(temp, Color.clear);
                DenoiseNoisy(ref renderData.directDiffuse, ref renderData.primateNormalDepth, ref renderData.primateSkyboxOutput, ref renderData.emissive, renderData.rtWidth, renderData.rtHeight, kernelStep);
                this.ClearOutRenderTexture(temp, Color.clear);
                DenoiseNoisy(ref renderData.indirectDiffuse, ref renderData.primateNormalDepth, ref renderData.primateSkyboxOutput, ref renderData.emissive, renderData.rtWidth, renderData.rtHeight, kernelStep);
            }
        }

        void DenoiseNoisy(ref RenderTexture rt, ref RenderTexture normalDepth, ref RenderTexture skybox, ref RenderTexture emissive, int rtW, int rtH, int kernelStep)
        {
            if (!enabled) return;
            atrousShader.SetTexture(0, "_Denoised", temp); // temp tex
            atrousShader.SetTexture(0, "_Noisy", rt);
            atrousShader.SetTexture(0, "_NormalDepth", normalDepth);
            atrousShader.SetTexture(0, "_Skybox", skybox);
            atrousShader.SetTexture(0, "_Emissive", emissive);
            atrousShader.SetInt("Step", kernelStep);
            atrousShader.SetFloat("phiNormal", phiNormal);
            var viewProjection = camera.nonJitteredProjectionMatrix * camera.transform.worldToLocalMatrix;
            atrousShader.SetMatrix("_CameraProj", viewProjection);
            atrousShader.SetVector("CamPos", camera.transform.position);
            atrousShader.SetVector("CamDir", camera.transform.forward);

            int threadGroupsX = Mathf.CeilToInt(rtW / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(rtH / 8.0f);
            atrousShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

            Graphics.Blit(temp, rt);
        }
    }
}
