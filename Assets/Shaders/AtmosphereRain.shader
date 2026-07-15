Shader "CandyCrush/AtmosphereRain"
{
    Properties
    {
        _Color ("Color", Color) = (0.88, 0.94, 1, 0.9)
        _Density ("Density", Range(8, 70)) = 18
        _Speed ("Speed", Range(0.1, 4)) = 1.35
        _Length ("Streak Length", Range(0.08, 0.7)) = 0.5
        _Thickness ("Thickness", Range(0.004, 0.06)) = 0.011
        _Angle ("Angle (deg)", Range(-60, -8)) = -24
        _Opacity ("Opacity", Range(0, 1)) = 0.48
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
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

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
            float _Density;
            float _Speed;
            float _Length;
            float _Thickness;
            float _Angle;
            float _Opacity;

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // 对齐旧粒子 Stretch 雨丝：斜向长细条，头尖尾淡
            float rainLayer(float2 uv, float density, float speed, float seed)
            {
                float rad = radians(_Angle);
                float s = sin(rad);
                float c = cos(rad);
                // 转到「竖直下落」UV（下落沿 -Y）
                float2 p = float2(c * uv.x + s * uv.y, -s * uv.x + c * uv.y);

                float cols = density;
                float col = floor(p.x * cols);
                float fx = frac(p.x * cols);
                float h = hash21(float2(col, seed));
                // 约 1/3 列有雨，避免帘状密雨
                if (h < 0.66) return 0.0;

                // 纵向更疏 → 单条更长、条与条间隔更大
                float rows = density * 0.16;
                float phase = hash21(float2(col, seed + 4.2));
                float along = frac(p.y * rows + _Time.y * speed * (0.85 + h * 0.4) + phase);

                // 带头近 0：亮尖；向后拖出长条（粒子 lengthScale 感）
                float len = _Length * (0.85 + h * 0.55);
                float trail = 1.0 - smoothstep(0.0, len, along);
                float head = smoothstep(0.0, 0.04, along);
                float body = trail * trail * head;

                // 宽度随尾巴收细（对齐 streak 贴图 tip）
                float tw = _Thickness * (0.45 + 0.7 * trail) * (0.7 + h * 0.45);
                float cx = abs(fx - (0.28 + h * 0.44));
                float thin = smoothstep(tw, 0.0, cx);

                return body * thin * (0.42 + h * 0.45);
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
                float a = 0.0;
                // 两层即可，第三层会把雨拉成帘
                a += rainLayer(i.uv, _Density, _Speed, 1.7);
                a += rainLayer(i.uv + 0.17, _Density * 0.72, _Speed * 0.88, 8.3) * 0.55;
                a = saturate(a) * _Opacity * _Color.a;
                return fixed4(_Color.rgb, a);
            }
            ENDCG
        }
    }
    FallBack Off
}
