using UnityEngine;

namespace DreamPhysics.MonoBehavior
{
    public class DreamRigidbodyMono : MonoBehaviour
    {
        [SerializeField, Min(0.00001f)] float mass = 1;
        [SerializeField] bool isStatic;
        
        DreamRigidbody rb;

        public DreamRigidbody RB => rb;

        public DreamRigidbody CreateRB()
        {
            var coll = GetComponent<DreamColliderMono>();
            coll.Create();
            rb = new DreamRigidbody(mass, 0, transform.position, transform.rotation, 0, isStatic, coll.GetCollider());
            return rb;
        }

        void Update()
        {
            if (rb == null) return;
            rb.Mass = mass;
            rb.isStatic = isStatic;
            transform.position = rb.position;
            transform.rotation = rb.rotation;
        }
    }
}
