using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DreamRaytracingRP.Rendering.ECS.Structs
{
    [BurstCompile]
    [StructLayout(LayoutKind.Sequential)]
    public struct TransformPosition : IComponentData
    {
        public double3 position;
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Sequential)]
    public struct TransformOrientation : IComponentData
    {
        public quaternion rotation;
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Sequential)]
    public struct TransformScale : IComponentData
    {
        public double3 scale;
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Sequential)]
    public struct Camera : IComponentData
    {
        
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Sequential)]
    public struct Cube : IComponentData
    {

    }
}
