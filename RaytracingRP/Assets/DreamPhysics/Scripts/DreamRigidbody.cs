using System;
using Unity.Mathematics;
using UnityEngine;

namespace DreamPhysics
{
    public class DreamRigidbody
    {
        float mass, invMass;
        public float3x3 I;
        private float3x3 I_inv;
        public bool isStatic;
        
        public float3 velocity; 
        public float3 local_angularVelocity_rad;
        float3 forceAccumulator, localTorqueAccumulator;

        public float3 position;
        public Quaternion rotation;


        public DreamCollider collider;

        public DreamRigidbody(float _m, float3 _initVel, float3 _initPos, bool _isStatic, DreamCollider _coll)
        :this(_m, _initVel, _initPos, Quaternion.identity, 0, _isStatic, _coll){}
        

        public DreamRigidbody(float _m, float3 _initVel, float3 _initPos, Quaternion quat, bool _isStatic, DreamCollider _coll)
        :this(_m, _initVel, _initPos, quat, 0, _isStatic, _coll){}

        public DreamRigidbody(float _m, float3 _initVel, float3 _initPos, Quaternion quat, float3 angVel, bool _isStatic, DreamCollider _coll)
        {
            this.velocity = _initVel;
            this.position = _initPos;
            this.collider = _coll;
            this.rotation = quat;
            this.collider.rb = this;
            this.local_angularVelocity_rad = angVel;
            this.isStatic = _isStatic;
            Mass = _m;
        }

        public float3x3 Iinv => this.I_inv;

        public float Mass
        {
            get
            {
                return mass;
            }
            set
            {
                mass = value;
                invMass = 1f / mass;

                
                this.I = this.collider.CreateInertiaTensor(this.mass);
                this.I_inv = math.inverse(this.I);

            }
        }

        public float InvMass => invMass;
        
        internal Quaternion UnityRot => new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
        internal Quaternion ToQuaternion(float4 q) => new Quaternion(q.x, q.y, q.z, q.w); 

        internal float3 WorldAngularVel => UnityRot * local_angularVelocity_rad;

        internal float3 GetVelocityAtWorldPosition(float3 worldPos)
        {
            return velocity + math.cross(WorldAngularVel, worldPos - position);
        }

        internal void AddForce(float3 force)
        {
            forceAccumulator += force;
        }

        internal void AddTorqueLocal(float3 localTorque)
        {
            localTorqueAccumulator += localTorque;
        }

        internal void ClearAccumulator()
        {
            forceAccumulator = 0;
            localTorqueAccumulator = 0;
        }

        internal void Integrate(float dt)
        {
            if (!isStatic) 
            {
                if (math.lengthsq(velocity) < 1e-8) velocity =0;
                if (math.lengthsq(forceAccumulator) < 1e-8) forceAccumulator =0;
                if (math.lengthsq(local_angularVelocity_rad) < 1e-8) local_angularVelocity_rad =0;
                if (math.lengthsq(localTorqueAccumulator) < 1e-8) localTorqueAccumulator =0;

                // velocity = math.lerp(velocity, 0, dt);
                // forceAccumulator = math.lerp(forceAccumulator, 0, dt);
                // local_angularVelocity_rad = math.lerp(local_angularVelocity_rad, 0, dt);
                // localTorqueAccumulator = math.lerp(localTorqueAccumulator, 0, dt);

                position += velocity * dt;
                var a = forceAccumulator * this.invMass;
                velocity += dt * a;

                #region New Rotation
                RotateByLocalEuler(local_angularVelocity_rad * dt* Mathf.Rad2Deg);

                var wCrossIW = math.cross(local_angularVelocity_rad, math.mul(I, local_angularVelocity_rad));
                var torqueExt_wCrossIW = localTorqueAccumulator * invMass - wCrossIW;
                var delW = math.mul(I_inv, torqueExt_wCrossIW);
                local_angularVelocity_rad += delW * dt;
                #endregion
            }

            else
            {
                velocity = 0;
                local_angularVelocity_rad = 0;    
            }

            
        }

        internal void RotateByLocalEuler(float3 localEuler)
        {
            rotation = rotation * Quaternion.Euler(localEuler);
            rotation = math.normalize(rotation);
        }

       public Vector3 TransformPointUnscaled(Vector3 pos)
        {
            var localToWorldMatrix = Matrix4x4.TRS(this.position, UnityRot, Vector3.one);
            return localToWorldMatrix.MultiplyPoint3x4(pos);
        }

        public Vector3 TransformDirectionUnscaled(Vector3 pos)
        {
            var localToWorldMatrix = Matrix4x4.TRS(Vector3.zero, UnityRot, Vector3.one);
            return localToWorldMatrix.MultiplyPoint3x4(pos);
        }

        public Vector3 InverseTransformDirectionUnscaled(Vector3 pos)
        {
            var worldToLocalMatrix = Matrix4x4.TRS(Vector3.zero, UnityRot, Vector3.one).inverse;
            return worldToLocalMatrix.MultiplyPoint3x4(pos);
        }

        public Vector3 InverseTransformPointUnscaled(Vector3 pos)
        {
            var worldToLocalMatrix = Matrix4x4.TRS(this.position, UnityRot, Vector3.one).inverse;
            return worldToLocalMatrix.MultiplyPoint3x4(pos);
        }
    }
}
