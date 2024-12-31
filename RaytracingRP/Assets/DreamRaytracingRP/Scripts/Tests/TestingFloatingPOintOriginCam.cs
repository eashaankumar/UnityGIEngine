using DreamRaytracingRP.Rendering.Layers;
using Unity.Mathematics;
using UnityEngine;

namespace DreamRaytracingRP.Rendering.Tests
{
    public class TestingFloatingPOintOriginCam : MonoBehaviour
    {
        [SerializeField] Mesh mesh;
        [SerializeField] Material material;
        [SerializeField] Vector3Int grid;
        [SerializeField] double startingPos;

        public void OnCameraCreated()
        {
            GraphicsLayer.Instance.UpdateCameraFloatingOrigin(new double3(startingPos, 0, 0), quaternion.identity);
        }

        private void Update()
        {
            AddAllInstancesToRTAS();
        }

        void AddAllInstancesToRTAS()
        {
            var start = new double3(startingPos, 0, 0);
            for (int x = 0; x < grid.x; x++)
            {
                for (int y = 0; y < grid.y; y++)
                {
                    for (int z = 0; z < grid.z; z++)
                    {
                        GraphicsLayer.Instance.AddMesh(mesh, material, start + new int3(x, y, z) * 10, quaternion.identity, 2);

                    }
                }
            }
        }

    }
}
