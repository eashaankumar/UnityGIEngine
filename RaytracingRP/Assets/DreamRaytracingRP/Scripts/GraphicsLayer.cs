using DreamRaytracingRP.Rendering.ECS.Structs;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace DreamRaytracingRP.Rendering.Layers
{
    [ExecuteInEditMode]
    public class GraphicsLayer : MonoBehaviour
    {
        [SerializeField] RayTracingTest rtTest;
        [SerializeField] Mesh mesh;
        [SerializeField] Material material;
        [SerializeField] Vector3Int grid;

        public static GraphicsLayer Instance { get; private set; }

        public static EntityManager EntityManager;
        public static Entity CameraEntity;

        double3 currentCameraFloatingOriginCache;

        private void Awake()
        {
            if (Instance != null) Destroy(Instance);
            Instance = this;

            EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            CameraEntity = EntityManager.CreateEntity();

            EntityManager.AddComponentData(CameraEntity, new TransformPosition
            {
                position = (float3)UnityEngine.Camera.main.transform.position
            });

            EntityManager.AddComponentData(CameraEntity, new TransformOrientation
            {
                rotation = UnityEngine.Camera.main.transform.rotation
            });

            EntityManager.AddComponentData(CameraEntity, new ECS.Structs.Camera
            {
                
            });

            //UnityEngine.Camera.main.transform.rotation = rot;
            UnityEngine.Camera.main.transform.position = Vector3.zero;
        }

        private void Update()
        {
            GetCameraFloatingOrigin(out currentCameraFloatingOriginCache, out var rot);

            UnityEngine.Camera.main.transform.rotation = rot;
            UnityEngine.Camera.main.transform.position = Vector3.zero;

            Debug.Log($"Floating Origin: {currentCameraFloatingOriginCache} {math.Euler(rot)}");
            
            /// Clear RTAS
            rtTest.RebuildRTAS();

            /// Add all instances
            AddAllInstancesToRTAS();

            /// Build RTAS
            rtTest.raytracingAccelerationStructure.Build();
        }

        public void UpdateCameraFloatingOrigin(double3 position, quaternion rotation)
        {
            EntityManager.SetComponentData<TransformPosition>(CameraEntity, new TransformPosition { position = position });
            EntityManager.SetComponentData<TransformOrientation>(CameraEntity, new TransformOrientation { rotation = rotation });
        }

        public void GetCameraFloatingOrigin(out double3 pos, out quaternion rotation)
        {
            pos = EntityManager.GetComponentData<TransformPosition>(CameraEntity).position;
            rotation = EntityManager.GetComponentData<TransformOrientation>(CameraEntity).rotation;
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

            position = position - currentCameraFloatingOriginCache;

            var config = new RayTracingMeshInstanceConfig(mesh, 0, mat);
            var matrix = Matrix4x4.TRS((float3)position, quat, (float3)scale);
            rtTest.raytracingAccelerationStructure.AddInstance(config, matrix);
        }
    }
}
