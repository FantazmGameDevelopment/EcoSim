// Simple shader for displaying info grids on terrain
// transparant, nolighting, noculling, no z-writing.
Shader "EcoSim/DisplayGrid" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Main Texture", 2D) = "white" {  }
	}
	
	SubShader {
		Tags {
			"Queue" = "Transparent"
			"IgnoreProjector"="True"
		}
		Cull Off
		ZWrite Off
		
		Pass {
			Lighting Off
			Blend SrcAlpha OneMinusSrcAlpha
			Offset -20, -1
			
			SetTexture [_MainTex] {
				constantColor [_Color]						
				combine constant * texture double, texture
			}
		}
	}	


	Fallback Off
}
