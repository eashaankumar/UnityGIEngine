using DreamPhysics.MonoBehavior;
using DreamPhysics.MonoBehavior.Joints;
using Unity.Mathematics;
using UnityEngine;

namespace DreamPhysics.Tests
{
    public class Test1 : MonoBehaviour
    {
        [SerializeField] uint substeps;
        [SerializeField] float3 gravity;
        [SerializeField] bool drawGizmos;
        DreamPhysicsWorld world;

        [SerializeField] DreamPhysics.MonoBehavior.DreamRigidbodyMono player;
        [SerializeField] float torque;

        //DreamRigidbody player;

        void Start()
        {
            world = new DreamPhysicsWorld();

            foreach(var rb in FindObjectsByType<DreamRigidbodyMono>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                world.AddBodyToSimulation(rb.CreateRB());
            }

            foreach(var rb in FindObjectsByType<DreamJointMono>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                world.AddJointToSimulation(rb.CreateJoint());
            }

            /*world.AddBodyToSimulation(player = new DreamRigidbody(1, 0, 0, false, new DreamSphere(0.5f)));

            for(int i = 0; i < 10; i++)
            {
                DreamRigidbody body2 = null;
                world.AddBodyToSimulation(body2 = new DreamRigidbody(10, 0, new float3(0, 2 * (i+1), 0), false, new DreamBox(new float3(1, 1,1) )));
                body2.local_angularVelocity_rad = new float3(0, 0, 0);
            }

            world.AddBodyToSimulation(new DreamRigidbody(1, 0, new float3(0, -1f, 0), false, new DreamSphere(0.25f)));

            world.AddBodyToSimulation(new DreamRigidbody(1, 0, math.down() * 5, true, new DreamBox(new float3(10, 1,10) )));*/

        }
        void FixedUpdate()
        {
            player.RB.AddTorqueLocal(Vector3.right * Input.GetAxis("Vertical") * torque);

            world.AddForceOnAllBodies((body) => {
                body.AddForce(body.Mass * gravity);
            });
            world.Simulate(Time.fixedDeltaTime, substeps);

        }

        void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            if (world != null)
                world.DrawGizmos();
        }
    }
}
