Shader "CandyCrush/AtmosphereSnow"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _FluffDensity ("Fluff Density", Range(2, 20)) = 6.5
        _FluffSpeed ("Fluff Speed", Range(0.05, 2)) = 0.55
        _FluffSize ("Fluff Size", Range(0.04, 0.4)) = 0.13
        _Drift ("Horizontal Drift", Range(0, 1)) = 0.32
        _FluffOpacity ("Fluff Opacity", Range(0, 1)) = 0.9
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }
        Cull Off
        ZWrite Off
        ZTest Always
        // 雪絮接近旧粒子 Additive：亮、柔，暗处不脏边
        Blend One OneMinusSrcAlpha

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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float _FluffDensity;
            float _FluffSpeed;
            float _FluffSize;
            float _Drift;
            float _FluffOpacity;

            float hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float2 hash22(float2 p)
            {
                float n = hash21(p);
                return float2(n, hash21(p + n + 17.13));
            }

            float softBall(float2 d, float radius)
            {
                float r = length(d) / max(0.001, radius);
                float a = saturate(1.0 - r);
                return a * a * a;
            }

            // 对齐旧粒子 GetClumpTexture：多瓣软棉团
            float fluffClump(float2 uv, float density, float speed, float size, float layer)
            {
                float2 p = uv * float2(density * 0.9, density);
                p.y += _Time.y * speed;
                p.x += sin(p.y * 1.6 + layer * 2.3 + _Time.y * speed * 0.9) * _Drift * 0.16;
                p.x += _Time.y * speed * _Drift * 0.14;

                float2 cell = floor(p);
                float2 f = frac(p) - 0.5;
                float2 rnd = hash22(cell + layer * 51.3);
                if (rnd.x < 0.52) return 0.0;

                float2 c = f - (rnd - 0.5) * 0.32;
                float s = max(0.08, size * (0.85 + rnd.y * 0.45));
                float ang = (rnd.x - 0.5) * 1.6;
                float ca = cos(ang), sa = sin(ang);

                float2 o1 = float2(ca * 0.22 - sa * 0.10, sa * 0.22 + ca * 0.10) * s;
                float2 o2 = float2(ca * -0.18 - sa * 0.14, sa * -0.18 + ca * 0.14) * s;
                float2 o3 = float2(ca * 0.06 - sa * -0.2, sa * 0.06 + ca * -0.2) * s;

                float a = 0.0;
                a = max(a, softBall(c, s));
                a = max(a, softBall(c - o1, s * 0.72) * 0.92);
                a = max(a, softBall(c - o2, s * 0.68) * 0.85);
                a = max(a, softBall(c - o3, s * 0.55) * 0.75);
                return a * (0.75 + rnd.y * 0.25);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 只要软絮，不要细雪小杂点
                float fluff = 0.0;
                fluff += fluffClump(i.uv, _FluffDensity, _FluffSpeed, max(_FluffSize, 0.08), 1.0);
                fluff += fluffClump(i.uv + 0.17, _FluffDensity * 0.7, _FluffSpeed * 0.85, max(_FluffSize * 1.15, 0.09), 2.0) * 0.9;

                float a = saturate(fluff * _FluffOpacity) * _Color.a;
                float3 rgb = _Color.rgb * a;
                return fixed4(rgb, a);
            }
            ENDCG
        }
    }
    FallBack Off
}
