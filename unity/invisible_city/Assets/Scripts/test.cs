// Assets/Scripts/PointCloudRenderer.cs
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class test: MonoBehaviour
{
    [Tooltip("Assign YourMesh_D100.asset here")]
    public PointCloudData data;
    [Tooltip("Must match your VFX Graph buffer name")]
    public string bufferProperty = "Buffer";

    VisualEffect   vfx;
    GraphicsBuffer gfxBuffer;
    int            bufferID;

    void Awake()
    {
        vfx      = GetComponent<VisualEffect>();
        bufferID = Shader.PropertyToID(bufferProperty);
    }

    void Start()
    {
        var pts = data.positions;
        // Create a structured GPU buffer for Vector3 positions
        gfxBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pts.Length, 3 * sizeof(float));
        gfxBuffer.SetData(pts);

        // DEBUG: Read back and log the first few points
        int sampleCount = Mathf.Min(5, pts.Length);
        var sample = new Vector3[sampleCount];
        gfxBuffer.GetData(sample, 0, 0, sampleCount);
        for (int i = 0; i < sampleCount; i++)
            Debug.Log($"[GPU Buffer] point[{i}] = {sample[i]}");

        // Bind to VFX Graph and play
        vfx.SetGraphicsBuffer(bufferID, gfxBuffer);
        vfx.Play();
    }

    void OnDestroy()
    {
        gfxBuffer?.Release();
    }
}
