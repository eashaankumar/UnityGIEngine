using Unity.Entities;
using Unity.Mathematics;

namespace DreamRaytracingRP.Rendering.ECS.Structs
{
    public struct Transform : IComponentData
    {
        public double3 position;
        public quaternion rotation;
        public double3 scale;
    }

    public struct Camera : IComponentData
    {
        
    }
}
