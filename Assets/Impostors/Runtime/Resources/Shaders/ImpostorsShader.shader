Shader "Impostors/ImpostorsShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" { }
    }

    // UniversalPipeline shader must be at the top...
    SubShader
    {
        Name "UniversalPipeline"
        Tags
        {
            "RenderType" = "TransparentCutout"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ImpostorsUnlitCutout"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            ZWrite On
            ZTest LEqual
            Cull Back
            AlphaToMask On

            CGPROGRAM
            #include "Impostors.cginc"

            #pragma multi_compile_fog
            #pragma vertex impostors_vert
            #pragma fragment impostors_frag
            ENDCG

        }

        Pass
        {
            Name "ImpostorsDepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ZTest LEqual
            Cull Back

            CGPROGRAM
            #include "Impostors.cginc"

            #pragma vertex impostors_vert
            #pragma fragment impostors_frag
            ENDCG

        }
    }

    SubShader
    {
        Name "StandardPipeline"
        Tags
        {
            "Queue" = "AlphaTest" // actual queue number sets from ImpostorsChunk
            "IgnoreProjector" = "True"
            "RenderType" = "TransparentCutout"
        }

        Lighting Off

        Pass
        {
            Name "ImpostorsUnlitCutout"
            Tags
            {
                "LightMode" = "ForwardBase"
                "ForceNoShadowCasting" = "True"
            }
            ZWrite On
            ZTest LEqual
            Cull Back
            AlphaToMask On

            CGPROGRAM
            #include "Impostors.cginc"

            #pragma multi_compile_fog
            #pragma vertex impostors_vert
            #pragma fragment impostors_frag
            ENDCG
        }

        Pass
        {
            Name "ImpostorsDepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }
            ZWrite On
            ZTest LEqual
            Cull Back

            CGPROGRAM
            #include "Impostors.cginc"

            #pragma vertex impostors_vert
            #pragma fragment impostors_frag
            ENDCG
        }

        /*Pass // FOR NOW I DIDN'T FIND A WAY TO DRAW IMPOSTORS IN DEFERRED PATH
        {
            Tags{ "LightMode" = "Deferred"  }
            //Tags{ "Queue" = "Geometry" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" }
            ZWrite On
            ZTest Less
            //Blend SrcAlpha OneMinusSrcAlpha
            //Cull Back
            
            CGPROGRAM
            
            #include "Impostors.cginc"
            
            #pragma vertex impostors_vert
            #pragma fragment impostors_deferred_frag
            #pragma multi_compile _ UNITY_HDR_ON
            
            struct deferred
			{
				half4 albedo : SV_Target0;
				half4 specular : SV_Target1;
				half4 normal : SV_Target2;
				half4 emission : SV_Target3;
			};
            
            void impostors_deferred_frag(v2f i,
                out half4 outDiffuse : SV_Target0,            // RT0: diffuse color (rgb), occlusion (a)
                out half4 outSpecSmoothness : SV_Target1,    // RT1: spec color (rgb), smoothness (a)
                out half4 outNormal : SV_Target2,            // RT2: normal (rgb), --unused, very low precision-- (a)
                out half4 outEmission : SV_Target3 
            ) 
            {
                //outDiffuse = impostors_frag(i);
                #if !defined(UNITY_HDR_ON)
                    half4 emission = impostors_frag(i);
			        emission.rgb = exp2(-emission.rgb);
			        outEmission = emission;
                    outDiffuse = half4(1,1,1,1);
                #else
                    half4 emission = impostors_frag(i);
			        outEmission = emission;
                    outDiffuse = half4(1,1,1,1);
		        #endif
            }
            
            ENDCG
        }*/


    }


}