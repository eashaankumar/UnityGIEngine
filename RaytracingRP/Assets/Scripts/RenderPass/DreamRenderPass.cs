using UnityEngine;

namespace DreamRaytracingRP.DreamRP
{
    //[CreateAssetMenu(fileName = "DreamRenderPass", menuName = "Scriptable Objects/DreamRenderPass")]
    public abstract class DreamRenderPass : ScriptableObject
    {
        public bool enabled;
        public class RenderData
        {
            public RenderTexture primateRayOutput = null,
                primateSkyboxOutput = null,
                primateNormalDepth = null, directDiffuse = null, indirectDiffuse = null, worldPosBuffer = null, motionBuffer = null,
                emissive = null;
            public int rtWidth;
            public int rtHeight;
        }
        public abstract void Init(int rtWidth, int rtHeight);
        public abstract void Render(RenderData renderData, RenderTexture dest);

        public abstract void Dispose();


        protected void CreateRT(ref RenderTexture tex, int rtWidth, int rtHeight)
        {
            if (tex)
                tex.Release();

            tex = new RenderTexture(rtWidth, rtHeight, 0, RenderTextureFormat.ARGBHalf);
            tex.enableRandomWrite = true;
            tex.Create();
        }

        protected void ClearOutRenderTexture(RenderTexture renderTexture, Color clearColor)
        {
            RenderTexture rt = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, clearColor);
            RenderTexture.active = rt;
        }

    }
}
