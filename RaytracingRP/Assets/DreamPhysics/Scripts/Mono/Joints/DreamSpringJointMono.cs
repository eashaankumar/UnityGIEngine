using DreamPhysics.Joints;
using UnityEngine;

namespace DreamPhysics.MonoBehavior.Joints
{
    public class DreamSpringJointMono : DreamJointMono
    {
        [SerializeField] DreamRigidbodyMono rb1;
        [SerializeField] DreamRigidbodyMono rb2;
        [SerializeField, Min(0)] float springStrength;
        [SerializeField, Min(0)] float minLength;
        DreamSpringJoint springJoint;
        public override DreamJoint CreateJoint()
        {
            springJoint = new DreamSpringJoint()
            {
                rb1 = rb1.RB,
                rb2 = rb2.RB,
                springStrength = springStrength,
                minLength = minLength,
            };
            return springJoint;
        }

        void Update()
        {
            if (springJoint == null) return;
            springJoint.springStrength = springStrength;
            springJoint.minLength = minLength;
            springJoint.rb1 = rb1.RB;
            springJoint.rb2 = rb2.RB;
        }

        #if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.black;
            Gizmos.DrawLine(rb1.transform.position, rb2.transform.position);
        }
        #endif
    }
}
