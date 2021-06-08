// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "COF/NM_FleetTrail"
{
    Properties
    {
		[Enum(Additive, 0, AlphaBlend, 1, Invert, 2, Custom, 3)]_BlendMode("Blend Mode", Float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("__src", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("__dst", Float) = 1
		_TexScale("Scale of Tex", 			Float) = 1.0
        _LineColor			("Line Color", Color)	= (1.0,1.0,1.0,1.0)
		_LineBright			("Line Bright", Float) = 1
		[NoScaleOffset]
        _MaskTex			("MaskTex (RGB)", 2D)	= "white" {}

		[Space(20)]
		_Count_U			("Count U", Float)		= 1.0
		_Count_V			("Count V", Float)		= 1.0

		_Speed_X			("Speed X", Float)		= 1.0
		_Speed_Y			("Speed Y", Float)		= 0.0

        [Space(20)]
        [KeywordEnum(2Sides, Back, Front)]
        _Cull				( "Culling" , Int)		= 2

        [Toggle]
        _IsZWrite			( "ZWrite" , Int)		= 0
	}
	
	
	Category 
	{
		Tags { "Queue"="Transparent+1" "IgnoreProjector"="True" "RenderType"="Transparent" }
		//Blend SrcAlpha OneMinusSrcAlpha
		Blend[_SrcBlend][_DstBlend]
		AlphaTest Greater .01
        Lighting Off
        Cull [_Cull]
        ZWrite [_IsZWrite]
		ZTest Always
        Fog {mode off}
        
		
		SubShader
		{
			Pass 
			{
				CGPROGRAM
				#pragma target 2.0
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"

				float4 _LineColor;
				sampler2D _MaskTex;
				float4 _MaskTex_ST;
				
				float _Count_U;
				float _Count_V;

				float _Speed_X;
				float _Speed_Y;
				
				float _TexScale;
				float _LineBright;
				struct appdata_t
				{
					float4 vertex	: POSITION;
					float2 texcoord : TEXCOORD0;
					float4 color	: COLOR;
				};
				
				
				struct v2f
				{
					float4 vertex	: POSITION;
					float2 uv		: TEXCOORD0;
					float4 color	: COLOR;
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
					
					float time = fmod(_Time, 1);
					v.texcoord.x = v.texcoord.x * _Count_U;
					v.texcoord.y = v.texcoord.y * _Count_V;
					float maxHeight = 30;
					float s = saturate(_WorldSpaceCameraPos.y / maxHeight);
					s = lerp(1, 0.3, saturate(s)); //_TexScale

#ifdef _USE_CROSSFADE
					float scaleX = CUSTOM_CROSSFADE(_G_CrossfadeTime.z, max(max(_G_CrossfadeMode.x, _G_CrossfadeMode.y), _G_CrossfadeMode.z)); // 1단계에서 UV스케일 처리
#else
					float scaleX = 1;
#endif
					o.uv = applyMatrix(
						getXYTranslationMatrix(float2(0.3, 0.5)),
						applyMatrix( // scale
							getXYScaleMatrix(float2(1, 0.3 + (s * 0.3))),
							applyMatrix( // rotate
								getXYRotationMatrix(0),
								applyMatrix(
									getXYTranslationMatrix(float2(-0.5, -0.5)),
									(v.texcoord.xy)
								)
							)
						)
					) + float2(_Speed_X * -2, _Speed_Y) * time;

					o.color = v.color * _LineColor * 3;
					return o;
				}
				
				
				float4 frag(v2f i) : COLOR
				{
					float4 finalColor = fixed4 (0,0,0,0);
					
					float4 maskTex = tex2D (_MaskTex,i.uv);
					
					finalColor.rgb = i.color.rgb * maskTex.r;
					
					finalColor.a = maskTex.r * i.color.a * 0.5;

     				return finalColor;
				}
				ENDCG
			}
		}
	}
	CustomEditor "ShaderGUI_NM_FleetTrail"
}
