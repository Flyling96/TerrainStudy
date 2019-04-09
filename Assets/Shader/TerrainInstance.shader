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
				#pragma hull hs_quad_surf
				#pragma domain ds_quad_surf
				#pragma fragment frag
				#pragma target 5.0
				#pragma 

				#include "UnityCG.cginc"

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
					uint instanceID : SV_InstanceID;
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD0;
					uint instanceID : SV_InstanceID;
				};

				struct internalTessInterp_appdata
				{
					float4 vertex : INTERNALTESSPOS;
					float2 uv:TEXCOORD0;
					uint instanceID : SV_InstanceID;
				};

				sampler2D _MainTex;
				float4 _Color;
				sampler2D _HeightNormalTex;
				float _MaxHeight;

#ifdef UNITY_INSTANCING_ENABLED
				UNITY_INSTANCING_BUFFER_START(TerrainProps)
					UNITY_DEFINE_INSTANCED_PROP(float4, _StartEndUV)
					UNITY_DEFINE_INSTANCED_PROP(float4, _TessVertexCounts)
					UNITY_DEFINE_INSTANCED_PROP(float, _LODTessVertexCounts)
				UNITY_INSTANCING_BUFFER_END(TerrainProps)
#else
				float4 _StartEndUV;
				float4 _TessVertexCounts;
				float4 _LODTessVertexCounts;
#endif
				float DecodeHeight(float2 heightXY)
				{
					float2 decodeDot = float2(1.0f, 1.0f / 255.0f);
					return dot(heightXY, decodeDot);
				}

				internalTessInterp_appdata vert(appdata v)
				{
					internalTessInterp_appdata o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
//#ifdef UNITY_INSTANCING_ENABLED
//					float4 startEndUV = UNITY_ACCESS_INSTANCED_PROP(TerrainProps, _StartEndUV);
//#else
//					float4 startEndUV = _StartEndUV;
//#endif
					float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

					//float2 uv;
					//uv.x = lerp(startEndUV.x, startEndUV.z, v.uv.x);
					//uv.y = lerp(startEndUV.y, startEndUV.w, v.uv.y);

					//o.uv = uv;
					o.uv = v.uv;

					//float2 heightTex = tex2Dlod(_HeightNormalTex, float4(o.uv.x, o.uv.y, 0, 0)).rg;
					//float height = DecodeHeight(heightTex) * _MaxHeight;
					//worldPos.y = height;

					//o.vertex = mul(UNITY_MATRIX_VP, worldPos);
					o.vertex = worldPos;
					return o;
				}

				//Tesslation
				struct UnityTessellationQuadFactors
				{
					float edge[4] : SV_TessFactor;
					float inside[2] : SV_InsideTessFactor;
				};

				UnityTessellationQuadFactors hsconst_quad_surf(InputPatch<internalTessInterp_appdata, 4> v)
				{
					UNITY_SETUP_INSTANCE_ID(v[0]);
					UnityTessellationQuadFactors o;
#ifdef UNITY_INSTANCING_ENABLED
					float4 tessCount = UNITY_ACCESS_INSTANCED_PROP(TerrainProps, _TessVertexCounts);
					float lodVertexCount = UNITY_ACCESS_INSTANCED_PROP(TerrainProps, _LODTessVertexCounts);
#else
					float4 tessCount = _TessVertexCounts;
					float lodVertexCount = _LODTessVertexCounts;
#endif

					o.edge[0] = tessCount.z;
					o.edge[1] = tessCount.x;
					o.edge[2] = tessCount.w;
					o.edge[3] = tessCount.y;

					o.inside[0] = lodVertexCount;
					o.inside[1] = lodVertexCount;

					return o;
				}

				[UNITY_domain("quad")]
				[UNITY_partitioning("integer")]
				[UNITY_outputtopology("triangle_cw")]
				[UNITY_patchconstantfunc("hsconst_quad_surf")]
				[UNITY_outputcontrolpoints(4)]
				internalTessInterp_appdata hs_quad_surf(InputPatch<internalTessInterp_appdata, 4> v, uint id:SV_OutputControlPointID)
				{
					return v[id];
				}

				[UNITY_domain("quad")]
				v2f ds_quad_surf(UnityTessellationQuadFactors tessFactors, const OutputPatch<internalTessInterp_appdata, 4> v, float2 uv:SV_DomainLocation)
				{
					UNITY_SETUP_INSTANCE_ID(v[0]);
					v2f o;
					o.vertex = lerp(lerp(v[0].vertex, v[1].vertex, uv.x), lerp(v[3].vertex, v[2].vertex, uv.x), uv.y);
					o.uv = lerp(lerp(v[0].uv, v[1].uv, uv.x), lerp(v[3].uv, v[2].uv, uv.x), uv.y);
					o.instanceID = lerp(lerp(v[0].instanceID, v[1].instanceID, uv.x), lerp(v[3].instanceID, v[2].instanceID, uv.x), uv.y);

#ifdef UNITY_INSTANCING_ENABLED
					float4 startEndUV = UNITY_ACCESS_INSTANCED_PROP(TerrainProps, _StartEndUV);
#else
					float4 startEndUV = _StartEndUV;
#endif
					float2 realUV;
					realUV.x = lerp(startEndUV.x, startEndUV.z, o.uv.x);
					realUV.y = lerp(startEndUV.y, startEndUV.w, o.uv.y);

					o.uv = realUV;

					float2 heightTex = tex2Dlod(_HeightNormalTex, float4(o.uv.x, o.uv.y, 0, 0)).rg;
					float height = DecodeHeight(heightTex) * _MaxHeight;
					o.vertex.y = height;

					o.vertex = mul(UNITY_MATRIX_VP, o.vertex);

					return o;
				}


				fixed4 frag(v2f i) : SV_Target
				{
					return float4(i.uv,0,1);
					UNITY_SETUP_INSTANCE_ID(i);
#ifdef UNITY_INSTANCING_ENABLED
					float4 tessCount = UNITY_ACCESS_INSTANCED_PROP(TerrainProps, _TessVertexCounts);
#else
					float4 tessCount = _TessVertexCounts;
#endif
					fixed4 col = tex2D(_HeightNormalTex, i.uv);
					return tessCount/20;
				}
				ENDCG
			}
		}
}
