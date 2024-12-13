using Unity.Mathematics;
using UnityEngine;

namespace DreamPhysics.Joints
{
    public class DreamFixedPositionJoint : DreamJoint
    {
        public DreamRigidbody rb1;
        public DreamRigidbody rb2;

        public float convergenceRate;
        public float3 relOffset;

        public override void SolveJoint(float dt)
        {
            var rb2pos = rb2.position;
            var targetPos = (float3)rb1.TransformPointUnscaled(relOffset);

            var correctionVector = rb2pos - targetPos;
            var dx = math.length(correctionVector);
            var correctionNormal = correctionVector/dx;

            var vr = rb1.velocity - rb2.velocity;
            float gamma = 0;
            var J = -(vr + convergenceRate * correctionVector / dt) * (1 / (gamma + 1 / (rb1.Mass + rb2.Mass)));

            //Debug.Log(J);
            rb1.velocity += -J * rb1.InvMass;
            rb2.velocity += J * rb2.InvMass;
        }
    }
}
