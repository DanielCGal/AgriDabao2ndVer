Shader "AgriDabao/StormSky"
{
    Properties
    {
        _Color ("Overhead Colour (alpha is how much of the sky it covers)", Color) = (0.27, 0.29, 0.32, 1)
        _HorizonColor ("Horizon Colour", Color) = (0.38, 0.42, 0.48, 1)
        _HorizonHeight ("Horizon Blend Height", Range(0.01, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent-499"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Sphere"
        }

        Cull Front
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            fixed4 _HorizonColor;
            float _HorizonHeight;

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float height : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.height = normalize(v.vertex.xyz).y;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed3 colour = lerp(_HorizonColor.rgb, _Color.rgb, smoothstep(0.0, _HorizonHeight, i.height));
                return fixed4(colour, _Color.a);
            }
            ENDCG
        }
    }
}
