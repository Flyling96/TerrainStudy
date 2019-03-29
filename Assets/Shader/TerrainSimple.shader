Shader "Unlit/TerrainSimple"
{
    Properties
    {
		_HeightNormalTex("Height Normal Texture",2D) = "white" {}
		_MaxHeight("Max Height",Float) = 100.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
            };

            sampler2D _HeightNormalTex;
			float _MaxHeight;

			float DecodeHeight(float2 heightXY)
			{
				float2 decodeDot = float2(1.0f, 1.0f / 255.0f);
				return dot(heightXY, decodeDot);
			}

            v2f vert (appdata v)
            {
                v2f o;
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.uv = v.uv;
				float2 heightTex = tex2Dlod(_HeightNormalTex, float4(o.uv.x, o.uv.y, 0, 0)).rg;
				float height = DecodeHeight(heightTex) * _MaxHeight;
				worldPos.y = height;
				o.vertex = mul(UNITY_MATRIX_VP, worldPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				//return 1;
                fixed4 col = tex2D(_HeightNormalTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
