using Unity.Mathematics;
using UnityEngine;

namespace DreamPhysics
{
    public class DreamCollider 
    {
        public DreamRigidbody rb;

        public Vector3 Position => rb.position;
        public Vector3 Up => rb.TransformDirectionUnscaled(math.up());
        public Vector3 Forward => rb.TransformDirectionUnscaled(math.forward());
        public Vector3 Right => rb.TransformDirectionUnscaled(math.right());

        public virtual float3x3 CreateInertiaTensor(float mass){return 0;}

        #if UNITY_EDITOR
        public virtual void DrawGizmos()
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.green;
            //Gizmos.DrawLine(Position, Position + Up);
            //Gizmos.DrawLine(Position, Position + Forward);
            //Gizmos.DrawLine(Position, Position + Right);

        }
        #endif
    }
}
