Shader "Custom/SeperateNestPart"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        //_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 200

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Stencil {
                Ref 20
                ReadMask 20
                Comp NotEqual
                Pass Replace
            }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _Color;

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                col.a *= _Color.a;
                //col.rgb = col.a; // Preserve color intensity when transparent
                return col;
            }
            ENDCG

            //UsePass "Transparent/Diffuse/FORWARD"
        }
    //FallBack "Diffuse"
    }
}
