Shader "Unlit/TerrainInstance"
{
	Properties
	{
		_MainTex("Main Tex",2D) = "white" {}
		_Color("Color",Color) = (1,1,1,1)
		_HeightNormalTex("Height Normal Texture",2D) = "white" {}
		_MaxHeight("Max Height",Float) = 100.0
	}
		SubShader
		{
			Tags { "RenderType" = "Opaque" }

			Pass
			{
				CGPROGRAM
				#pragma multi_compile_instancing
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD0;
				};

				sampler2D _MainTex;
				float4 _Color;
				sampler2D _HeightNormalTex;
				float _MaxHeight;

#ifdef UNITY_INSTANCING_ENABLED
				UNITY_INSTANCING_BUFFER_START(TerrainProps)
					UNITY_DEFINE_INSTANCED_PROP(float4,_StartEndUV)
				UNITY_INSTANCING_BUFFER_END(TerrainProps)
#else
				float4 _StartEndUV;
#endif
				float DecodeHeight(float2 heightXY)
				{
					float2 decodeDot = float2(1.0f, 1.0f / 255.0f);
					return dot(heightXY, decodeDot);
				}

				v2f vert(appdata v)
				{
					UNITY_SETUP_INSTANCE_ID(v);
					v2f o;
#ifdef UNITY_INSTANCING_ENABLED
					float4 startEndUV = UNITY_ACCESS_INSTANCED_PROP(TerrainProps, _StartEndUV);
#else
					float4 startEndUV = _StartEndUV;
#endif
					float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

					float2 uv;
					uv.x = lerp(startEndUV.x, startEndUV.z, v.uv.x);
					uv.y = lerp(startEndUV.y, startEndUV.w, v.uv.y);

					o.uv = uv;

					float2 heightTex = tex2Dlod(_HeightNormalTex, float4(o.uv.x, o.uv.y, 0, 0)).rg;
					float height = DecodeHeight(heightTex) * _MaxHeight;
					worldPos.y = height;

					o.vertex = mul(UNITY_MATRIX_VP, worldPos);
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					fixed4 col = tex2D(_HeightNormalTex, i.uv);
					return col;
				}
				ENDCG
			}
		}
}
