using DreamPhysics.Joints;
using UnityEngine;

namespace DreamPhysics.MonoBehavior.Joints
{
    public abstract class DreamJointMono : MonoBehaviour
    {
        public abstract DreamPhysics.Joints.DreamJoint CreateJoint();
    }
}
