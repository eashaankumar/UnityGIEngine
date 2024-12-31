using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DreamRaytracingRP.Physics.ECS.Structs
{
    [BurstCompile]
    [StructLayout(LayoutKind.Sequential)]
    public struct Intertia : IComponentData
    {
        private double3x3 t;
        private double3x3 invTensor;

        public Intertia(double3x3 _t)
        {
            t = _t;
            invTensor = math.inverse(t);
        }

        public double3x3 T => t;
        public double3x3 InvT => invTensor;
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Sequential)]
    public struct Velocity : IComponentData
    {
        public double3 v;
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Sequential)]
    public struct ForceAccumulator : IComponentData
    {
        public double3 v;
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Sequential)]
    public struct AngularVelocity : IComponentData
    {
        public double3 v;
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Sequential)]
    public struct TorqueAccumulator : IComponentData
    {
        public double3 v;
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Sequential)]
    public struct Mass : IComponentData
    {
        private double m;
        private double invM;

        public Mass(double _m)
        {
            m = _m;
            invM = 1.0/m;
        }

        public double M => m;
        public double InvM => invM;
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Sequential)]
    public struct VerletParticle : IComponentData
    {
        
    }
}
