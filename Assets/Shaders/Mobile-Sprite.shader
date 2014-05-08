Shader "Ctrl-J/Mobile/Sprite"
{
    Properties
    {
        _MainTex ("Base (RGB) Trans. (Alpha)", 2D) = "white" { }
		
    }

	SubShader
	{
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		Pass
		{
			ZWrite Off
			ZTest Always  
			Alphatest Off
			Fog { Mode Off }
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off
			Lighting Off
			SetTexture [_MainTex]
			{
				combine texture, texture
			} 
		}
	} 
}