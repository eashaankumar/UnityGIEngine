using DreamPhysics.Joints;
using Unity.Mathematics;
using UnityEngine;

namespace DreamPhysics
{
    public class DreamSpringJoint : DreamJoint
    {
        public DreamPhysics.DreamRigidbody rb1;
        public DreamPhysics.DreamRigidbody rb2;

        public float minLength;

        public float springStrength;

        public override void SolveJoint(float dt)
        {
            var vec12 = rb2.position - rb1.position;
            float dx = math.length(vec12);
            var dir = vec12 / dx;

            var vec = dir * (dx - minLength);

            rb1.velocity += vec/2 * springStrength * dt * rb1.InvMass;
            
            rb2.velocity += -vec/2 * springStrength * dt * rb2.InvMass;
        }
    }
}
