Shader "UI/QTE Radial Reveal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Radius ("Reveal Radius", Float) = -0.05
        _Edge ("Edge Softness", Float) = 0.04
        _SpeedLineIntensity ("Speed Line Intensity", Range(0, 1)) = 0
        _SpeedLineTime ("Speed Line Time", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float _Radius;
            float _Edge;
            float _SpeedLineIntensity;
            float _SpeedLineTime;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centeredUV = i.uv - 0.5;
                centeredUV.x *= _ScreenParams.x / _ScreenParams.y;
                float distanceFromCenter = length(centeredUV);
                float darkness = smoothstep(_Radius - _Edge, _Radius + _Edge, distanceFromCenter);
                float angle = atan2(centeredUV.y, centeredUV.x) / 6.2831853 + 0.5;
                float sector = angle * 36.0;
                float sectorId = floor(sector);
                float sectorPosition = frac(sector);
                float seed = frac(sin(sectorId * 127.1 + 17.0) * 43758.5453);
                float travel = frac(_SpeedLineTime * (0.35 + seed * 0.45) + seed);
                float lineStart = 0.42 + travel * 0.62;
                float lineLength = 0.15 + seed * 0.38;
                float lineProgress = (distanceFromCenter - lineStart) / lineLength;
                float tipTaper = smoothstep(0.0, 0.16, lineProgress) * (1.0 - smoothstep(0.82, 1.0, lineProgress));
                float lineWidth = (0.018 + seed * 0.02) * tipTaper;
                float angularLine = (1.0 - smoothstep(lineWidth, lineWidth + 0.018, abs(sectorPosition - 0.5))) * step(0.28, seed);
                float radialLine = smoothstep(0.0, 0.025, lineProgress) * (1.0 - smoothstep(0.93, 1.0, lineProgress));
                float speedLineAmount = angularLine * radialLine * _SpeedLineIntensity;
                float3 lineColor = lerp(float3(1.0, 1.0, 1.0), float3(1.0, 0.06, 0.06), step(0.5, seed));
                fixed textureAlpha = tex2D(_MainTex, i.uv).a * _Color.a;
                return fixed4(lineColor * speedLineAmount, max(darkness, speedLineAmount) * textureAlpha);
            }
            ENDCG
        }
    }
}
