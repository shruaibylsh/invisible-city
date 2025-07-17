using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

public class DepthCaptureFeature : ScriptableRendererFeature
{
    class DepthPass : ScriptableRenderPass
    {
        const string PROFILER_TAG = "Depth Copy Pass";
        static readonly int k_DepthTexID = Shader.PropertyToID("_CustomDepthTexture");

        Material depthCopyMaterial;

        public DepthPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;

            // Create the material using URP's internal copy shader
            Shader copyShader = Shader.Find("Hidden/Universal Render Pipeline/CopyDepth");
            if (copyShader != null)
                depthCopyMaterial = new Material(copyShader);
            else
                Debug.LogError("Failed to find CopyDepth shader!");
        }

        class PassData
        {
            internal TextureHandle sourceDepth;
            internal TextureHandle depthCopy;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            var srcDepth = resources.activeDepthTexture;

            var camDesc = frameData.Get<UniversalCameraData>().cameraTargetDescriptor;
            camDesc.graphicsFormat = GraphicsFormat.R32_SFloat;
            camDesc.depthBufferBits = 0;
            camDesc.msaaSamples = 1;
            camDesc.enableRandomWrite = false;
            camDesc.sRGB = false;

            var depthCopy = UniversalRenderer.CreateRenderGraphTexture(renderGraph, camDesc, "DepthCopy", false);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(PROFILER_TAG, out var passData))
            {
                passData.sourceDepth = srcDepth;
                passData.depthCopy = depthCopy;

                builder.UseTexture(passData.sourceDepth, AccessFlags.Read);
                builder.SetRenderAttachment(passData.depthCopy, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    if (depthCopyMaterial != null)
                    {
                        ctx.cmd.SetGlobalTexture("_SourceTex", data.sourceDepth);
                        CoreUtils.DrawFullScreen(ctx.cmd, depthCopyMaterial);
                    }
                    else
                    {
                        Debug.LogError("DepthCopy material is null. Cannot copy depth.");
                    }
                });

                builder.SetGlobalTextureAfterPass(passData.depthCopy, k_DepthTexID);
            }
        }
    }

    DepthPass m_Pass;

    public override void Create()
    {
        m_Pass = new DepthPass(RenderPassEvent.AfterRenderingOpaques);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
            renderer.EnqueuePass(m_Pass);
    }
}
