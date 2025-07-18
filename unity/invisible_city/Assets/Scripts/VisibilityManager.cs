using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine.VFX;

public class PointCloudVisibilityManager : MonoBehaviour
{
    public ComputeShader visibilityCS;
    public BuildingPointCloudRenderer[] renderers;
    [Range(0,1)] public float baseLearnRate  = 0.4f;
    [Range(0,1)] public float baseForgetRate = 0.2f;
    public float cullRadius = 50f;
    public float driftAmplitude = 0.5f;
    public LayerMask occlusionLayer;

    static readonly int ID_Count             = Shader.PropertyToID("Count");
    static readonly int ID_CameraVP          = Shader.PropertyToID("CameraVP");
    static readonly int ID_LocalToWorld      = Shader.PropertyToID("LocalToWorld");
    static readonly int ID_DeltaTime         = Shader.PropertyToID("DeltaTime");
    static readonly int ID_LearnRate         = Shader.PropertyToID("LearnRate");
    static readonly int ID_ForgetRate        = Shader.PropertyToID("ForgetRate");
    static readonly int ID_ScreenSize        = Shader.PropertyToID("ScreenSize");
    static readonly int ID_CamPosition       = Shader.PropertyToID("CameraPosition");
    static readonly int ID_CullRadiusSqr     = Shader.PropertyToID("CullRadiusSqr");
    static readonly int ID_VisibilityBuffer  = Shader.PropertyToID("VisibilityBuffer");
    static readonly int ID_FinalPositionBuffer = Shader.PropertyToID("FinalPositionBuffer");
    static readonly int ID_Amplitude         = Shader.PropertyToID("Amplitude");

    Camera cam;
    int kernel;
    GraphicsBuffer visibilityBuffer;
    const float epsilon = 0.01f;

    void Awake()
    {
        cam    = Camera.main;
        kernel = visibilityCS.FindKernel("CSMain");
    }

    void LateUpdate()
    {
        if (visibilityCS == null || kernel < 0) return;

        Matrix4x4 vp = cam.projectionMatrix * cam.worldToCameraMatrix;
        Vector2   screenSize = new Vector2(Screen.width, Screen.height);
        float     dt = Time.deltaTime;
        Vector3   camPos = cam.transform.position;
        float     radiusSqr = cullRadius * cullRadius;

        visibilityCS.SetVector (ID_ScreenSize, screenSize);
        visibilityCS.SetFloat  (ID_DeltaTime,  dt);
        visibilityCS.SetFloat  (ID_LearnRate,  baseLearnRate);
        visibilityCS.SetFloat  (ID_ForgetRate, baseForgetRate);
        visibilityCS.SetMatrix (ID_CameraVP,   vp);
        visibilityCS.SetVector (ID_CamPosition, camPos);
        visibilityCS.SetFloat  (ID_CullRadiusSqr, radiusSqr);
        visibilityCS.SetFloat  (ID_Amplitude, driftAmplitude);

        foreach (var r in renderers)
        {
            int cnt = r.PointCount;
            if (cnt == 0) continue;

            var positions = r.PointCloudData.positions;
            NativeArray<RaycastCommand> commands = new NativeArray<RaycastCommand>(cnt, Allocator.TempJob);
            NativeArray<RaycastHit>     results  = new NativeArray<RaycastHit>(cnt, Allocator.TempJob);
            var visibilityData = new NativeArray<int>(cnt, Allocator.TempJob);

            var queryParams = new QueryParameters(occlusionLayer, false);

            for (int i = 0; i < cnt; i++)
            {
                Vector3 worldPos = r.transform.TransformPoint(positions[i]);
                Vector3 dir = worldPos - camPos;
                float distToPoint = dir.magnitude;

                if (distToPoint * distToPoint > radiusSqr)
                {
                    visibilityData[i] = 0;
                    commands[i] = default;
                    continue;
                }

                commands[i] = new RaycastCommand(camPos, dir.normalized, queryParams, distToPoint);
            }

            JobHandle handle = RaycastCommand.ScheduleBatch(commands, results, 32);
            handle.Complete();

            for (int i = 0; i < cnt; i++)
            {
                Vector3 worldPos = r.transform.TransformPoint(positions[i]);
                Vector3 dir = worldPos - camPos;
                float distToPoint = dir.magnitude;

                if (distToPoint * distToPoint > radiusSqr)
                {
                    visibilityData[i] = 0;
                    continue;
                }

                if (results[i].collider != null)
                    visibilityData[i] = Mathf.Abs(results[i].distance - distToPoint) < epsilon ? 1 : 0;
                else
                    visibilityData[i] = 1;
            }

            visibilityBuffer?.Release();
            visibilityBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, cnt, sizeof(int));
            visibilityBuffer.SetData(visibilityData.ToArray());

            int groups = Mathf.CeilToInt(cnt / 64f);

            visibilityCS.SetInt   (ID_Count, cnt);
            visibilityCS.SetMatrix(ID_LocalToWorld, r.transform.localToWorldMatrix);
            visibilityCS.SetBuffer(kernel, "PositionBuffer", r.PositionBuffer);
            visibilityCS.SetBuffer(kernel, "MemoryBuffer",   r.MemoryBuffer);
            visibilityCS.SetBuffer(kernel, "VisibilityBuffer", visibilityBuffer);
            visibilityCS.SetBuffer(kernel, "FinalPositionBuffer", r.FinalPositionBuffer);
            visibilityCS.SetFloat("GlobalTime", Time.time);

            visibilityCS.Dispatch(kernel, groups, 1, 1);

            commands.Dispose();
            results.Dispose();
            visibilityData.Dispose();
        }
    }

    void OnDestroy()
    {
        visibilityBuffer?.Release();
    }
}
