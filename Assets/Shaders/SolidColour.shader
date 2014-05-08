Shader "Ctrl-J/SolidColour" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
	}
	
	SubShader {
		Tags {
			"RenderType" = "Opaque"
			"IgnoreProjector"="True"
		}
		Cull Back
		ColorMask RGB
		
		Pass {
			Lighting Off
			ZWrite On
			Fog { Mode Off }
			Blend One Zero
			Color [_Color]
		}
	}	


	Fallback Off
}
