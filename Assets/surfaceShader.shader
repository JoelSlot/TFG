Shader "Custom/NewSurfaceShader"
{

	Properties{

		_MainTex("Texture", 2D) = "white" {}
		_TexScale("Texture Scale", Float) = 1

	}

	SubShader {
		Tags {"RenderType"="Opaque"} //no transparent terrain
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0

		sampler2D _MainTex;
		float _TexScale;
		
		struct Input {
			float3 worldPos;
			float3 worldNormal;
		};

		void surf (Input IN, inout SurfaceOutputStandard o){

			o.Albedo = tex2D(_MainTex, IN.worldPos.xy);
		}

		ENDCG

	}
	Fallback "Diff"
}
