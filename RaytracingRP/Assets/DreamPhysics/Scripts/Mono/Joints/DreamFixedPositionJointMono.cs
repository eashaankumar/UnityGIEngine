using DreamPhysics.Joints;
using UnityEngine;

namespace DreamPhysics.MonoBehavior.Joints
{
    public class DreamFixedPositionJointMono : DreamJointMono
    {
        [SerializeField] DreamRigidbodyMono rb1;
        [SerializeField] DreamRigidbodyMono rb2;
        [SerializeField, Min(0)] float convergenceRate;
        [SerializeField] Vector3 relOffset;
        DreamFixedPositionJoint fixedJoint;
        public override DreamJoint CreateJoint()
        {
            fixedJoint = new DreamFixedPositionJoint()
            {
                rb1 = rb1.RB,
                rb2 = rb2.RB,
                convergenceRate = convergenceRate,
                relOffset = relOffset,
            };
            return fixedJoint;
        }

        void Update()
        {
            if (fixedJoint == null) return;
            fixedJoint.convergenceRate = convergenceRate;
            fixedJoint.relOffset = relOffset;
            fixedJoint.rb1 = rb1.RB;
            fixedJoint.rb2 = rb2.RB;
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
