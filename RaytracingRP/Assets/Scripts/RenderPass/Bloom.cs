using UnityEngine;
namespace DreamRaytracingRP.DreamRP
{
    [CreateAssetMenu(fileName = "Bloom", menuName = "DreamVox/DreamRaytracingRP/Scriptable Objects/Bloom")]
    public class Bloom : DreamRenderPass
    {
        public ComputeShader gaussianBlur;
        public Material mergeMaterial;
        [SerializeField] uint gaussianBlurKernel;
        [SerializeField] uint mips;

        [SerializeField]  RenderTexture mipmap;

        public override void Dispose()
        {
            if (mipmap != null)
            {
                mipmap.Release();
                mipmap = null;
            }
        }

        public override void Init(int rtWidth, int rtHeight)
        {
            CreateMipMapRT(ref mipmap, rtWidth, rtHeight);
        }

        protected void CreateMipMapRT(ref RenderTexture tex, int rtWidth, int rtHeight)
        {
            if (tex)
                tex.Release();

            tex = new RenderTexture(rtWidth, rtHeight, 0, RenderTextureFormat.ARGBHalf, (int)mips);
            tex.enableRandomWrite = true;
            tex.useMipMap = true;
            tex.autoGenerateMips = false;
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
            Graphics.Blit(dest, mipmap);
            // create mip map of bloom tex
            mipmap.GenerateMips();
            // Apply heavy blur on each mip map
            for(int i = 0; i < mips; i++)
            {
                gaussianBlur.SetTexture(0, "Result", mipmap, i);
                gaussianBlur.SetInt("KernelSize", (int)gaussianBlurKernel);

                int threadGroupsX = Mathf.CeilToInt(renderData.rtWidth / 8.0f);
                int threadGroupsY = Mathf.CeilToInt(renderData.rtHeight / 8.0f);
                gaussianBlur.Dispatch(0, threadGroupsX, threadGroupsY, 1);
            }
            // blit bloom to dest using merger Material

            mergeMaterial.SetTexture("_MainTex", mipmap);
            mergeMaterial.SetInt("_MipCount", (int)mips);
            Graphics.Blit(mipmap, dest, mergeMaterial);
        }
    }
}
