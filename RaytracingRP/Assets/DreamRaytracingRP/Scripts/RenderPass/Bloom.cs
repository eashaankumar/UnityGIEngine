using UnityEngine;
namespace DreamRaytracingRP.DreamRP
{
    [CreateAssetMenu(fileName = "Bloom", menuName = "DreamVox/DreamRaytracingRP/Scriptable Objects/Bloom")]
    public class Bloom : DreamRenderPass
    {
        public ComputeShader gaussianBlur;
        public ComputeShader bloom;
        public Material mergeMaterial;
        [SerializeField] uint gaussianBlurKernel;
        [SerializeField] uint mips;
        [SerializeField, Min(0)] float threshold;
        [SerializeField, Min(0)] float bloomIntensity;

        [SerializeField]  RenderTexture mipmap;
        [SerializeField] RenderTexture mask;
        [SerializeField] RenderTexture bloomed;

        public override void Dispose()
        {
            if (mipmap != null)
            {
                mipmap.Release();
                mipmap = null;
            }
            if (mask != null)
            {
                mask.Release();
                mask = null;
            }
            if (bloomed != null)
            {
                bloomed.Release();
                bloomed = null;
            }
        }

        public override void Init(int rtWidth, int rtHeight)
        {
            CreateMipMapRT(ref mipmap, rtWidth, rtHeight);
            this.CreateRT(ref mask, rtWidth, rtHeight);
            this.CreateRT(ref bloomed, rtWidth, rtHeight);    
        }

        protected void CreateMipMapRT(ref RenderTexture tex, int rtWidth, int rtHeight)
        {
            if (tex)
                tex.Release();

            tex = new RenderTexture(rtWidth, rtHeight, 0, RenderTextureFormat.ARGBHalf, (int)mips);
            tex.enableRandomWrite = true;
            tex.useMipMap = true;
            tex.autoGenerateMips = false;
            tex.filterMode = FilterMode.Bilinear;
            tex.Create();
        }

        void UpdateMipMap(int rtWidth, int rtHeight)
        {
            if (mipmap == null || mipmap.width != rtWidth || mipmap.height != rtHeight || !mipmap.useMipMap || mipmap.mipmapCount != mips)
            {
                Dispose();
                Init(rtWidth, rtHeight);
            }
        }

        public override void Render(RenderData renderData, RenderTexture dest)
        {
            if (renderData == null) return;
            if (dest == null) return;
            if (!enabled) return;
            UpdateMipMap(renderData.rtWidth, renderData.rtHeight);
            // Blit dest to bloom tex
            Graphics.Blit(dest, mask);

            // Threshold bloom tex
            bloom.SetTexture(0, "Result", mask);
            bloom.SetFloat("Threshold", threshold);
            int threadGroupsX = Mathf.CeilToInt(renderData.rtWidth / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(renderData.rtHeight / 8.0f);
            bloom.Dispatch(0, threadGroupsX, threadGroupsY, 1);

            Graphics.Blit(mask, mipmap);

            // create mip map of bloom tex
            mipmap.GenerateMips();
            // Apply heavy blur on each mip map
            for(int i = 0; i < mips; i++)
            {
                gaussianBlur.SetTexture(0, "Result", mipmap, i);
                gaussianBlur.SetInt("KernelSize", (int)gaussianBlurKernel);

                threadGroupsX = Mathf.CeilToInt(renderData.rtWidth / 8.0f);
                threadGroupsY = Mathf.CeilToInt(renderData.rtHeight / 8.0f);
                gaussianBlur.Dispatch(0, threadGroupsX, threadGroupsY, 1);
            }
            // blit bloom to dest using merger Material
            mergeMaterial.SetTexture("_MainTex", mipmap);
            mergeMaterial.SetInt("_MipCount", (int)mips);
            mergeMaterial.SetFloat("_MipBlendPower", bloomIntensity);
            Graphics.Blit(mipmap, bloomed, mergeMaterial);

            // Combine bloom with everything else
            bloom.SetTexture(1, "Result", dest);
            bloom.SetTexture(1, "Bloomed", bloomed);
            bloom.SetTexture(1, "Mask", mask);
            bloom.SetFloat("Threshold", threshold);
            threadGroupsX = Mathf.CeilToInt(renderData.rtWidth / 8.0f);
            threadGroupsY = Mathf.CeilToInt(renderData.rtHeight / 8.0f);
            bloom.Dispatch(1, threadGroupsX, threadGroupsY, 1);
        }
    }
}
