using UnityEngine;
using System.Collections.Generic;

public class BuildingTriangleBufferWithAABB : MonoBehaviour
{
    public LayerMask occlusionLayer;
    public ComputeShader visibilityCS;

    GraphicsBuffer triangleBuffer;
    GraphicsBuffer aabbBuffer;
    GraphicsBuffer triangleRangeBuffer;

    public static BuildingTriangleBufferWithAABB Instance { get; private set; }
    public List<AABB> AABBList => aabbs;

    const float AreaThreshold = 0.1f;

    struct Triangle
    {
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;
    }

    public struct AABB
    {
        public Vector3 min;
        public Vector3 max;
    }

    struct TriangleRange
    {
        public int startIndex;
        public int count;
    }

    List<AABB> aabbs = new List<AABB>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        BuildBuffers();
    }

    void BuildBuffers()
    {
        List<Triangle> triangles = new List<Triangle>();
        aabbs.Clear();
        List<TriangleRange> triangleRanges = new List<TriangleRange>();

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in allObjects)
        {
            if ((occlusionLayer.value & (1 << obj.layer)) == 0)
                continue;

            MeshFilter mf = obj.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                continue;
            }

            Mesh mesh = mf.sharedMesh;
            int subMeshCount = mesh.subMeshCount;
            Vector3[] vertices = mesh.vertices;
            Matrix4x4 localToWorld = obj.transform.localToWorldMatrix;

            for (int s = 0; s < subMeshCount; s++)
            {
                int[] indices = mesh.GetTriangles(s);
                Vector3 min = Vector3.positiveInfinity;
                Vector3 max = Vector3.negativeInfinity;

                int startIdx = triangles.Count;
                int kept = 0, skipped = 0;

                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vector3 v0 = localToWorld.MultiplyPoint3x4(vertices[indices[i]]);
                    Vector3 v1 = localToWorld.MultiplyPoint3x4(vertices[indices[i + 1]]);
                    Vector3 v2 = localToWorld.MultiplyPoint3x4(vertices[indices[i + 2]]);

                    float area = Vector3.Cross(v1 - v0, v2 - v0).magnitude * 0.5f;
                    if (area < AreaThreshold)
                    {
                        skipped++;
                        continue;
                    }

                    triangles.Add(new Triangle { v0 = v0, v1 = v1, v2 = v2 });
                    min = Vector3.Min(min, v0);
                    min = Vector3.Min(min, v1);
                    min = Vector3.Min(min, v2);
                    max = Vector3.Max(max, v0);
                    max = Vector3.Max(max, v1);
                    max = Vector3.Max(max, v2);
                    kept++;
                }

                int count = triangles.Count - startIdx;
                if (count > 0)
                {
                    aabbs.Add(new AABB { min = min, max = max });
                    triangleRanges.Add(new TriangleRange { startIndex = startIdx, count = count });
                }
            }
        }

        triangleBuffer?.Release();
        aabbBuffer?.Release();
        triangleRangeBuffer?.Release();

        triangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, triangles.Count, sizeof(float) * 9);
        triangleBuffer.SetData(triangles);

        aabbBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, aabbs.Count, sizeof(float) * 6);
        aabbBuffer.SetData(aabbs);

        triangleRangeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, aabbs.Count, sizeof(int) * 2);
        triangleRangeBuffer.SetData(triangleRanges);

        int kernel = visibilityCS.FindKernel("CSMain");
        visibilityCS.SetBuffer(kernel, "TriangleBuffer", triangleBuffer);
        visibilityCS.SetBuffer(kernel, "AABBBuffer", aabbBuffer);
        visibilityCS.SetBuffer(kernel, "TriangleRangeBuffer", triangleRangeBuffer);
        visibilityCS.SetInt("TotalAABBs", aabbs.Count);

        Debug.Log($"[TriangleBuffer] Final: {aabbs.Count} surfaces, {triangles.Count} triangles total.");
    }

    void OnDestroy()
    {
        triangleBuffer?.Release();
        aabbBuffer?.Release();
        triangleRangeBuffer?.Release();
    }
}
