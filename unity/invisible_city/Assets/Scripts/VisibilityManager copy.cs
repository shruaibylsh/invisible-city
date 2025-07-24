using UnityEngine;
using System.Collections.Generic;

public class PointCloudVisibilityManagerCopy : MonoBehaviour
{
    public ComputeShader visibilityCS;
    public BuildingPointCloudRenderer[] renderers;
    public Camera activeCamera;
    [Range(0, 1)] public float baseLearnRate = 0.4f;
    [Range(0, 1)] public float baseForgetRate = 0.2f;
    public float cullRadius = 50f;
    public float driftAmplitude = 0.15f;

    int kernel;
    List<int> visibleAABBIndices = new List<int>();
    GraphicsBuffer visibleAABBBuffer;

    void Awake()
    {
        kernel = visibilityCS.FindKernel("CSMain");
    }

    void LateUpdate()
    {
        if (visibilityCS == null || activeCamera == null) return;

        Matrix4x4 vp = activeCamera.projectionMatrix * activeCamera.worldToCameraMatrix;
        Vector3 camPos = activeCamera.transform.position;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        float radiusSqr = cullRadius * cullRadius;

        visibilityCS.SetVector("ScreenSize", screenSize);
        visibilityCS.SetFloat("DeltaTime", Time.deltaTime);
        visibilityCS.SetFloat("LearnRate", baseLearnRate);
        visibilityCS.SetFloat("ForgetRate", baseForgetRate);
        visibilityCS.SetMatrix("CameraVP", vp);
        visibilityCS.SetVector("CameraPosition", camPos);
        visibilityCS.SetFloat("CullRadiusSqr", radiusSqr);
        visibilityCS.SetFloat("Amplitude", driftAmplitude);
        visibilityCS.SetFloat("GlobalTime", Time.time);

        // Rebuild visible AABB list from frustum
        visibleAABBIndices.Clear();
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(activeCamera);
        var aabbList = BuildingTriangleBufferWithAABB.Instance.AABBList;

        for (int i = 0; i < aabbList.Count; i++)
{
    var aabb = aabbList[i];
    Bounds b = new Bounds();
    b.SetMinMax(aabb.min, aabb.max);

    // Center of the box
    Vector3 center = b.center;
    float sqrDist = (center - camPos).sqrMagnitude;

    // Only include if in frustum AND within culling radius
    if (sqrDist <= radiusSqr && GeometryUtility.TestPlanesAABB(frustumPlanes, b))
        visibleAABBIndices.Add(i);
}


        // DEBUG LOG: Print how many AABBs are visible this frame
        if (Time.frameCount % 10 == 0)
            Debug.Log($"[VisibilityManager] Visible AABBs this frame: {visibleAABBIndices.Count} / {aabbList.Count}");

        // Upload visible AABB indices to buffer
        if (visibleAABBBuffer == null || visibleAABBBuffer.count < visibleAABBIndices.Count)
        {
            visibleAABBBuffer?.Release();
            visibleAABBBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, visibleAABBIndices.Count, sizeof(int));
        }
        visibleAABBBuffer.SetData(visibleAABBIndices);
        visibilityCS.SetBuffer(kernel, "VisibleAABBIndices", visibleAABBBuffer);
        visibilityCS.SetInt("VisibleAABBCount", visibleAABBIndices.Count);

        // Dispatch compute shader per renderer
        foreach (var r in renderers)
        {
            int cnt = r.PointCount;
            if (cnt == 0) continue;

            visibilityCS.SetInt("Count", cnt);
            visibilityCS.SetMatrix("LocalToWorld", r.transform.localToWorldMatrix);
            visibilityCS.SetBuffer(kernel, "PositionBuffer", r.PositionBuffer);
            visibilityCS.SetBuffer(kernel, "MemoryBuffer", r.MemoryBuffer);
            visibilityCS.SetBuffer(kernel, "VisibilityBuffer", r.VisibilityBuffer);
            visibilityCS.SetBuffer(kernel, "FinalPositionBuffer", r.FinalPositionBuffer);
            visibilityCS.SetBuffer(kernel, "PrevRayDirBuffer", r.PrevRayDirBuffer);
            visibilityCS.SetBuffer(kernel, "PrevVisibilityBuffer", r.PrevVisibilityBuffer);

            int groups = Mathf.CeilToInt(cnt / 64f);
            visibilityCS.Dispatch(kernel, groups, 1, 1);
        }
    }

    void OnDestroy()
    {
        visibleAABBBuffer?.Release();
    }
}
