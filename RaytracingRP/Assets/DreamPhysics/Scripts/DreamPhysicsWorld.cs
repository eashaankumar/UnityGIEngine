using System.Collections.Generic;
using System.Threading.Tasks;
using DreamPhysics.Joints;
using Unity.Mathematics;
using UnityEngine;

namespace DreamPhysics
{
    public class DreamPhysicsWorld
    {
        public List<DreamRigidbody> bodies = new List<DreamRigidbody>();
        public List<Joints.DreamJoint> joints = new List<Joints.DreamJoint>();

        public struct DebugContactPoint
        {
            public DreamRigidbody rb1;
            public DreamRigidbody rb2;
            public Vector3 point;
            public Vector3 normal;
            public float depth;

            #if UNITY_EDITOR
            public void DrawGizmos()
            {
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(point, 0.01f);
                Gizmos.DrawLine(point, point + normal * depth);
            }
            #endif
        }

        public List<DebugContactPoint> debugCps = new List<DebugContactPoint>();

        public DreamPhysicsWorld()
        {

        }

        public void AddBodyToSimulation(DreamRigidbody rb)
        {
            bodies.Add(rb);
        }

        public void AddJointToSimulation(Joints.DreamJoint joint)
        {
            joints.Add(joint);
        }

        public void AddForceOnAllBodies(System.Action<DreamRigidbody> forceApplicator)
        {
            Parallel.ForEach(bodies, (body) => {
                forceApplicator.Invoke(body);
            });
        }

        public void Simulate(float dt, uint substeps)
        {
            float substep_dt = dt / substeps;
            for(int step = 0; step < substeps; step++)
            {
                Integration(substep_dt);
                Solve(substep_dt);
                SolveVelocities(substep_dt);
            }
            ClearAccumulator();
        }

        void Damping(float dt)
        {
            Parallel.ForEach(bodies, (body) => {
                body.velocity = math.lerp(body.velocity, 0, dt);
                body.local_angularVelocity_rad = math.lerp(body.local_angularVelocity_rad, 0, dt);

                if (math.lengthsq(body.velocity) < 1e-3f) body.velocity =0;
                if (math.lengthsq(body.local_angularVelocity_rad) < 1e-3f) body.local_angularVelocity_rad =0;
            });
        }

        void Integration(float dt)
        {
            Parallel.ForEach(bodies, (body)=>{
                body.Integrate(dt);
            });
        }

        void ClearAccumulator()
        {
            Parallel.ForEach(bodies, (b)=>{
                b.ClearAccumulator();
            });
        }

        void Solve(float dt)
        {
            // foreach constraint, compute delta X and update X (ONLY, NO VEL UPDATES HERE!)
            CollisionSolver(dt);
        }

        
        void CollisionSolver(float dt)
        {
            debugCps.Clear();
            for(int i = 0; i < bodies.Count; i++)
            {
                var body1 = bodies[i];
                for(int j = i+1; j < bodies.Count; j++)
                {
                    var body2 = bodies[j];
                    if (body1 == body2) continue;
                    if (body1.isStatic && body2.isStatic) continue;
                    CollisionSolverManager.SolveCollision(body1, body2, dt, debugCps);
                }
            }
        }

        void SolveVelocities(float dt)
        {
            foreach(var cp in debugCps)
            {
                VelocitySolverManager.SolveVelocityFriction(cp, dt);
            }

            foreach(var j in joints)
            {
                j.SolveJoint(dt);
            }
        }
    
        #if UNITY_EDITOR
        public void DrawGizmos()
        {
            foreach(var body in bodies)
            {
                body.collider.DrawGizmos();
            }
            foreach(var de in debugCps)
            {
                de.DrawGizmos();
            }
        }
        #endif
    
        public static class VelocitySolverManager
        {

            public static void SolveVelocityNoFriction(DebugContactPoint cp, float dt)
            {
                // Physics for game developers - Bourg & Bywalec
                // pg 117
                var r1_wld = (float3)cp.point - cp.rb1.position; // world contact pos w.r.t. rb1
                var r2_wld = (float3)cp.point - cp.rb2.position; // world contact pos w.r.t. rb2

                float GetJ(float3 vcp1, float3 vcp2, float e, float invM1, float invM2, float3 r1, float3 r2, float3x3 invI1, float3x3 invI2)
                {
                    var vr = vcp1 - vcp2;
                    var vrdotn = math.dot(vr, cp.normal);
                    var ndotr1XnJcrossr = math.dot(cp.normal, math.cross(math.mul(math.cross(r1, cp.normal), invI1), r1));
                    var ndotr2XnJcrossr = math.dot(cp.normal, math.cross(math.mul(math.cross(r2, cp.normal), invI2), r2));

                    var denom = invM1 + invM2 + ndotr1XnJcrossr + ndotr2XnJcrossr;
                    var numer = -vrdotn * (e + 1);
                    return numer / denom;
                }

                var Jmag = 
                    GetJ(vcp1: cp.rb1.GetVelocityAtWorldPosition(cp.point), vcp2: cp.rb2.GetVelocityAtWorldPosition(cp.point), 
                        e: 0.1f, 
                        invM1: cp.rb1.InvMass, invM2: cp.rb2.InvMass, 
                        r1: r1_wld, r2: r2_wld, 
                        invI1: cp.rb1.Iinv, invI2: cp.rb2.Iinv
                    );

                var dv1 = Jmag * cp.normal * cp.rb1.InvMass;
                var dv2 = -Jmag * cp.normal * cp.rb2.InvMass;

                cp.rb1.velocity += (float3)dv1 ;
                cp.rb2.velocity += (float3)dv2 ;

                var dang1 = math.mul(math.cross(r1_wld, Jmag * cp.normal), cp.rb1.Iinv) ;
                var dang2 = math.mul(math.cross(r2_wld, -Jmag * cp.normal), cp.rb2.Iinv);

                cp.rb1.local_angularVelocity_rad += (float3)(Quaternion.Inverse(cp.rb1.rotation) * dang1);
                cp.rb2.local_angularVelocity_rad += (float3)(Quaternion.Inverse(cp.rb2.rotation) * dang2);
            }

            public static void SolveVelocityFriction(DebugContactPoint cp, float dt)
            {
                // Physics for game developers - Bourg & Bywalec
                // pg 117
                var r1_wld = (float3)cp.point - cp.rb1.position; // world contact pos w.r.t. rb1
                var r2_wld = (float3)cp.point - cp.rb2.position; // world contact pos w.r.t. rb2

                float3 n = math.normalizesafe(cp.normal);

                float GetJ(float3 vcp1, float3 vcp2, float e, float invM1, float invM2, float3 r1, float3 r2, float3x3 invI1, float3x3 invI2, out float3 vr)
                {
                    vr = vcp1 - vcp2;
                    var vrdotn = math.dot(vr, n);
                    var ndotr1XnJcrossr = math.dot(n, math.cross(math.mul(math.cross(r1, n), invI1), r1));
                    var ndotr2XnJcrossr = math.dot(n, math.cross(math.mul(math.cross(r2, n), invI2), r2));

                    var denom = invM1 + invM2 + ndotr1XnJcrossr + ndotr2XnJcrossr;
                    var numer = -vrdotn * (e + 0f);
                    return numer / denom;
                }

                var Jmag = 
                    GetJ(vcp1: cp.rb1.GetVelocityAtWorldPosition(cp.point), vcp2: cp.rb2.GetVelocityAtWorldPosition(cp.point), 
                        e: 0.1f, 
                        invM1: cp.rb1.InvMass, invM2: cp.rb2.InvMass, 
                        r1: r1_wld, r2: r2_wld, 
                        invI1: cp.rb1.Iinv, invI2: cp.rb2.Iinv,
                        out var vr
                    );

                var t = math.normalizesafe(math.cross(math.cross(n, vr ), n));

                var dynamic_friction_coefficient = 2f;
                var static_friction_coefficient = 2f;
                var chosen_friction_coef = math.lengthsq(vr) < 1e-5f ? static_friction_coefficient : dynamic_friction_coefficient;
                var frictionImpulse = Jmag * t * chosen_friction_coef;

                var normalImpulse = Jmag * n;

                Debug.DrawLine(cp.point, (float3)cp.point + n, Color.magenta, Time.deltaTime);
                Debug.DrawLine(cp.point, (float3)cp.point + t, Color.white, Time.deltaTime);
                Debug.DrawLine(cp.point, (float3)cp.point + math.normalize(vr), Color.yellow, Time.deltaTime);


                var dv1 = (normalImpulse + frictionImpulse) * cp.rb1.InvMass;
                var dv2 = (-normalImpulse - frictionImpulse) * cp.rb2.InvMass;

                cp.rb1.velocity += dv1 ;
                cp.rb2.velocity += dv2 ;

                var dang1 = math.mul(math.cross(r1_wld, normalImpulse + frictionImpulse), cp.rb1.Iinv) ;
                var dang2 = math.mul(math.cross(r2_wld, -normalImpulse - frictionImpulse), cp.rb2.Iinv);

                cp.rb1.local_angularVelocity_rad += (float3)(Quaternion.Inverse(cp.rb1.rotation) * dang1);
                cp.rb2.local_angularVelocity_rad += (float3)(Quaternion.Inverse(cp.rb2.rotation) * dang2);
            }
        }

        public static class CollisionSolverManager
        {
            public static void SolveCollision(DreamRigidbody rb1, DreamRigidbody rb2, float dt, List<DebugContactPoint> debugcps)
            {
                var isRb1Sphere = rb1.collider.GetType() == typeof(DreamSphere);
                var isRb2Sphere = rb2.collider.GetType() == typeof(DreamSphere);

                if (isRb1Sphere && isRb2Sphere)
                {
                    SolveCollisionSphere(rb1, rb2, debugcps);
                    return;
                }

                var isRb1Box = rb1.collider.GetType() == typeof(DreamBox);
                if (isRb1Box && isRb2Sphere)
                {
                    SolveCollisionSphereBox(rb2, rb1, debugcps);
                    return;
                }
                var isRb2Box = rb2.collider.GetType() == typeof(DreamBox);
                if (isRb1Sphere && isRb2Box)
                {
                    SolveCollisionSphereBox(rb1, rb2, debugcps);
                    return;
                }
                if (isRb1Box && isRb2Box)
                {
                    SolveCollisionBox(rb1, rb2, debugcps);
                    return;
                }
            }

            public static void SolveCollisionBox(DreamRigidbody rb1, DreamRigidbody rb2, List<DebugContactPoint> debugcps)
            {
                var box1 = rb1.collider as DreamBox;
                var box2 = rb2.collider as DreamBox;

                BoxSATSolver boxSolver = new BoxSATSolver();
                List<BoxSATSolver.ContactPoint> cps = new List<BoxSATSolver.ContactPoint>();
                boxSolver.Solve(box1, box2, cps, out var bestCase);
                if (cps.Count == 0 || bestCase < 0) return;
                BoxSATSolver.ContactPoint deepestCp = default;
                deepestCp.penDepth = float.MinValue;
                foreach(var cp in cps)
                {

                    if (cp.penDepth > deepestCp.penDepth)
                    {
                        deepestCp = cp;
                    }
                }

                #region Solve collision constraint
                float3 normal = 0;
                bool isSign = false;
                if (bestCase < 6)
                {
                    int sign = bestCase < 3 ? 1 : -1;
                    isSign = sign < 0;
                    normal = (float3)deepestCp.normal * sign; // box sat solver returns opposite normals :)
                }
                else
                {
                    isSign = true;
                    normal = -(float3)deepestCp.normal; // box sat solver returns opposite normals :)
                }
                float isStatic = rb1.isStatic || rb2.isStatic ? 1 : 0.5f;

                if (!rb1.isStatic) rb1.position += -normal * deepestCp.penDepth * isStatic;
                if (!rb2.isStatic) rb2.position += normal * deepestCp.penDepth * isStatic;

                foreach(var cp in cps)
                {
                    debugcps.Add(new DebugContactPoint
                    {
                        normal = normal,
                        point = cp.point,
                        depth = cp.penDepth,
                        rb1 = rb1,
                        rb2 = rb2,
                    });
                }
                #endregion
            }

            public static void SolveCollisionSphereBox(DreamRigidbody sphererb, DreamRigidbody boxrb, List<DebugContactPoint> debugcps)
            {
                var boxColl = boxrb.collider as DreamBox;
                var sphereColl = sphererb.collider as DreamSphere;

                var sphereCenterInBoxCoords = boxrb.InverseTransformPointUnscaled(sphererb.position);
                
                float3 normal = 0;

                #region Get normal
                //var axes = new float3[]{boxColl.Up, boxColl};
                Bounds b = new Bounds(Vector3.zero, boxColl.Size);
                var closestPointOnAABB = b.ClosestPoint(sphereCenterInBoxCoords);
                var distanceToClosestPoint = Vector3.Magnitude(closestPointOnAABB - sphereCenterInBoxCoords);
                if (distanceToClosestPoint > sphereColl.radius) return;
                var delta = sphereColl.radius - distanceToClosestPoint;
                var closestPointOnOBB = boxrb.TransformPointUnscaled(closestPointOnAABB);
                normal = math.normalize((float3)closestPointOnOBB - sphererb.position);
                #endregion
                
                float isStatic = sphererb.isStatic || boxrb.isStatic ? 1 : 0.5f;

                if (!sphererb.isStatic) sphererb.position += -normal * delta * isStatic;
                if (!boxrb.isStatic) boxrb.position += normal * delta * isStatic;

                debugcps.Add(new DebugContactPoint
                {
                    normal = normal,
                    point = sphererb.position + normal * sphereColl.radius,
                    depth = delta,
                    rb1 = sphererb,
                    rb2 = boxrb,
                });
                
            }

            public static void SolveCollisionSphere(DreamRigidbody rb1, DreamRigidbody rb2, List<DebugContactPoint> debugcps)
            {
                var sphere1 = rb1.collider as DreamSphere;
                var sphere2 = rb2.collider as DreamSphere;

                var dst = math.distance(rb1.position, rb2.position);
                var sum_rad = sphere1.radius + sphere2.radius;
                if (dst < sum_rad)
                {
                    // collision
                    var delta = sum_rad - dst;
                    var vec1_2_center = rb2.position - rb1.position;
                    float3 normal = 0;

                    if (Mathf.Approximately(dst, 0))
                    {
                        normal = math.right();
                    }
                    else{
                        normal = vec1_2_center / dst;
                    }

                    float isStatic = rb1.isStatic || rb1.isStatic ? 1 : 0.5f;
                    // delta x update
                    if (!rb1.isStatic) rb1.position += -normal * delta * isStatic ;
                    if (!rb2.isStatic) rb2.position += normal * delta * isStatic ;

                    debugcps.Add(new DebugContactPoint
                    {
                        normal = normal,
                        point = rb1.position + normal * sphere1.radius,
                        depth = delta,
                        rb1 = rb1,
                        rb2 = rb2
                    });
                }
            }
        }

        public static class SDF
    {
        public static float SDBox( float3 p, float3 c, float3 b )
        {
            float3 q = math.abs(p - c) - b;
            return math.length(math.max(q,0f)) + math.min(math.max(q.x, math.max(q.y,q.z)), 0f);
        }
    }

    }
}
