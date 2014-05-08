Shader "Ctrl-J/TileSelect" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
	}
	
	SubShader {
		Tags {
			"Queue" = "Transparent-100"
			"IgnoreProjector"="True"
		}
		Cull Back
		ColorMask RGB
		
		Pass {
			Lighting Off
			Offset -25, -1
			ZWrite Off
			Fog { Mode Off }
			Blend SrcAlpha OneMinusSrcAlpha
			Color [_Color]
		}
	}	


	Fallback Off
}
