Shader "Ctrl-J/Leaves Rendertex" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,0)
		_MainTex ("Main Texture", 2D) = "white" {}
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.25
		
		// These are here only to provide default values
		_Scale ("Scale", Vector) = (1,1,1,1)
	}
	SubShader {
		Tags { "Queue" = "Transparent-99" }
		Cull Off
		Fog { Mode Off}
		
		Pass {
			CGPROGRAM
			#pragma exclude_renderers gles xbox360 ps3
			#pragma vertex leaves
			#include "SH_Vertex.cginc"
			ENDCG
			
			Lighting Off
			ZWrite On
			
			// We want to do alpha testing on cutoff, but at the same
			// time write 1.0 into alpha. So we multiply alpha by 0.25/cutoff
			// and alpha test on alpha being greater or equal to 1.0.
			// That will work for cutoff values in range [0.25;1].
			// Remember that color gets clamped to [0;1].
			AlphaTest GEqual 1.0
			SetTexture [_MainTex] {
				constantColor (1,1,1,1)			
				combine constant * texture double, primary * texture QUAD
			}
		}
	}
	
	Fallback Off
}
