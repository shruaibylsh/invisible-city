using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;  // for GraphicsFormat

public class DepthCaptureFeature : ScriptableRendererFeature
{
    // Inject this pass after opaque geometry
    class DepthPass : ScriptableRenderPass
    {
        const string PROFILER_TAG = "Depth Copy Pass";
        static readonly int k_DepthTexID = Shader.PropertyToID("_CustomDepthTexture");

        public DepthPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

        // MUST be a class (not a struct) for AddRasterRenderPass<T> :contentReference[oaicite:0]{index=0}
        class PassData
        {
            internal TextureHandle sourceDepth;
            internal TextureHandle depthCopy;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // 1) Grab the camera’s built-in depth texture handle
            var resources = frameData.Get<UniversalResourceData>();
            var srcDepth = resources.activeDepthTexture;

            // 2) Build a RenderTextureDescriptor that matches the camera target, but as R32_SFloat
            var camDesc = frameData
                .Get<UniversalCameraData>()
                .cameraTargetDescriptor;
            camDesc.graphicsFormat   = GraphicsFormat.R32_SFloat; // 32-bit float red channel for depth :contentReference[oaicite:1]{index=1}
            camDesc.depthBufferBits  = 0;
            camDesc.msaaSamples      = 1;
            camDesc.enableRandomWrite = false;
            camDesc.sRGB              = false;

            // 3) Allocate a Render Graph texture with that descriptor
            var depthCopy = UniversalRenderer
                .CreateRenderGraphTexture(renderGraph, camDesc, "DepthCopy", false);

            // 4) Declare and record the copy pass
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG, out var passData))
            {
                passData.sourceDepth = srcDepth;
                passData.depthCopy   = depthCopy;

                // we will read the camera depth
                builder.UseTexture(passData.sourceDepth, AccessFlags.Read);
                // and write into our RFloat texture
                builder.SetRenderAttachment(passData.depthCopy, 0, AccessFlags.Write);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    // Blit the depth into the bound render target’s red channel
                    // using the built-in Blitter. It writes into the currently bound attachment :contentReference[oaicite:2]{index=2}
                    Blitter.BlitTexture(ctx.cmd,
                                        data.sourceDepth,
                                        new Vector4(1, 1, 0, 0),
                                        0,
                                        false);
                });

                // 5) Make it available globally after this pass
                builder.SetGlobalTextureAfterPass(passData.depthCopy, k_DepthTexID);
            }
        }
    }

    DepthPass m_Pass;
    public override void Create() => m_Pass = new DepthPass(RenderPassEvent.AfterRenderingOpaques);
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // only for game cameras
        if (renderingData.cameraData.cameraType == CameraType.Game)
            renderer.EnqueuePass(m_Pass);
    }
}
