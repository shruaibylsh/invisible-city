using UnityEngine;
using System.Collections.Generic;

public class BuildingTriangleBufferWithAABB : MonoBehaviour
{
    public LayerMask occlusionLayer;
    public ComputeShader visibilityCS;

    GraphicsBuffer triangleBuffer;
    GraphicsBuffer aabbBuffer;
    GraphicsBuffer triangleRangeBuffer;

    int totalTriangles = 0;
    int totalAABBs = 0;

    struct Triangle
    {
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;
    }

    struct AABB
    {
        public Vector3 min;
        public Vector3 max;
    }

    struct TriangleRange
    {
        public int startIndex;
        public int count;
    }

    void Start()
    {
        BuildBuffers();
    }

    void BuildBuffers()
    {
        List<Triangle> triangles = new List<Triangle>();
        List<AABB> aabbs = new List<AABB>();
        List<TriangleRange> triangleRanges = new List<TriangleRange>();

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        Debug.Log($"[AABB Buffer] Found {allObjects.Length} objects in scene.");

        foreach (GameObject obj in allObjects)
        {
            if ((occlusionLayer.value & (1 << obj.layer)) == 0)
                continue;

            MeshFilter mf = obj.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
                continue;

            Mesh mesh = mf.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            int[] indices = mesh.triangles;

            Matrix4x4 localToWorld = obj.transform.localToWorldMatrix;

            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

            int startIdx = triangles.Count;

            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 v0 = localToWorld.MultiplyPoint3x4(vertices[indices[i]]);
                Vector3 v1 = localToWorld.MultiplyPoint3x4(vertices[indices[i + 1]]);
                Vector3 v2 = localToWorld.MultiplyPoint3x4(vertices[indices[i + 2]]);

                triangles.Add(new Triangle { v0 = v0, v1 = v1, v2 = v2 });

                min = Vector3.Min(min, v0);
                min = Vector3.Min(min, v1);
                min = Vector3.Min(min, v2);
                max = Vector3.Max(max, v0);
                max = Vector3.Max(max, v1);
                max = Vector3.Max(max, v2);
            }

            int triCount = triangles.Count - startIdx;
            aabbs.Add(new AABB { min = min, max = max });
            triangleRanges.Add(new TriangleRange { startIndex = startIdx, count = triCount });

            Debug.Log($"[AABB Buffer] Processed {obj.name}: {triCount} triangles, AABB min {min}, max {max}");
        }

        totalTriangles = triangles.Count;
        totalAABBs = aabbs.Count;

        triangleBuffer?.Release();
        aabbBuffer?.Release();
        triangleRangeBuffer?.Release();

        triangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalTriangles, sizeof(float) * 9);
        triangleBuffer.SetData(triangles);

        aabbBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalAABBs, sizeof(float) * 6);
        aabbBuffer.SetData(aabbs);

        triangleRangeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalAABBs, sizeof(int) * 2);
        triangleRangeBuffer.SetData(triangleRanges);

        int kernel = visibilityCS.FindKernel("CSMain");
        visibilityCS.SetBuffer(kernel, "TriangleBuffer", triangleBuffer);
        visibilityCS.SetBuffer(kernel, "AABBBuffer", aabbBuffer);
        visibilityCS.SetBuffer(kernel, "TriangleRangeBuffer", triangleRangeBuffer);
        visibilityCS.SetInt("TotalAABBs", totalAABBs);

        Debug.Log($"[AABB Buffer] Final: {totalAABBs} AABBs, {totalTriangles} triangles uploaded.");
    }

    void OnDestroy()
    {
        triangleBuffer?.Release();
        aabbBuffer?.Release();
        triangleRangeBuffer?.Release();
    }
}
