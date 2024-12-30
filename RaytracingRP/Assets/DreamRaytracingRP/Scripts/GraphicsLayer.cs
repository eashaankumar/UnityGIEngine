using Unity.Mathematics;
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
        [SerializeField] Vector3Int grid;

        public static GraphicsLayer Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null) Destroy(Instance);
            Instance = this;
        }

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
            var start = new double3(50, 372, 146);
            for (int x = 0; x < grid.x; x++)
            {
                for( int y = 0; y < grid.y; y++)
                {
                    for( int z = 0; z < grid.z; z++)
                    {
                        AddMesh(mesh, material, start + new int3(x, y, z) * 10, quaternion.identity, 2);

                    }
                }
            }
        }

        public void AddMesh(Mesh mesh, Material mat, double3 position, quaternion quat, double3 scale)
        {
            if (rtTest.raytracingAccelerationStructure == null) return;
            var config = new RayTracingMeshInstanceConfig(mesh, 0, mat);
            var matrix = Matrix4x4.TRS((float3)position, quat, (float3)scale);
            rtTest.raytracingAccelerationStructure.AddInstance(config, matrix);
        }
    }
}
