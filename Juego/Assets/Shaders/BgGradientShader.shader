Shader "Unlit/BgGradientShader"
{
    Properties
    {
        _ColorBottom ("Color Bottom", Color) = (0.2, 0.2, 0.2, 1) // Gris oscuro
        _ColorTop ("Color Top", Color) = (1, 1, 1, 1) // Blanco
    }
    SubShader
    {
        Tags {"Queue"="Overlay" "RenderType"="Opaque"}
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
                float2 uv : TEXCOORD0;
                float4 vertex : POSITION;
            };

            // Propiedades
            uniform float4 _ColorBottom;
            uniform float4 _ColorTop;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Crear el gradiente vertical
                float lerpValue = i.uv.y; // Usamos la coordenada Y para el gradiente vertical
                half4 color = lerp(_ColorBottom, _ColorTop, lerpValue);
                return color;
            }
            ENDCG
        }
    }
}