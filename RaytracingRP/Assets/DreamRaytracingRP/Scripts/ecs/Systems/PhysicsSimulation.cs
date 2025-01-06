using DreamRaytracingRP.Physics.ECS.Structs;
using DreamRaytracingRP.Rendering.ECS.Structs;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace DreamRaytracingRP.Physics.ECS.Systems
{
    [BurstCompile]
    [StructLayout(LayoutKind.Sequential)]
    partial struct PhysicsSimulation : ISystem
    {
        double lastPhysicsUpdate;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            int dim = 10;
            for(int x = -dim/2; x < dim/2; x++)
            {
                for (int y = -dim / 2; y < dim / 2; y++)
                {
                    CreateParticle(x, y, ref state);
                }
            }
        }

        Entity CreateParticle(int x, int y, ref SystemState state)
        {
            
            Entity entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new TransformPosition
            {
                position = new double3(1000000 + x * 3, 0, y * 3)
            });

            state.EntityManager.AddComponentData(entity, new Velocity
            {
                v = new double3(0, 0, 0)
            });

            state.EntityManager.AddComponentData(entity, new Mass(1.0));

            state.EntityManager.AddComponentData(entity, new ForceAccumulator
            {
                v = 0.0
            });

            state.EntityManager.AddComponentData(entity, new PBDParticle
            {
                radius = 1
            });
            return entity;
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var currTime = SystemAPI.Time.ElapsedTime;
            var dt = currTime - lastPhysicsUpdate;

            if (dt < 1e-5) return;

            new PBDGravityJob {  }.ScheduleParallel();

            new PBDIntegrateJob { dt = dt }.ScheduleParallel();

            // solve constraints
            // 1. Collisions
            // 2. Joints

            new PBDVelocityUpdateJob { dt = dt }.ScheduleParallel();

            lastPhysicsUpdate = currTime;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        
        }

        [BurstCompile]
        [StructLayout(LayoutKind.Sequential)]
        partial struct PBDGravityJob : IJobEntity
        {
            public void Execute( ref ForceAccumulator forceAcc, ref Mass mass)
            {
                double g = mass.M * -9.81;
                forceAcc.AddForce(new double3(0, g, 0));
            }
        }

        [BurstCompile]
        [StructLayout(LayoutKind.Sequential)]
        partial struct PBDIntegrateJob : IJobEntity
        {
            public double dt;
            public void Execute(ref PBDParticle p, ref TransformPosition pos, ref Velocity vel, ref Mass mass, ref ForceAccumulator forceAcc)
            {
                var a = forceAcc.v * mass.InvM;
                vel.v += dt * a;
                p.previousPos = pos.position;
                pos.position += dt * vel.v;

                forceAcc.ClearAccumulator();
            }
        }

        [BurstCompile]
        [StructLayout(LayoutKind.Sequential)]
        partial struct PBDVelocityUpdateJob : IJobEntity
        {
            public double dt;
            public void Execute(ref PBDParticle p, ref TransformPosition pos, ref Velocity vel, ref Mass mass, ref ForceAccumulator forceAcc)
            {
                vel.v = (pos.position - p.previousPos) / dt;
            }
        }
    }
}
