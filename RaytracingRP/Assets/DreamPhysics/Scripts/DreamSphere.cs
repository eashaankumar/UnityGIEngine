using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace DreamPhysics
{
    public class DreamSphere : DreamCollider
    {
        public float radius;

        public DreamSphere(float _r)
        {
            this.radius = _r;
        }

        public override float3x3 CreateInertiaTensor(float mass)
        {
            var r2 = radius * radius;
            return math.mulScale(float3x3.identity, 2f/5f * mass * r2);
        }

#if UNITY_EDITOR
        public override void DrawGizmos()
        {
            Handles.matrix = Matrix4x4.identity;
            Handles.color = Color.Lerp(Color.blue, Color.white, 0.5f);
            Handles.DrawWireDisc(rb.position, this.Up, radius);
            Handles.DrawWireDisc(rb.position, this.Right, radius);
            Handles.DrawWireDisc(rb.position, this.Forward, radius);
        }
        #endif
    }
}
