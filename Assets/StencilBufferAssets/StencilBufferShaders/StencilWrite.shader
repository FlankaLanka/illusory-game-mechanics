Shader "Custom/StencilWrite"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            // Stencil settings
            Stencil
            {
                Ref 1              
                Comp always        
                Pass replace       
            }

            // Basic rendering
            ColorMask 0           
        }
    }
}
