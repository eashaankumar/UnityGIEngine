using UnityEngine;

namespace DreamPhysics
{
    public static class Math
    {
        public static Vector3 ElementWiseMult(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);

        public static void Intersection(Vector3 startA, Vector3 StartB, Vector3 EndMinusStartA, Vector3 EndMinusStartB, out Vector3 p0, out Vector3 p1, out bool onSegment, out bool intersects)
        {
            // https://www.youtube.com/watch?v=ELQG5OvmAE8
            Vector3 r = EndMinusStartA;
            Vector3 s = EndMinusStartB;
            Vector3 q = startA - StartB;

            float dotqr = Vector3.Dot(q, r);
            float dotqs = Vector3.Dot(q, s);
            float dotrs = Vector3.Dot(r, s);
            float dotrr = Vector3.Dot(r, r);
            float dotss = Vector3.Dot(s, s);

            float denom = dotrr * dotss - dotrs * dotrs;
            float numer = dotqs * dotrs - dotqr * dotss;

            float t = numer / denom;
            float u = (dotqs + t * dotrs) / dotss;

            p0 = startA + t * r;
            p1 = StartB + u * s;

            onSegment = false;
            intersects = false;
            if (0 <= t && t <= 1 && 0 <= u && u <= 1) onSegment = true;
            if ((p0 - p1).magnitude <= 1e-4f) intersects = true;
        }
    }
}
