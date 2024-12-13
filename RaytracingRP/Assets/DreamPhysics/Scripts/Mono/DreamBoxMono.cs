using UnityEditor;
using UnityEngine;

namespace DreamPhysics.MonoBehavior
{
    public class DreamBoxMono : DreamColliderMono
    {
        [SerializeField, Min(0.00001f)] Vector3 size = Vector3.one;

        DreamBox box;

        Vector3 GizmosSize => size;

        public override void Create()
        {
            box = new DreamBox(size);
        }

        public override DreamCollider GetCollider()
        {
            return box;
        }

        void OnValidate()
        {
            transform.localScale = size;
        }

        #if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Handles.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Color col = Color.Lerp(Color.blue, Color.white, 0.5f);
            col.a = 1;
            Handles.color = col;
            Handles.DrawWireCube(Vector3.zero, GizmosSize);
            
        }
        #endif
    }
}
