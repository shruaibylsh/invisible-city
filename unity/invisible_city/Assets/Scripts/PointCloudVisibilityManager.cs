using UnityEngine;

public class PointSceneDepthDebugManager : MonoBehaviour
{
    public ComputeShader debugShader;
    public BuildingPointCloudRenderer[] renderers;
    [Range(1, 20)] public int raysPerRenderer = 5;

    Camera cam;
    int kernel;
    GraphicsBuffer debugBuffer;

    static readonly int ID_Count      = Shader.PropertyToID("Count");
    static readonly int ID_L2W        = Shader.PropertyToID("LocalToWorld");
    static readonly int ID_W2C        = Shader.PropertyToID("WorldToCamera");
    static readonly int ID_VP         = Shader.PropertyToID("CameraVP");
    static readonly int ID_Near       = Shader.PropertyToID("CamNear");
    static readonly int ID_Far        = Shader.PropertyToID("CamFar");
    static readonly int ID_DepthTex   = Shader.PropertyToID("DepthTexture");
    static readonly int ID_ScrW       = Shader.PropertyToID("ScreenWidth");
    static readonly int ID_ScrH       = Shader.PropertyToID("ScreenHeight");

    struct Pair { public float point; public float scene; }

    void Start()
    {
        cam = Camera.main;
        cam.depthTextureMode |= DepthTextureMode.Depth;
        kernel = debugShader.FindKernel("CSMain");
    }

    void LateUpdate()
    {
        Matrix4x4 w2c = cam.worldToCameraMatrix;
        Matrix4x4 vp  = cam.projectionMatrix * w2c;

        debugShader.SetMatrix(ID_W2C, w2c);
        debugShader.SetMatrix(ID_VP, vp);
        debugShader.SetFloat(ID_Near, cam.nearClipPlane);
        debugShader.SetFloat(ID_Far, cam.farClipPlane);
        debugShader.SetInt(ID_ScrW, Screen.width);
        debugShader.SetInt(ID_ScrH, Screen.height);
        debugShader.SetTexture(kernel, ID_DepthTex, Shader.GetGlobalTexture("_CameraDepthTexture"));

        foreach (var r in renderers)
        {
            int count = r.PointCount;
            int groups = Mathf.CeilToInt(count / 64f);

            debugShader.SetInt(ID_Count, count);
            debugShader.SetMatrix(ID_L2W, r.transform.localToWorldMatrix);
            debugShader.SetBuffer(kernel, "PositionBuffer", r.PositionBuffer);
            debugShader.SetBuffer(kernel, "MemoryBuffer", r.MemoryBuffer);

            if (debugBuffer == null || debugBuffer.count != count)
            {
                debugBuffer?.Release();
                debugBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, sizeof(float) * 2);
            }

            debugShader.SetBuffer(kernel, "DebugBuffer", debugBuffer);
            debugShader.Dispatch(kernel, groups, 1, 1);

            int n = Mathf.Min(raysPerRenderer, count);
            Pair[] pairs = new Pair[n];
            debugBuffer.GetData(pairs, 0, 0, n);

            for (int i = 0; i < n; i++)
            {
                Vector3 worldPos = r.transform.TransformPoint(r.PointCloudData.positions[i]);
                Vector3 camPos   = cam.transform.position;

                float pd = pairs[i].point;
                float sd = pairs[i].scene;

                // draw only point-depth ray (green)
                Vector3 dir = (worldPos - camPos).normalized;
                float maxDist = cam.farClipPlane;
                Debug.DrawLine(camPos, camPos + dir * (pd * maxDist), Color.green);

                Debug.Log($"[{r.name}] P{i}: pointDepth={pd:0.000}, sceneDepth={sd:0.000}");
            }
        }
    }

    void OnDestroy()
    {
        debugBuffer?.Release();
    }
}
