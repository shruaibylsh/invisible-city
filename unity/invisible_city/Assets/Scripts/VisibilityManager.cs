using UnityEngine;
using System.Collections.Generic;

public class PointCloudVisibilityManager : MonoBehaviour
{
    [System.Serializable]
    public class AgentCamera
    {
        public Camera cam;
    }

    public ComputeShader visibilityCS;
    public AgentCamera[] agents;
    public BuildingPointCloudRenderer[] renderers;
    [Range(0, 1)] public float baseLearnRate = 0.4f;
    [Range(0, 1)] public float baseForgetRate = 0.2f;
    public float cullRadius = 50f;
    public float driftAmplitude = 0.15f;

    int kernel;
    List<int> visibleAABBIndices = new List<int>();
    GraphicsBuffer visibleAABBBuffer;

void Start()
{
    foreach (var r in renderers)
    {
        int count = r.PointCount;
        if (count <= 0)
        {
            Debug.LogError($"[{r.name}] Skipping InitAgentBuffers — PointCount is 0");
            continue;
        }

        r.InitAgentBuffers(agents.Length);
        Debug.Log($"[{r.name}] InitAgentBuffers successful with {count} points × {agents.Length} agents.");
    }
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
            var cam = agents[agentIdx].cam;
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

        // Combine memory from all agents, clamped to 1
        foreach (var r in renderers)
        {
            int cnt = r.PointCount;
            float[] combined = new float[cnt];
            float[] temp = new float[cnt];

            for (int a = 0; a < agents.Length; a++)
            {
                r.AgentMemoryBuffers[a].GetData(temp);
                for (int i = 0; i < cnt; i++)
                    combined[i] += temp[i];
            }

            for (int i = 0; i < cnt; i++)
                combined[i] = Mathf.Min(1f, combined[i]);

                r.MemoryBuffer.SetData(combined);
        }
    }

    void OnDestroy()
    {
        visibleAABBBuffer?.Release();
        foreach (var r in renderers)
        {
            r.ReleaseAgentBuffers();
        }
    }
}
