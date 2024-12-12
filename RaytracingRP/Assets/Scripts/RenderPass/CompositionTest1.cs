using UnityEngine;

namespace DreamRaytracingRP
{
    [CreateAssetMenu(fileName = "CompositionTest1", menuName = "DreamVox/DreamRaytracingRP/Scriptable Objects/CompositionTest1")]
    public class CompositionTest1 : DreamRP.DreamRenderPass
    {
        public ComputeShader composeDiffuse;
        public override void Dispose()
        {

        }

        public override void Init(int rtWidth, int rtHeight)
        {
        }


        public override void Render(RenderData renderData, RenderTexture dest)
        {
            if (renderData == null) return;
            if (renderData.primateRayOutput == null) return;
            if (renderData.directDiffuse == null) return;
            if (renderData.indirectDiffuse == null) return;
            if (renderData.primateSkyboxOutput == null) return;
            composeDiffuse.SetTexture(0, "g_Result", dest);
            composeDiffuse.SetTexture(0, "g_Albedo", renderData.primateRayOutput);
            composeDiffuse.SetTexture(0, "g_DirectDiffuse", renderData.directDiffuse);
            composeDiffuse.SetTexture(0, "g_IndirectDiffuse", renderData.indirectDiffuse);
            composeDiffuse.SetTexture(0, "g_PrimateSkyboxOutput", renderData.primateSkyboxOutput);

            int threadGroupsX = Mathf.CeilToInt(renderData.rtWidth / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(renderData.rtHeight / 8.0f);
            composeDiffuse.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        }
    }
}
