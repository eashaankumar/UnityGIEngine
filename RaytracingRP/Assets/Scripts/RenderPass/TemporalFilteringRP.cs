using UnityEngine;

namespace DreamRaytracingRP.DreamRP
{
    [CreateAssetMenu(fileName = "TemporalFilteringRP", menuName = "DreamVox/DreamRaytracingRP/Scriptable Objects/TemporalFilteringRP")]
    public class TemporalFilteringRP : DreamRenderPass
    {
        [SerializeField] ComputeShader m_temporalFilterCS;
        Camera camera;
        RenderTexture directDiffuseAcc, historyBuffer;
        public override void Dispose()
        {
            if (directDiffuseAcc != null)
            {
                directDiffuseAcc.Release();
                directDiffuseAcc = null;

                historyBuffer.Release();
                historyBuffer = null;
            }
        }

        public override void Init(int rtWidth, int rtHeight)
        {
            this.CreateRT(ref directDiffuseAcc, rtWidth, rtHeight);
            CreateHistoryBuffer(ref historyBuffer, rtWidth, rtHeight);
        }

        protected void CreateHistoryBuffer(ref RenderTexture tex, int rtWidth, int rtHeight)
        {
            if (tex)
                tex.Release();

            tex = new RenderTexture(rtWidth, rtHeight, 0, RenderTextureFormat.RHalf);
            tex.enableRandomWrite = true;
            tex.Create();
        }

        public override void Render(RenderData renderData, RenderTexture dest)
        {
            if (renderData == null) return;
            if (dest == null) return;
            if (!enabled) return;
            if (camera == null) camera = Camera.main;
            if (camera == null) return;
            m_temporalFilterCS.SetTexture(0, "Current", dest);
            m_temporalFilterCS.SetTexture(0, "Accumulated", directDiffuseAcc);
            m_temporalFilterCS.SetTexture(0, "MotionVectors", renderData.motionBuffer);
            m_temporalFilterCS.SetTexture(0, "WorldPos", renderData.worldPosBuffer);
            m_temporalFilterCS.SetTexture(0, "HistoryBuffer", historyBuffer);
            var viewProjection = camera.nonJitteredProjectionMatrix * camera.transform.worldToLocalMatrix;
            m_temporalFilterCS.SetMatrix("_CameraProj", viewProjection);
            m_temporalFilterCS.SetInt("_RTWidth", directDiffuseAcc.width);
            m_temporalFilterCS.SetInt("_RTHeight", directDiffuseAcc.height);

            //Camera.main.WorldToScreenPoint()

            int threadGroupsX = Mathf.CeilToInt(renderData.rtWidth / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(renderData.rtHeight / 8.0f);
            m_temporalFilterCS.Dispatch(0, threadGroupsX, threadGroupsY, 1);

            // debug
            //Graphics.Blit(renderData.directDiffuse, dest);
        }
    }
}
