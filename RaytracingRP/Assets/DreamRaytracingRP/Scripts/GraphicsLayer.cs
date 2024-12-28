using UnityEngine;
using UnityEngine.Rendering;

namespace DreamRaytracingRP.Rendering
{
    [ExecuteInEditMode]
    public class GraphicsLayer : MonoBehaviour
    {
        [SerializeField] RayTracingTest rtTest;
        [SerializeField] Mesh mesh;
        [SerializeField] Material material;

        private void Update()
        {
            /// Clear RTAS
            rtTest.RebuildRTAS();

            /// Add all instances
            AddAllInstancesToRTAS();

            /// Build RTAS
            rtTest.raytracingAccelerationStructure.Build();
        }

        void AddAllInstancesToRTAS()
        {
            AddInstance();
        }

        public void AddInstance()
        {
            if (rtTest.raytracingAccelerationStructure == null) return;
            var config = new RayTracingMeshInstanceConfig(mesh, 0, material);
            var matrix = Matrix4x4.TRS(transform.position + transform.forward * 3, transform.rotation, Vector3.one * 2);
            rtTest.raytracingAccelerationStructure.AddInstance(config, matrix);
        }
    }
}
