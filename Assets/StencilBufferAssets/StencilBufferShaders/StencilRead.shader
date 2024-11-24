Shader "Custom/StencilRead"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { 
            "RenderType"="Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Off
            Stencil
            {
                Ref 1              
                Comp Equal         
                Pass Keep          
            }
            Color [_Color]
        }
    }
}
