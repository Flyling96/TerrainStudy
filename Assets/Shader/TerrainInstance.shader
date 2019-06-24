Shader "Unlit/TerrainInstance"
{
	Properties
	{
		_MainTex("Main Tex",2D) = "white" {}
		_Color("Color",Color) = (1,1,1,1)
		_HeightNormalTex("Height Normal Texture",2D) = "white" {}
		_MaxHeight("Max Height",Float) = 100.0
		_TerrainMapArray("Terrain Map Array",2DArray) = "white" {}
		_AlphaMap("Alpha Map",2D) = "white" {}

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
				#pragma target 4.6

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
					int instanceID : SV_InstanceID;
				};

				struct internalTessInterp_appdata
				{
					float4 vertex : SV_POSITION;
					float2 uv:TEXCOORD0;
					float4 startEndUV:TEXCOORD1;
					int instanceID : SV_InstanceID;
				};

				sampler2D _MainTex;
				float4 _Color;
				sampler2D _HeightNormalTex;
				float _MaxHeight;
				sampler2D _AlphaMap;
				uniform float4 _TerrainMapSize[30];
				uniform float4 _ChunkPixelCount;
				uniform float4 _MapSize;
				UNITY_DECLARE_TEX2DARRAY(_TerrainMapArray);

#ifdef UNITY_INSTANCING_ENABLED
				UNITY_INSTANCING_BUFFER_START(TerrainProps)
					UNITY_DEFINE_INSTANCED_PROP(float4, _StartEndUV)
					UNITY_DEFINE_INSTANCED_PROP(float4, _AlphaTexIndexs)
					UNITY_DEFINE_INSTANCED_PROP(float4, _TessVertexCounts)
					UNITY_DEFINE_INSTANCED_PROP(float, _LODTessVertexCounts)
				UNITY_INSTANCING_BUFFER_END(TerrainProps)
#else
				float4 _StartEndUV;
				float4 _AlphaTexIndexs;
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
					//UNITY_TRANSFER_INSTANCE_ID(v, o);
					o.instanceID = v.instanceID;
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

#ifdef UNITY_INSTANCING_ENABLED
					float4 startEndUV = UNITY_ACCESS_INSTANCED_PROP(TerrainProps, _StartEndUV);
#else
					float4 startEndUV = _StartEndUV;
#endif

					o.startEndUV = startEndUV;

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

					//o.edge[0] = 3;
					//o.edge[1] = 3;
					//o.edge[2] = 3;
					//o.edge[3] = 3;

					//o.inside[0] = 10;
					//o.inside[1] = 10;

					return o;
				}

				[UNITY_domain("quad")]
				[UNITY_partitioning("integer")]
				[UNITY_outputtopology("triangle_cw")]
				[UNITY_patchconstantfunc("hsconst_quad_surf")]
				[UNITY_outputcontrolpoints(4)]
				//[maxtessfactor(64.0f)]
				internalTessInterp_appdata hs_quad_surf(InputPatch<internalTessInterp_appdata, 4> v, uint id:SV_OutputControlPointID)
				{
					return v[id];
				}

				[UNITY_domain("quad")]
				v2f ds_quad_surf(UnityTessellationQuadFactors tessFactors, const OutputPatch<internalTessInterp_appdata, 4> v, float2 uv:SV_DomainLocation)
				{
					v2f o;
					o.vertex = lerp(lerp(v[0].vertex, v[1].vertex, uv.x), lerp(v[3].vertex, v[2].vertex, uv.x), uv.y);
					o.uv = lerp(lerp(v[0].uv, v[1].uv, uv.x), lerp(v[3].uv, v[2].uv, uv.x), uv.y);
					o.instanceID = v[0].instanceID;

					float2 heightUV;
					heightUV.x = lerp(v[0].startEndUV.x, v[0].startEndUV.z, o.uv.x);
					heightUV.y = lerp(v[0].startEndUV.y, v[0].startEndUV.w, o.uv.y);


					//float2 alphaLimit = float2(0.5f / _ChunkPixelCount.z, 0.5f / _ChunkPixelCount.w);
					//o.uv.x = o.uv.x * step(alphaLimit.x, o.uv.x) + alphaLimit.x * (1 - step(alphaLimit.x, o.uv.x));
					//o.uv.x = o.uv.x * step(o.uv.x,1-alphaLimit.x) + (1-alphaLimit.x) * (1 - step(o.uv.x, 1 - alphaLimit.x));
					//o.uv.y = o.uv.y * step(alphaLimit.y, o.uv.y) + alphaLimit.y * (1 - step(alphaLimit.y, o.uv.y));
					//o.uv.y = o.uv.y * step(o.uv.y, 1 - alphaLimit.y) + (1 - alphaLimit.y) * (1 - step(o.uv.y, 1 - alphaLimit.y));

					//o.uv.x = lerp(startEndUV.x, startEndUV.z, o.uv.x);
					//o.uv.y = lerp(startEndUV.y, startEndUV.w, o.uv.y);

					float2 heightTex = tex2Dlod(_HeightNormalTex, float4(heightUV.x, heightUV.y, 0, 0)).rg;
					float height = DecodeHeight(heightTex) * _MaxHeight;
					o.vertex.y = height;

					o.vertex = mul(UNITY_MATRIX_VP, o.vertex);

					return o;
				}

				float2 GetRealUV(float2 uv, float index)
				{
					uv.x *= _MapSize.z/_TerrainMapSize[index].x;
					uv.x += _TerrainMapSize[index].z;
					uv.y *= _MapSize.w/ _TerrainMapSize[index].y;
					uv.y += _TerrainMapSize[index].w;
					return uv;
				}

				float4 GetAlphaColor(float4 alphaTexIndexs, float2 weightUV, float2 uv)
				{
					float4 weight = tex2D(_AlphaMap, weightUV);
					//return weight;
					float4 color = UNITY_SAMPLE_TEX2DARRAY(_TerrainMapArray, float3(GetRealUV(uv, alphaTexIndexs.x), alphaTexIndexs.x)) * weight.x * step(0, alphaTexIndexs.x)+
						UNITY_SAMPLE_TEX2DARRAY(_TerrainMapArray, float3(GetRealUV(uv, alphaTexIndexs.y), alphaTexIndexs.y)) * weight.y * step(0, alphaTexIndexs.y) +
						UNITY_SAMPLE_TEX2DARRAY(_TerrainMapArray, float3(GetRealUV(uv, alphaTexIndexs.z), alphaTexIndexs.z)) * weight.z * step(0, alphaTexIndexs.z) +
						UNITY_SAMPLE_TEX2DARRAY(_TerrainMapArray, float3(GetRealUV(uv, alphaTexIndexs.w), alphaTexIndexs.w)) * weight.w * step(0, alphaTexIndexs.w);

					return color;
				}

				//float GetMipMapLevel(float2 uv) // in texel units
				//{
				//	float2  dx = ddx(uv);
				//	float2  dy = ddy(uv);
				//	float maxdd = max(dot(dx, dx), dot(dy, dy));
				//	return 0.5 * log2(maxdd);
				//}


				float4 frag(v2f i) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(i);
#ifdef UNITY_INSTANCING_ENABLED
					float4 alphaTexIndexs = UNITY_ACCESS_INSTANCED_PROP(TerrainProps, _AlphaTexIndexs);
					float4 startEndUV = UNITY_ACCESS_INSTANCED_PROP(TerrainProps, _StartEndUV);
#else
					float4 alphaTexIndexs = _AlphaTexIndexs;
					float4 startEndUV = _StartEndUV;
#endif

					float2 uv;
					uv.x = lerp(startEndUV.x, startEndUV.z, i.uv.x);
					uv.y = lerp(startEndUV.x, startEndUV.z, i.uv.y);

					float2 alphaLimit = float2(0.5f / _ChunkPixelCount.z, 0.5f / _ChunkPixelCount.w);
					i.uv.x = i.uv.x * step(alphaLimit.x, i.uv.x) + alphaLimit.x * (1 - step(alphaLimit.x, i.uv.x));
					i.uv.x = i.uv.x * step(i.uv.x,1-alphaLimit.x) + (1-alphaLimit.x) * (1 - step(i.uv.x, 1 - alphaLimit.x));
					i.uv.y = i.uv.y * step(alphaLimit.y, i.uv.y) + alphaLimit.y * (1 - step(alphaLimit.y, i.uv.y));
					i.uv.y = i.uv.y * step(i.uv.y, 1 - alphaLimit.y) + (1 - alphaLimit.y) * (1 - step(i.uv.y, 1 - alphaLimit.y));

					i.uv.x = lerp(startEndUV.x, startEndUV.z, i.uv.x);
					i.uv.y = lerp(startEndUV.y, startEndUV.w, i.uv.y);

					//float2 uv;
					//uv.x = lerp(startEndUV.x, startEndUV.z, i.uv.x);
					//uv.y = lerp(startEndUV.y, startEndUV.w, i.uv.y);

					//float mipmapLevel = GetMipMapLevel(uv);

					//float2 chunkPixelCount = _ChunkPixelCount.zw / pow(2, mipmapLevel);

					//float2 alphaLimit = float2(0.5f / chunkPixelCount.x, 0.5f / chunkPixelCount.y);
					//uv.x = uv.x * step(alphaLimit.x, uv.x) + alphaLimit.x * (1 - step(alphaLimit.x, uv.x));
					//uv.x = uv.x * step(uv.x, 1 - alphaLimit.x) + (1 - alphaLimit.x) * (1 - step(uv.x, 1 - alphaLimit.x));
					//uv.y = uv.y * step(alphaLimit.y, uv.y) + alphaLimit.y * (1 - step(alphaLimit.y, uv.y));
					//uv.y = uv.y * step(uv.y, 1 - alphaLimit.y) + (1 - alphaLimit.y) * (1 - step(uv.y, 1 - alphaLimit.y));

					float4 col = GetAlphaColor(alphaTexIndexs,i.uv,uv);
					return col;
				}


				ENDCG
			}
		}
}
