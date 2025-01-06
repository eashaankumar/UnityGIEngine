using DreamRaytracingRP.Physics.ECS.Structs;
using DreamRaytracingRP.Rendering.ECS.Structs;
using DreamRaytracingRP.Rendering.Layers;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DreamRaytracingRP.Rendering.Tests
{
    public class PhysicsRendering : MonoBehaviour
    {
        [SerializeField] Mesh mesh;
        [SerializeField] Material material;

        EntityQuery query;

        public void OnCameraCreated()
        {

        }

        private void Awake()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TransformPosition>(),
                                                        ComponentType.ReadOnly<PBDParticle>(),
                                                        ComponentType.ReadOnly<Velocity>());
        }

        private void Update()
        {
            AddAllInstancesToRTAS();
        }

        void AddAllInstancesToRTAS()
        {
            

            var positions = query.ToComponentDataArray<TransformPosition>(Allocator.TempJob);
            var particles = query.ToComponentDataArray<PBDParticle>(Allocator.TempJob);
            var velocity = query.ToComponentDataArray<Velocity>(Allocator.TempJob);

            for (int i = 0; i < positions.Length; i++)
            {
                GraphicsLayer.Instance.AddMesh(mesh, material, positions[i].position, quaternion.identity, particles[i].radius);
            }

            positions.Dispose();
            particles.Dispose();
            velocity.Dispose();
        }
    }
}
