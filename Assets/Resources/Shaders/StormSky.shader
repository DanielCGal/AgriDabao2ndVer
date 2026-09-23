// The overcast sky WeatherVisualController fades in over the normal sky during
// rain and typhoons. It lives in Resources and is loaded by name, so it ships in
// builds without being assigned to anything.
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
        // URP draws the skybox between the opaque and transparent queues. One
        // step into the transparent range puts this after the sky and before
        // everything else transparent, so rain still falls in front of it.
        Tags
        {
            "Queue" = "Transparent-499"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Sphere"
        }

        // Seen from inside, so the inner faces are the ones drawn. It never writes
        // depth, and anything nearer - terrain, crops - hides it as usual.
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
                // The dome is never rotated, so its own up is the world's up.
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
