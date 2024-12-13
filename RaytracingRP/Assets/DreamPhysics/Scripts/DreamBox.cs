using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace DreamPhysics
{
    public class DreamBox : DreamCollider
    {
        public float3 bounds;

        public Vector3 Size => bounds;

        public Vector3 NNN => rb.TransformPointUnscaled(-Size/2);
        public Vector3 NNP => rb.TransformPointUnscaled(new Vector3(-Size.x, -Size.y, Size.z)/2);
        public Vector3 PNP => rb.TransformPointUnscaled(new Vector3(Size.x, -Size.y, Size.z)/2);
        public Vector3 PNN => rb.TransformPointUnscaled(new Vector3(Size.x, -Size.y, -Size.z)/2);

        public Vector3 NPN => rb.TransformPointUnscaled(new Vector3(-Size.x, Size.y, -Size.z)/2);

        public Vector3 NPP => rb.TransformPointUnscaled(new Vector3(-Size.x, Size.y, Size.z)/2);
        public Vector3 PPP => rb.TransformPointUnscaled(new Vector3(Size.x, Size.y, Size.z)/2);
        public Vector3 PPN => rb.TransformPointUnscaled(new Vector3(Size.x, Size.y, -Size.z)/2);

        Color col;

        public Vector3 GetAxis(int i)
        {
            if (i == 0) return this.Right;
            if (i == 1) return this.Up;
            if (i == 2) return this.Forward;
            return Position;
        }
        public bool ContainsPoint(Vector3 point)
        {
            var local = rb.InverseTransformPointUnscaled(point);
            var sizeH = Size / 2 + Vector3.one * 1e-4f;
            return Mathf.Abs(local.x) <= sizeH.x && Mathf.Abs(local.y) <= sizeH.y && Mathf.Abs(local.z) <= sizeH.z;
        }

        public float3 GetFaceSize(int i, out Vector3 localA, out Vector3 localB, out float nSize, out Vector3 localN)
        {
            var size = Size;
            if (i == 0)
            {
                localA = Vector3.up;
                localB = Vector3.forward;
                nSize = size.x;
                localN = Vector3.right;
                return new float3(0, size.y, size.z);
            }
            if (i == 1)
            {
                localA = Vector3.right;
                localB = Vector3.forward;
                nSize = size.y;
                localN = Vector3.up;
                return new float3(size.x, 0, size.z);
            }
            if (i == 2)
            {
                localA = Vector3.right;
                localB = Vector3.up;
                nSize = size.z;
                localN = Vector3.forward;
                return new float3(size.x, size.y, 0);
            }
            throw new System.InvalidOperationException($"Unknown face index: {i} for box");
        }

        public DreamBox(float3 _b)
        {
            this.bounds = _b;
            col = UnityEngine.Random.ColorHSV();
        }

        public override float3x3 CreateInertiaTensor(float mass)
        {
            var s2 = (float3)Size * Size;
            return math.mulScale(float3x3.identity, new float3(s2.y + s2.z, s2.x + s2.z, s2.x + s2.y) * mass / 12f);
        }

#if UNITY_EDITOR
        public override void DrawGizmos()
        {
            base.DrawGizmos();
            Handles.matrix = Matrix4x4.identity;
            Handles.color = Color.Lerp(Color.blue, Color.white, 0.5f);
            Handles.DrawDottedLines(new Vector3[]{
                // bottom face
                this.NNN, this.PNN,
                this.PNN, this.PNP,
                this.PNP, this.NNP,
                this.NNP, this.NNN,

                // top face
                this.NPN, this.PPN,
                this.PPN, this.PPP,
                this.PPP, this.NPP,
                this.NPP, this.NPN,

                // side faces
                this.NNN, this.NPN,
                this.NNP, this.NPP,
                this.PNP, this.PPP,
                this.PNN, this.PPN

            }, 5);
            //Gizmos.matrix = Matrix4x4.TRS(Position, rb.rotation, Vector3.one);
            //Gizmos.color = col;
            //Gizmos.DrawCube(Vector3.zero, Size);
        }
        #endif
    }
}
