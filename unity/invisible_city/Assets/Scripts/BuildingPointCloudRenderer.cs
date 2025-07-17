using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class BuildingPointCloudRenderer : MonoBehaviour
{
    [Header("Point-cloud data (baked asset)")]
    [SerializeField] PointCloudData pointCloudData;
    public PointCloudData PointCloudData => pointCloudData;

    [Header("Visual settings")]
    [SerializeField] Color pointColor = Color.white;

    GraphicsBuffer positionBuffer;
    GraphicsBuffer memoryBuffer;
    GraphicsBuffer visibleBuffer;
    VisualEffect   vfx;
    int            pointCount;

    static readonly int ID_PositionBuffer = Shader.PropertyToID("PositionBuffer");
    static readonly int ID_MemoryBuffer   = Shader.PropertyToID("MemoryBuffer");
    static readonly int ID_VisibleBuffer  = Shader.PropertyToID("VisibleBuffer");
    static readonly int ID_SpawnCount     = Shader.PropertyToID("SpawnCount");
    static readonly int ID_PointColor     = Shader.PropertyToID("PointColor");

    // Exposed for the visibility manager
    public GraphicsBuffer PositionBuffer => positionBuffer;
    public GraphicsBuffer MemoryBuffer   => memoryBuffer;
    public GraphicsBuffer VisibleBuffer  => visibleBuffer;
    public int PointCount => pointCount;

    void Awake()
    {
        vfx = GetComponent<VisualEffect>();
        pointCount = pointCloudData.positions.Length;
        if (pointCount == 0)
        {
            Debug.LogError($"[{name}] pointCloudData.positions is empty!");
            enabled = false;
            return;
        }

        // 1) Position buffer
        positionBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Structured,
            pointCount,
            sizeof(float) * 3
        );
        positionBuffer.SetData(pointCloudData.positions);
        vfx.SetGraphicsBuffer(ID_PositionBuffer, positionBuffer);
        vfx.SetUInt(ID_SpawnCount, (uint)pointCount);

        // 2) Memory buffer (initialize all to 1 = fully “learned”)
        memoryBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Structured,
            pointCount,
            sizeof(float)
        );
        var ones = new float[pointCount];
        for (int i = 0; i < pointCount; i++) ones[i] = 1f;
        memoryBuffer.SetData(ones);
        vfx.SetGraphicsBuffer(ID_MemoryBuffer, memoryBuffer);

        // 3) Visible buffer (initialize all to 1 = start fully visible)
        visibleBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Structured,
            pointCount,
            sizeof(uint)
        );
        var initialVis = new uint[pointCount];
        for (int i = 0; i < pointCount; i++) initialVis[i] = 1u;
        visibleBuffer.SetData(initialVis);
        vfx.SetGraphicsBuffer(ID_VisibleBuffer, visibleBuffer);

        // 4) Color parameter (drives alpha)
        vfx.SetVector4(ID_PointColor, pointColor);

        // 5) Spawn
        vfx.SendEvent("SpawnEvent");
    }

    void OnDestroy()
    {
        positionBuffer?.Release();
        memoryBuffer?.Release();
        visibleBuffer?.Release();
    }
}
