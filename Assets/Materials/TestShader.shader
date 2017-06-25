// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/TestSurfaceShader" {
	Properties {
		_MainTex ("Albedo", 3D) = "white" {}
		_Depth ("Depth", Float) = 0 
	}
	SubShader {
		Pass {
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0

			sampler3D _MainTex;
			float _Depth;

			struct v2f {
				float4 pos : SV_POSITION;
				float3 srcPos : TEXCOORD0;
			};

			v2f vert(float4 objPos : POSITION)
			{
				v2f o;

				o.pos =	UnityObjectToClipPos(objPos);
				
				o.srcPos = mul(unity_ObjectToWorld, objPos).xyz;
				o.srcPos *= 1;
				o.srcPos.z += _Time.y * 0.01;
				
				return o;
			}
			
			float4 frag(v2f i) : COLOR
			{
				//fixed4 c = tex3D (_MainTex, float3(IN.uv_MainTex, _Time.y));
				//float ns = snoise(i.srcPos) / 2 + 0.5f;
				float ns = tex3D (_MainTex, i.srcPos);
				return float4(ns, _Depth, ns, 1.0f);
			}
			
			ENDCG
		}		


	}
}
