Shader "Custom/TerraForgeTerrainShader" {
    Properties {
        // Ground Layer properties (unchanged)
        [NoScaleOffset]_MainTex0 ("Texture Ground Layer", 2D) = "white" {}
        [HDR]_Color0 ("Color Ground Layer", Color) = (1, 1, 1, 1)
        _MaxAngle0 ("Max Angle Ground Layer", Range(0, 90)) = 10
        _TransitionWidth0 ("Transition Width Ground Layer", Range(0, 20)) = 5
        _TileSizeX0 ("Tile Size X Ground Layer", Range(0, 100)) = 10
        _TileSizeY0 ("Tile Size Y Ground Layer", Range(0, 100)) = 10

        // Cliff Layer properties (unchanged)
        [NoScaleOffset]_MainTex1 ("Texture Cliff Layer", 2D) = "white" {}
        [HDR]_Color1 ("Color Cliff Layer", Color) = (1, 1, 1, 1)
        _TileSizeX1 ("Tile Size X Cliff Layer", Range(0, 100)) = 10
        _TileSizeY1 ("Tile Size Y Cliff Layer", Range(0, 100)) = 10

        // Snow Layer properties (unchanged)
        [NoScaleOffset]_MainTex2 ("Texture Snow Layer", 2D) = "white" {}
        [HDR]_Color2 ("Color Snow Layer", Color) = (1, 1, 1, 1)
        _SnowHeight ("Snow Height", Range(0, 1000)) = 0.5
        _SnowTransitionWidth ("Snow Transition Width", Range(0, 1000)) = 0.1
        _TileSizeX2 ("Tile Size X Snow Layer", Range(0, 100)) = 10
        _TileSizeY2 ("Tile Size Y Snow Layer", Range(0, 100)) = 10

        // Sand Layer properties
        [NoScaleOffset]_MainTex3 ("Texture Sand Layer", 2D) = "white" {}
        [HDR]_Color3 ("Color Sand Layer", Color) = (1, 1, 1, 1)
        _SandHeight ("Sand Height", Range(0, 1000)) = 0.2
        _SandTransitionWidth ("Sand Transition Width", Range(0, 50)) = 0.1
        _TileSizeX3 ("Tile Size X Sand Layer", Range(0, 100)) = 10
        _TileSizeY3 ("Tile Size Y Sand Layer", Range(0, 100)) = 10
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert

        sampler2D _MainTex0;
        sampler2D _MainTex1;
        sampler2D _MainTex2;
        sampler2D _MainTex3;

        fixed4 _Color0;
        fixed4 _Color1;
        fixed4 _Color2;
        fixed4 _Color3;

        float _MaxAngle0;
        float _TransitionWidth0;

        float _TileSizeX0;
        float _TileSizeY0;
        float _TileSizeX1;
        float _TileSizeY1;
        float _TileSizeX2;
        float _TileSizeY2;
        float _TileSizeX3;
        float _TileSizeY3;

        float _SnowHeight;
        float _SnowTransitionWidth;

        float _SandHeight;
        float _SandTransitionWidth;

        struct Input {
            float2 uv_MainTex;
            float3 worldNormal;
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutput o) {
            // Calculate the slope angle in degrees (for Ground and Cliff Layers)
            float slopeAngle = degrees(acos(dot(IN.worldNormal, float3(0, 1, 0))));

            // Determine the level based on slope angle (Ground and Cliff Layers)
            int level = (slopeAngle <= _MaxAngle0) ? 0 : 1;

            // Apply the tile sizes to the UV coordinates (Ground and Cliff Layers)
            float tileSizeX, tileSizeY;
            if (level == 0) {
                tileSizeX = _TileSizeX0;
                tileSizeY = _TileSizeY0;
            } else {
                tileSizeX = _TileSizeX1;
                tileSizeY = _TileSizeY1;
            }
            float2 uvTile = IN.uv_MainTex;
            uvTile.x *= tileSizeX;
            uvTile.y *= tileSizeY;

            // Calculate the transition factor for snow
            float snowFactor = 0.0;
            if (IN.worldPos.y >= _SnowHeight) {
                float snowDistance = IN.worldPos.y - _SnowHeight;
                snowFactor = saturate(snowDistance / _SnowTransitionWidth);
            }

            // Apply the tile sizes to the UV coordinates (Snow Layer)
            float2 uvSnow = IN.uv_MainTex;
            uvSnow.x *= _TileSizeX2;
            uvSnow.y *= _TileSizeY2;

            // Calculate the transition factor for sand
            float sandFactor = 0.0;
            if (IN.worldPos.y <= _SandHeight) {
                float sandDistance = _SandHeight - IN.worldPos.y;
                sandFactor = saturate(sandDistance / _SandTransitionWidth);
            }

            // Apply the tile sizes to the UV coordinates (Sand Layer)
            float2 uvSand = IN.uv_MainTex;
            uvSand.x *= _TileSizeX3;
            uvSand.y *= _TileSizeY3;

            // Sample textures for each layer
            fixed4 texColor0 = tex2D(_MainTex0, uvTile);
            fixed4 texColor1 = tex2D(_MainTex1, uvTile);
            fixed4 texColor2 = tex2D(_MainTex2, uvSnow);
            fixed4 texColor3 = tex2D(_MainTex3, uvSand);

            // Calculate the final color by combining the colors of the levels, snow, and sand
            fixed4 finalColor = lerp(texColor0 * _Color0, texColor1 * _Color1, smoothstep(_MaxAngle0 - _TransitionWidth0, _MaxAngle0, slopeAngle));
            finalColor = lerp(finalColor, texColor2 * _Color2, snowFactor);
            finalColor = lerp(finalColor, texColor3 * _Color3, sandFactor);

            o.Albedo = finalColor.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
