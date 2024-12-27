using DreamRaytracingRP.DreamRP;
using UnityEngine;

namespace DreamRaytracingRP.DreamRP
{
    [CreateAssetMenu(fileName = "CustomDenoiserRP", menuName = "DreamVox/DreamRaytracingRP/Scriptable Objects/CustomDenoiserRP")]
    public class CustomDenoiserRP : DreamRenderPass
    {
        public ComputeShader customDenoiser;
        [SerializeField] float depthFallOff;
        [SerializeField] float normalFallOff;
        [SerializeField] int blurPasses;

        RenderTexture temp;

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

            for (int i = 0; i < blurPasses; i++)
            {
                int kernelSize = 1 << i;
                this.ClearOutRenderTexture(temp, Color.clear);
                DenoiseNoisy(ref renderData.directDiffuse, ref renderData.primateNormalDepth, renderData.rtWidth, renderData.rtHeight, kernelSize);
                this.ClearOutRenderTexture(temp, Color.clear);
                DenoiseNoisy(ref renderData.indirectDiffuse, ref renderData.primateNormalDepth, renderData.rtWidth, renderData.rtHeight, kernelSize);
            }
        }

        void DenoiseNoisy(ref RenderTexture rt, ref RenderTexture normalDepth, int rtW, int rtH, int kernelSize)
        {
            if (!enabled) return;
            customDenoiser.SetTexture(0, "_Denoised", temp); // temp tex
            customDenoiser.SetTexture(0, "_Noisy", rt);
            customDenoiser.SetTexture(0, "_NormalDepth", normalDepth);
            customDenoiser.SetFloat("_DepthFalloff", depthFallOff);
            customDenoiser.SetInt("_DepthBlurKernelSize", kernelSize);
            customDenoiser.SetFloat("_NormalFalloff", normalFallOff);


            int threadGroupsX = Mathf.CeilToInt(rtW / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(rtH / 8.0f);
            customDenoiser.Dispatch(0, threadGroupsX, threadGroupsY, 1);

            Graphics.Blit(temp, rt);
        }
    }
}
