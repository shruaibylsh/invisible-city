using UnityEngine;
using System.Collections.Generic;

public class PointCloudVisibilityManager : MonoBehaviour
{
    public Camera[] agents;

    public ComputeShader visibilityCS;
    public BuildingPointCloudRenderer[] renderers;
    [Range(0.5f, 1.5f)] public float baseLearnRate = 0.9f;
    [Range(0f, 0.5f)] public float baseForgetRate = 0.15f;
    public float cullRadius = 50f;
    public float driftAmplitude = 0.15f;

    int kernel;
    int mergeKernel;
    public ComputeShader mergeMemoryCS; 
    List<int> visibleAABBIndices = new List<int>();
    GraphicsBuffer visibleAABBBuffer;

    void Awake()
    {
        kernel = visibilityCS.FindKernel("CSMain");
        mergeKernel = mergeMemoryCS.FindKernel("MergeMemory");
    }

    void Start()
    {
        foreach (var r in renderers)
            r.InitAgentBuffers(agents.Length);
    }

    void LateUpdate()
    {
        if (visibilityCS == null || agents.Length == 0) return;

        float radiusSqr = cullRadius * cullRadius;
        visibilityCS.SetFloat("DeltaTime", Time.deltaTime);
        visibilityCS.SetFloat("LearnRate", baseLearnRate);
        visibilityCS.SetFloat("ForgetRate", baseForgetRate);
        visibilityCS.SetFloat("CullRadiusSqr", radiusSqr);
        visibilityCS.SetFloat("Amplitude", driftAmplitude);
        visibilityCS.SetFloat("GlobalTime", Time.time);
        visibilityCS.SetVector("ScreenSize", new Vector2(Screen.width, Screen.height));

        var aabbList = BuildingTriangleBufferWithAABB.Instance.AABBList;

        for (int agentIdx = 0; agentIdx < agents.Length; agentIdx++)
        {
            var cam = agents[agentIdx];
            if (!cam) continue;

            Matrix4x4 vp = cam.projectionMatrix * cam.worldToCameraMatrix;
            Vector3 camPos = cam.transform.position;
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(cam);

            visibleAABBIndices.Clear();
            for (int i = 0; i < aabbList.Count; i++)
            {
                var aabb = aabbList[i];
                Bounds b = new Bounds();
                b.SetMinMax(aabb.min, aabb.max);

                if ((camPos - b.center).sqrMagnitude <= radiusSqr && GeometryUtility.TestPlanesAABB(frustumPlanes, b))
                    visibleAABBIndices.Add(i);
            }

            if (visibleAABBBuffer == null || visibleAABBBuffer.count < visibleAABBIndices.Count)
            {
                visibleAABBBuffer?.Release();
                visibleAABBBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, visibleAABBIndices.Count, sizeof(int));
            }
            visibleAABBBuffer.SetData(visibleAABBIndices);

            visibilityCS.SetBuffer(kernel, "VisibleAABBIndices", visibleAABBBuffer);
            visibilityCS.SetInt("VisibleAABBCount", visibleAABBIndices.Count);
            visibilityCS.SetMatrix("CameraVP", vp);
            visibilityCS.SetVector("CameraPosition", camPos);

            foreach (var r in renderers)
            {
                int cnt = r.PointCount;
                if (cnt == 0) continue;

                visibilityCS.SetInt("Count", cnt);
                visibilityCS.SetMatrix("LocalToWorld", r.transform.localToWorldMatrix);
                visibilityCS.SetBuffer(kernel, "PositionBuffer", r.PositionBuffer);
                visibilityCS.SetBuffer(kernel, "FinalPositionBuffer", r.FinalPositionBuffer);
                visibilityCS.SetBuffer(kernel, "VisibilityBuffer", r.VisibilityBuffer);
                visibilityCS.SetBuffer(kernel, "PrevRayDirBuffer", r.PrevRayDirBuffer);
                visibilityCS.SetBuffer(kernel, "PrevVisibilityBuffer", r.PrevVisibilityBuffer);
                visibilityCS.SetBuffer(kernel, "MemoryBuffer", r.AgentMemoryBuffers[agentIdx]);

                int groups = Mathf.CeilToInt(cnt / 64f);
                visibilityCS.Dispatch(kernel, groups, 1, 1);
            }
        }

        // Merge memory on GPU
        foreach (var r in renderers)
        {
            int cnt = r.PointCount;
            mergeMemoryCS.SetInt("Count", cnt);
            mergeMemoryCS.SetBuffer(mergeKernel, "MergedMemory", r.MemoryBuffer);
            mergeMemoryCS.SetBuffer(mergeKernel, "AgentMemory0", r.AgentMemoryBuffers[0]);
            mergeMemoryCS.SetBuffer(mergeKernel, "AgentMemory1", r.AgentMemoryBuffers[1]);
            mergeMemoryCS.SetBuffer(mergeKernel, "AgentMemory2", r.AgentMemoryBuffers[2]);
            mergeMemoryCS.SetBuffer(mergeKernel, "AgentMemory3", r.AgentMemoryBuffers[3]);
            mergeMemoryCS.SetBuffer(mergeKernel, "AgentMemory4", r.AgentMemoryBuffers[4]);
            mergeMemoryCS.SetBuffer(mergeKernel, "AgentMemory5", r.AgentMemoryBuffers[5]);

            int groups = Mathf.CeilToInt(cnt / 64f);
            mergeMemoryCS.Dispatch(mergeKernel, groups, 1, 1);
        }

    }

    void OnDestroy()
    {
        visibleAABBBuffer?.Release();
        foreach (var r in renderers)
            r.ReleaseAgentBuffers();
    }
}
