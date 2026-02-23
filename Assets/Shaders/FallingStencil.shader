Shader "HoleGame/FallingStencil"
{
    Properties
    {
        _Color ("Color", Color) = (1.0, 0.55, 0.2, 1.0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "FallingStencilURP"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Back
            ZWrite Off
            ZTest Always
            Blend Off

            Stencil
            {
                Ref 1
                Comp Equal
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            half4 _Color;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            Name "FallingStencilBuiltin"

            Cull Back
            ZWrite Off
            ZTest Always
            Blend Off

            Stencil
            {
                Ref 1
                Comp Equal
                Pass Keep
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }

    FallBack Off
}
