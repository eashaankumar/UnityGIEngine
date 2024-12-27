using UnityEngine;

namespace DreamRaytracingRP.DreamRP
{
    [CreateAssetMenu(fileName = "AcesRP", menuName = "DreamVox/DreamRaytracingRP/Scriptable Objects/AcesRP")]
    public class AcesRP : DreamRP.DreamRenderPass
    {
        public ComputeShader aces;
        public override void Dispose()
        {
        }

        public override void Init(int rtWidth, int rtHeight)
        {
        }

        public override void Render(RenderData renderData, RenderTexture dest)
        {
            if (renderData == null) return;
            if (dest == null) return;
            if (!enabled) return;
            aces.SetTexture(0, "Result", dest);

            int threadGroupsX = Mathf.CeilToInt(renderData.rtWidth / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(renderData.rtHeight / 8.0f);
            aces.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        }
    }
}
