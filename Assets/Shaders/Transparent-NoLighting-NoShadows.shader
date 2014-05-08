Shader "Ctrl-J/Transparent-NoLighting-NoShadows" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Main Texture", 2D) = "white" {  }
		_Cutoff ("Alpha cutoff", Range(0.25,0.75)) = 0.25
	}
	
	SubShader {
		Tags {
			"Queue" = "Transparent-500"
			"IgnoreProjector"="True"
		}
		Cull Back
		ColorMask RGB
		
		Pass {
			Lighting Off
			BindChannels {
				Bind "Color", color
				Bind "Vertex", vertex
				Bind "TexCoord", texcoord
			}			
			AlphaTest GEqual [_Cutoff]
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Offset -2, -1
			
			SetTexture [_MainTex] {
				constantColor [_Color]						
				combine constant * texture double, texture
			}
		}
	}	


	Fallback Off
}
