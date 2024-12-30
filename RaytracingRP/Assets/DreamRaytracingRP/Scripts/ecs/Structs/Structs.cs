using Unity.Entities;
using Unity.Mathematics;

namespace DreamRaytracingRP.Rendering.ECS.Structs
{
    public struct TransformPosition : IComponentData
    {
        public double3 position;
    }

    public struct TransformOrientation : IComponentData
    {
        public quaternion rotation;
    }

    public struct TransformScale : IComponentData
    {
        public double3 scale;
    }

    public struct Camera : IComponentData
    {
        
    }

    public struct Cube : IComponentData
    {

    }
}
