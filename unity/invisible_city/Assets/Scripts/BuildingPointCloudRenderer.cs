using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class BuildingPointCloudRenderer : MonoBehaviour
{
    [Header("Baked point-cloud asset")]
    [SerializeField] PointCloudData pointCloudData;
    public PointCloudData PointCloudData => pointCloudData;

    [Header("Tint colour")]
    [SerializeField] Color pointTint = Color.white;

    public GraphicsBuffer PositionBuffer => positionBuffer;
    public GraphicsBuffer MemoryBuffer   => memoryBuffer;
    public GraphicsBuffer FinalPositionBuffer => finalPositionBuffer;
    public int            PointCount    => pointCount;

    GraphicsBuffer positionBuffer;
    GraphicsBuffer memoryBuffer;
    GraphicsBuffer offsetBuffer;  // not needed anymore if we always output FinalPosition
    GraphicsBuffer finalPositionBuffer;
    VisualEffect   vfx;
    int            pointCount;

    static readonly int ID_PositionBuffer = Shader.PropertyToID("PositionBuffer");
    static readonly int ID_MemoryBuffer   = Shader.PropertyToID("MemoryBuffer");
    static readonly int ID_FinalPositionBuffer = Shader.PropertyToID("FinalPositionBuffer");
    static readonly int ID_SpawnCount     = Shader.PropertyToID("SpawnCount");
    static readonly int ID_PointTint      = Shader.PropertyToID("PointColor");

    void Awake()
    {
        if (pointCloudData == null || pointCloudData.positions == null || pointCloudData.positions.Length == 0)
        {
            Debug.LogError($"[{name}] PointCloudData is missing or empty"); enabled = false; return;
        }

        pointCount = pointCloudData.positions.Length;
        vfx        = GetComponent<VisualEffect>();

        var meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.enabled = false;

        positionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pointCount, sizeof(float) * 3);
        positionBuffer.SetData(pointCloudData.positions);
        vfx.SetGraphicsBuffer(ID_PositionBuffer, positionBuffer);
        vfx.SetUInt(ID_SpawnCount, (uint)pointCount);

        memoryBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pointCount, sizeof(float));
        var zeros = new float[pointCount];
        memoryBuffer.SetData(zeros);
        vfx.SetGraphicsBuffer(ID_MemoryBuffer, memoryBuffer);

        finalPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pointCount, sizeof(float) * 3);
        var zeros3 = new Vector3[pointCount];
        finalPositionBuffer.SetData(zeros3);
        vfx.SetGraphicsBuffer(ID_FinalPositionBuffer, finalPositionBuffer);

        vfx.SetVector4(ID_PointTint, pointTint);
        vfx.SendEvent("SpawnEvent");
    }

    void OnDestroy()
    {
        positionBuffer?.Release();
        memoryBuffer ?.Release();
        finalPositionBuffer?.Release();
    }
}
