// Assets/Scripts/BuildingPointCloudRenderer.cs
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class BuildingPointCloudRenderer : MonoBehaviour
{
    [Header("Point-cloud data (baked asset)")]
    [SerializeField] PointCloudData pointCloudData;

    [Header("Visual settings")]
    [SerializeField] float  baseSize   = 0.02f;
    [SerializeField] Color  pointColor = Color.white;

    GraphicsBuffer positionBuffer;
    GraphicsBuffer visibleBuffer;
    GraphicsBuffer memoryBuffer;
    VisualEffect   vfx;
    int            pointCount;


    // Shader property IDs
    static readonly int ID_PositionBuffer = Shader.PropertyToID("PositionBuffer");
    static readonly int ID_VisibleBuffer  = Shader.PropertyToID("VisibleBuffer");
    static readonly int ID_SpawnCount     = Shader.PropertyToID("SpawnCount");
    static readonly int ID_BaseSize       = Shader.PropertyToID("BaseSize");
    static readonly int ID_PointColor     = Shader.PropertyToID("PointColor");

    // ─── Public getters for the visibility manager ─────────────────────────
    public GraphicsBuffer PositionBuffer => positionBuffer;
    public GraphicsBuffer VisibleBuffer  => visibleBuffer;
    
    public GraphicsBuffer MemoryBuffer  => memoryBuffer;
    public int PointCount => pointCount;
    // ──────────────────────────────────────────────────────────────────────

    void Awake()
    {
        vfx = GetComponent<VisualEffect>();
        pointCount = pointCloudData.positions.Length;

        // Position buffer
        positionBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Structured,
            pointCount,
            sizeof(float) * 3);
        positionBuffer.SetData(pointCloudData.positions);
        vfx.SetGraphicsBuffer(ID_PositionBuffer, positionBuffer);
        vfx.SetUInt(ID_SpawnCount, (uint)pointCount);

        // Visibility buffer (initialize with ones)
        visibleBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Structured,
            pointCount,
            sizeof(uint));
        uint[] ones = new uint[pointCount];
        System.Array.Fill(ones, 1u);
        visibleBuffer.SetData(ones);
        vfx.SetGraphicsBuffer(ID_VisibleBuffer, visibleBuffer);

        // memory buffer
        memoryBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Structured, pointCount, sizeof(float));
        float[] init = new float[pointCount];                 // all 0.0f
        memoryBuffer.SetData(init);
        vfx.SetGraphicsBuffer(Shader.PropertyToID("MemoryBuffer"), memoryBuffer);


        // Style parameters
        vfx.SetFloat(ID_BaseSize, baseSize);
        vfx.SetVector4(ID_PointColor, pointColor);

        // Optionally hide the original mesh
        if (TryGetComponent<MeshRenderer>(out var mr)) mr.enabled = false;

        // Spawn the cloud
        vfx.SendEvent("SpawnEvent");
    }

    void LateUpdate()
    {
        // refill visibility buffer each frame (will be overwritten by compute shader later)
        uint[] flags = new uint[pointCount];
        System.Array.Fill(flags, 1u);
        visibleBuffer.SetData(flags);
    }

    void OnDestroy()
    {
        positionBuffer?.Release();
        visibleBuffer?.Release();
        memoryBuffer?.Release();
    }
}
