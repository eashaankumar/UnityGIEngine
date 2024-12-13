using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

namespace AnimalFarmTerrainSystem
{
    public class ComputeShaderTerrainSystem : MonoBehaviour
    {
        [SerializeField] ComputeShader terrainGeneratorShader;
        [SerializeField] ComputeShader meshGenShader;
        [SerializeField] Vector2Int numChunks;
        [SerializeField] float chunkSize;
        [SerializeField] int numVoxelsPerAxis;
        [SerializeField] int maxHeight;
        [SerializeField] int seed;
        [SerializeField, Min(1)] int octaves;
        [SerializeField, Min(1)] float spread;
        [SerializeField] Material material;
        [SerializeField] UnityEngine.Experimental.Rendering.RayTracingMode rayTracingMode;

        public RenderTexture _noiseTex;

        public UnityEvent ChunksLoaded;

        public float VoxelSize => chunkSize / numVoxelsPerAxis;

        int RTWidth => numChunks.x * numVoxelsPerAxis;
        int RTHeight => numChunks.y * numVoxelsPerAxis;

        int ChunkWidth =>  numVoxelsPerAxis;
        int ChunkHeight => numVoxelsPerAxis;

        struct Voxel
        {
            public int voxelX, voxelY;
            public float noise;

            public static int NumBytes =>
                sizeof(int) * 2 + sizeof(float);

            public int2 ToId => new int2(voxelX, voxelY);

            public Vector3 VoxelToVertex(float maxHeight, float voxelSize)
            {
                return new Vector3(voxelX * voxelSize, noise * maxHeight, voxelY * voxelSize);
            }
            public static Vector2Int to2D(int index, int dim)
            {
                Vector2Int p = Vector2Int.zero;
                p.x = index / dim;
                p.y = index % dim;
                return p;
            }

            public static Vector3 VoxelIdToVertex(int index, int dim, float height, float voxelSize)
            {
                var voxId = Voxel.to2D(index, dim);
                return new Vector3(voxId.x * voxelSize, height, voxId.y * voxelSize);
            }
        };

        private void Awake()
        {
            GenerateTerrain();
        }

        private void OnDestroy()
        {
            _noiseTex.Release();
        }

        void GenerateTerrain()
        {
            // create entire world
            InitRenderTexture(ref _noiseTex);

            terrainGeneratorShader.SetTexture(0, "NoiseTex", _noiseTex);
            terrainGeneratorShader.SetInt("Seed", seed);
            terrainGeneratorShader.SetInt("Octaves", octaves);
            terrainGeneratorShader.SetFloat("Frequency", 1f / (spread + 1));
            int threadGroupsX = Mathf.CeilToInt((RTWidth+1) / 8.0f);
            int threadGroupsY = Mathf.CeilToInt((RTHeight+1) / 8.0f);
            terrainGeneratorShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

            StartCoroutine(ExtractChunks());
        }

        IEnumerator ExtractChunks()
        {
            float totalProgress = numChunks.x * numChunks.y;
            float currentProgress = 0;

            meshGenShader.SetTexture(0, "NoiseTex", _noiseTex);
            meshGenShader.SetFloat("MaxHeight", maxHeight);
            meshGenShader.SetFloat("VoxelSize", VoxelSize);
            meshGenShader.SetInt("NumVoxelsPerAxis", numVoxelsPerAxis);

            for (int x = 0; x < numChunks.x; x++)
            {
                for (int y = 0; y < numChunks.y; y++)
                {
                    var chunkId = new Vector2((int)x, (int)y);
                    GameObject go = new GameObject(chunkId + "");
                    go.transform.SetParent(transform, false);
                    var mf = go.AddComponent<MeshFilter>();
                    var mr = go.AddComponent<MeshRenderer>();
                    go.transform.position = new Vector3(x, 0, y) * chunkSize;
                    mr.sharedMaterial = material;
                    mf.sharedMesh = new Mesh();
                    mr.rayTracingMode = rayTracingMode;
                    mr.rayTracingAccelerationStructureBuildFlags = UnityEngine.Rendering.RayTracingAccelerationStructureBuildFlags.PreferFastTrace;
                    Mesh mesh = mf.sharedMesh;

                    var vertexComputeBuffer = new ComputeBuffer((ChunkWidth + 1) * (ChunkHeight + 1), Voxel.NumBytes, ComputeBufferType.Append);
                    vertexComputeBuffer.SetCounterValue(0);

                    meshGenShader.SetBuffer(0, "Voxels", vertexComputeBuffer);
                    meshGenShader.SetVector("ChunkId", chunkId);

                    int threadGroupsX = Mathf.CeilToInt((ChunkWidth +1) / 8.0f);
                    int threadGroupsY = Mathf.CeilToInt((ChunkHeight +1) / 8.0f);
                    meshGenShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

                    #region Get Buffer Size
                    var countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
                    var args = new uint[1];

                    ComputeBuffer.CopyCount(vertexComputeBuffer, countBuffer, 0);
                    countBuffer.GetData(args);
                    countBuffer.Dispose();
                    #endregion

                    #region Read Triangle Buffer
                    var VoxelCount = args[0];
                    Debug.Log(VoxelCount + " " + vertexComputeBuffer.count);
                    var voxels = new Voxel[VoxelCount];
                    vertexComputeBuffer.GetData(voxels, 0, 0, (int)VoxelCount);
                    #endregion

                    #region Create Mesh
                    List<Vector3> vertices = new List<Vector3>();
                    List<int> tris = new List<int>();

                    ConcurrentDictionary<int2, float> voxelIdToHeightMap = new ConcurrentDictionary<int2, float>();

                    Parallel.ForEach(voxels, (v) =>
                    {
                        voxelIdToHeightMap.TryAdd(v.ToId, v.noise * maxHeight);
                    });

                    Vector3 toVertex(int2 id, float height) => new Vector3(id.x * VoxelSize, height, id.y * VoxelSize);

                    for(int vx = 0; vx < ChunkWidth; vx++)
                    {
                        var worldXVoxelId = vx + chunkId.x * numVoxelsPerAxis; // Compute Shader generates a wall because rt is out of bounds
                        if (worldXVoxelId >= RTWidth-1) continue;
                        for(int vy = 0; vy < ChunkHeight; vy++)
                        {
                            var worldYVoxelId = vy + chunkId.y * numVoxelsPerAxis;
                            if (worldYVoxelId >= RTHeight-1) continue;
                            try
                            {


                                var id = new int2(vx, vy);
                                var idX = id + new int2(1, 0);
                                var idY = id + new int2(0, 1);
                                var idXY = id + new int2(1, 1);

                                var height = voxelIdToHeightMap[id];
                                var heightX = voxelIdToHeightMap[idX];
                                var heightY = voxelIdToHeightMap[idY];
                                var heightXY = voxelIdToHeightMap[idXY];

                                var vertex = toVertex(id, height);
                                var vertexX = toVertex(idX, heightX);
                                var vertexY = toVertex(idY, heightY);
                                var vertexXY = toVertex(idXY, heightXY);

                                vertices.Add(vertex);
                                vertices.Add(vertexX);
                                vertices.Add(vertexXY);

                                tris.Add(vertices.Count - 1);
                                tris.Add(vertices.Count - 2);
                                tris.Add(vertices.Count - 3);

                                vertices.Add(vertexXY);
                                vertices.Add(vertexY);
                                vertices.Add(vertex);

                                tris.Add(vertices.Count - 1);
                                tris.Add(vertices.Count - 2);
                                tris.Add(vertices.Count - 3);
                            }
                            catch
                            {

                            }
                        }
                    }

                    mesh.SetVertices(vertices.ToArray());
                    mesh.SetTriangles(tris.ToArray(), 0);
                    mesh.RecalculateNormals();  
                    mesh.RecalculateBounds();
                    #endregion
                    vertexComputeBuffer.Dispose();
                    currentProgress++;
                }
                Debug.Log(currentProgress / totalProgress);
                yield return null;
            }

            ChunksLoaded?.Invoke();
        }

        private void InitRenderTexture(ref RenderTexture rt, int mips = 0)
        {
            if (rt == null || rt.width != RTWidth || rt.height != RTHeight)
            {
                // Release render texture if we already have one
                if (rt != null)
                    rt.Release();

                // Get a render target for Ray Tracing
                rt = new RenderTexture(RTWidth, RTHeight, 0, RenderTextureFormat.RFloat, mips);
                rt.enableRandomWrite = true;
                rt.useMipMap = true;
                rt.autoGenerateMips = false;
                rt.Create();
            }
        }
    }
}
