Shader "Hidden/Opaque"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        Pass
        {
            Name "DepthOnly"
            ZWrite On
            ColorMask 0   // nothing in the colour buffer
        }
    }
}
