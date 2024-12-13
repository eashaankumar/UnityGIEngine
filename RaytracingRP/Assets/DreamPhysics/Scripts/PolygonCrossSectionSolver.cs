using System.Collections.Generic;
using UnityEngine;

namespace DreamPhysics
{
    public class PolygonCrossSectionSolver
    {

        public static Polygon NewPolygon()
        {
            return new PolygonCrossSectionSolver.Polygon
                {
                    edges = new List<PolygonCrossSectionSolver.Edge>()
                };
        }
        [System.Serializable]
        public struct Edge
        {
            public Vector3 start, end;
            public Vector3 NormalVec => end - start;

            
        }

        [System.Serializable]
        public struct Polygon
        {
            public List<Edge> edges;
            public List<Vector3> GetPolygonPoints()
            {
                List<Vector3> points = new List<Vector3>();

                for(int i = 0; i < edges.Count; i++)
                {
                    for(int j = i+1; j < edges.Count; j++)
                    {
                        Math.Intersection(edges[i].start, edges[j].start, edges[i].NormalVec, edges[j].NormalVec, out var p0, out var p1, out var onSegment, out var intersects);
                        if (onSegment && intersects)
                        {
                            points.Add(p0);
                        }
                    }
                }

                return points;
            }

            public List<Vector3> GetPolygonPoints(float minDist)
            {
                List<Vector3> points = new List<Vector3>();

                for (int i = 0; i < edges.Count; i++)
                {
                    for (int j = i + 1; j < edges.Count; j++)
                    {
                        Math.Intersection(edges[i].start, edges[j].start, edges[i].NormalVec, edges[j].NormalVec, out var p0, out var p1, out var onSegment, out var intersects);
                        if (Vector3.Distance(p0, p1) < minDist)
                        {
                            points.Add(p0);
                        }
                    }
                }

                return points;
            }

            public void AddEdge(Vector3 start, Vector3 end)
            {
                edges.Add(new Edge
                {
                    start = start,
                    end = end,
                });
            }

            public void DrawGizmos()
            {
                if (edges != null)
                {
                    Gizmos.matrix = Matrix4x4.identity;
                    Gizmos.color = Color.white;
                    for (int i = 0; i < edges.Count; i++)
                    {
                        var edge = edges[i];
                        Gizmos.DrawLine(edge.start, edge.end);
                    }

                    var points = GetPolygonPoints();
                    Gizmos.color = Color.red;
                    for (int i = 0; i < points.Count; i++)
                    {
                        Gizmos.DrawSphere(points[i], 0.01f);
                    }
                }
            }

            public void DrawLines(Color color)
            {
                if (edges != null)
                {
                    for (int i = 0; i < edges.Count; i++)
                    {
                        var edge = edges[i];
                        Debug.DrawLine(edge.start, edge.end, color);
                    }
                }
            }
        }
    }
}
