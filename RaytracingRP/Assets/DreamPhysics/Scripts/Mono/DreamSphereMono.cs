using UnityEditor;
using UnityEngine;

namespace DreamPhysics.MonoBehavior
{
    public class DreamSphereMono : DreamColliderMono
    {
        [SerializeField, Min(0.00001f)] float radius = 0.5f;

        DreamSphere sphere;

        public override void Create()
        {
            sphere = new DreamSphere(radius);
        }

        public override DreamCollider GetCollider()
        {
            return sphere;
        }

        void OnValidate()
        {
            transform.localScale = Vector3.one * radius * 2;
        }

        #if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Handles.matrix = Matrix4x4.identity;
            Color col = Color.Lerp(Color.blue, Color.white, 0.5f);
            col.a = 1;
            Handles.color = col;
            Handles.DrawWireDisc(transform.position, transform.up, radius);
            Handles.DrawWireDisc(transform.position, transform.right, radius);
            Handles.DrawWireDisc(transform.position, transform.forward, radius);
        }
        #endif
    }
}
