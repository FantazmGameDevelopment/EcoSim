Shader "Ctrl-J/Leaves" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Main Texture", 2D) = "white" {  }
		_Cutoff ("Alpha cutoff", Range(0.25,0.75)) = 0.25
	}
	
	SubShader {
		Tags {
			"Queue" = "Transparent-99"
			"IgnoreProjector"="True"
			"RenderType" = "TreeTransparentCutout"
		}
		Cull Off
		ColorMask RGB
		
		
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			Fog {Mode Off}
			ZWrite On ZTest Less Cull Off
			Offset 1, 1
	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_shadowcaster
			#include "UnityCG.cginc"
			#include "TerrainEngine.cginc"
			
			struct v2f { 
				V2F_SHADOW_CASTER;
				float2 uv : TEXCOORD1;
			};
			
			struct appdata {
			    float4 vertex : POSITION;
			    fixed4 color : COLOR;
			    float4 texcoord : TEXCOORD0;
			};
			v2f vert( appdata v )
			{
				v2f o;
				TerrainAnimateTree(v.vertex, v.color.w);
				TRANSFER_SHADOW_CASTER(o)
				o.uv = v.texcoord;
				return o;
			}
			
			sampler2D _MainTex;
			fixed _Cutoff;
					
			float4 frag( v2f i ) : COLOR
			{
				fixed4 texcol = tex2D( _MainTex, i.uv );
				clip( texcol.a - _Cutoff );
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG	
		}
	}
	
	SubShader {
		Tags {
			"Queue" = "Transparent-99"
			"IgnoreProjector"="True"
			"RenderType" = "TreeTransparentCutout"
		}
		Cull Off
		ColorMask RGB
		
		Pass {
			CGPROGRAM
// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it does not contain a surface program or both vertex and fragment programs.
			#pragma exclude_renderers gles
			#pragma vertex leaves
			#include "SH_Vertex.cginc"
			ENDCG

			Lighting Off
			BindChannels {
				Bind "Color", color
				Bind "Vertex", vertex
				Bind "TexCoord", texcoord
			}			
			AlphaTest GEqual [_Cutoff]
			ZWrite On
			
			SetTexture [_MainTex] {
				constantColor (1,1,1,1)						
				combine primary * texture double, texture
			}
		}
	}	

	SubShader {
		Tags {
			"Queue" = "Transparent-99"
			"IgnoreProjector"="True"
			"RenderType" = "TreeTransparentCutout"
		}
		Cull Off
		ColorMask RGB
		
		Pass {
			Lighting Off
			BindChannels {
				Bind "Color", color
				Bind "Vertex", vertex
				Bind "TexCoord", texcoord
			}			
			AlphaTest GEqual [_Cutoff]
			ZWrite On
			
			SetTexture [_MainTex] {
				constantColor (1,1,1,1)						
				combine primary	 * texture double, texture
			}
		}
	}	


	Dependency "BillboardShader" = "Ctrl-J/Leaves Rendertex"
	Fallback Off
}
