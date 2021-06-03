// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "COF/NM_AllyFotress"
{
    Properties
    {
		_TexScale("Scale of Tex", 			Float) = 1.0
        _MaskTex("MaskTex (RGB)", 2D)		= "white" {}
        _LineColor("Line Color", Color)		= (1.0,1.0,1.0,1.0)
		_Blend("Blend", Range(0, 1))		= 1.0
		_Speed_X("Speed", Float)			= 1.0
		_UVOffset_Start("_UVOffset_Start", Float) = 0
		_UVOffset_End("_UVOffset_End", Float) = 0

        [Space(20)]
        [KeywordEnum(2Sides, Back, Front)]
        _Cull				( "Culling" , int)			= 2
        [Toggle]
        _IsZWrite			( "ZWrite" , int)			= 0
	}
	
	
	Category 
	{
		Tags { "Queue"="Transparent-2" "IgnoreProjector"="True" "RenderType"="Transparent" }
    	//Blend SrcAlpha OneMinusSrcAlpha
		Blend  One OneMinusSrcColor
		Colormask RGB
		AlphaTest Greater .01
        Lighting Off
        Cull [_Cull]
        ZWrite Off
        Fog {mode off}
        //ZTest NotEqual
        //Colormask RGB
		Offset -5, -5
		SubShader
		{
			Pass 
			{
				//Stencil
				//{
				//	Ref 0
				//	Comp Equal
				//	Pass IncrWrap
				//	ZFail Keep
				//}
				CGPROGRAM
				#pragma target 2.0
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"
				fixed4 _LineColor;
				
				sampler2D _MaskTex;
				float4 _MaskTex_ST;

				fixed  _Blend;
				fixed _Speed_X;
				fixed _UVOffset_Start, _UVOffset_End;
				float _TexScale;

				struct appdata_t
				{
					float4 vertex	: POSITION;
					float2 texcoord : TEXCOORD0;
				};
				
				
				struct v2f
				{
					fixed4 vertex	: POSITION;
					float2 uv		: TEXCOORD0;
				};
				float3x3 getXYTranslationMatrix(float2 translation) {
					return float3x3(1, 0, translation.x, 0, 1, translation.y, 0, 0, 1);
				}

				float3x3 getXYRotationMatrix(float theta) {
					float s = -sin(theta);
					float c = cos(theta);
					return float3x3(c, -s, 0, s, c, 0, 0, 0, 1);
				}

				float3x3 getXYScaleMatrix(float2 scale) {
					return float3x3(scale.x, 0, 0, 0, scale.y, 0, 0, 0, 1);
				}

				float2 applyMatrix(float3x3 m, float2 uv) {
					return mul(m, float3(uv.x, uv.y, 1)).xy;
				}
				v2f vert(appdata_t v)
				{
					v2f o;
					
					o.vertex = UnityObjectToClipPos(v.vertex);
					float maxHeight = 250;
					float min = -400;
					//float s = (maxHeight - _WorldSpaceCameraPos.y) / (maxHeight);
					float s = saturate((min - _WorldSpaceCameraPos.y * 0.65) / (min - maxHeight));
					//float s = saturate(maxHeight - _WorldSpaceCameraPos.y / (maxHeight - 50);
					float offset = lerp(_UVOffset_Start * 0.01, _UVOffset_End * 0.01, s);
					s = lerp(1, 0.02, s); //_TexScale
					//s = 1;
					float time = fmod(_Time, 1);

					o.uv.xy = applyMatrix(
						getXYTranslationMatrix(float2(0.5, 0.03)),
						applyMatrix( // scale
							getXYScaleMatrix(float2(1.2, s)),
							applyMatrix( // rotate
								getXYRotationMatrix(0),
								applyMatrix(
									getXYTranslationMatrix(float2(-0.5, -0.03)),
									(v.texcoord.xy)
								) 
							)
						)
					) + float2(_Speed_X * time, offset);// +float2(0, offset);
					
					return o;
				}
				
				
				fixed4 frag(v2f i) : COLOR
				{
					fixed4 finalColor = fixed4 (0,0,0,0);
					
					fixed4 maskTex = tex2D (_MaskTex,i.uv);
					finalColor.rgb = pow((_LineColor.rgb * maskTex.rgb * maskTex.r) * (1 + _LineColor.b * 2), 1.5) * 2;
					finalColor.a = maskTex.r * _LineColor.a;

					clip(finalColor.a - 0.1);
     				return finalColor;
				}
				ENDCG
			}
		}
	}
}
