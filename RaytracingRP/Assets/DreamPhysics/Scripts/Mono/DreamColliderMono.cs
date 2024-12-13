using UnityEngine;

namespace DreamPhysics.MonoBehavior
{
    public abstract class DreamColliderMono : MonoBehaviour
    {
        public abstract void Create();
        public abstract DreamCollider GetCollider();
    }
}
