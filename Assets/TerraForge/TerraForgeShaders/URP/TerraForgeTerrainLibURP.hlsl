void TerraForge_float
(
	float3 Position,
	float3 Normal,
	float4 UV,
	UnityTexture2D MainTex0,
	UnityTexture2D MainTex1,
	UnityTexture2D MainTex2,
	UnityTexture2D MainTex3,
	UnitySamplerState Sampler,
	float4 Color0,
	float4 Color1,
	float4 Color2,
	float4 Color3,
	float MaxAngle0,
	float SnowHeight,
	float SnowTransitionWidth,
	float TileSizeX0,
	float TileSizeY0,
	float TileSizeX1,
	float TileSizeY1,
	float TileSizeX2,
	float TileSizeY2,
	float SandHeight,
	float SandTransitionWidth,
	float TileSizeX3,
	float TileSizeY3,
	float TransitionWidth0,
	out float3 Out
)
{
	            // Calculate the slope angle in degrees (for Ground and Cliff Layers)
            float slopeAngle = degrees(acos(dot(Normal, float3(0, 1, 0))));

            // Determine the level based on slope angle (Ground and Cliff Layers)
            int level = (slopeAngle <= MaxAngle0) ? 0 : 1;

            // Apply the tile sizes to the UV coordinates (Ground and Cliff Layers)
            float tileSizeX, tileSizeY;
            if (level == 0)
			{
                tileSizeX = TileSizeX0;
                tileSizeY = TileSizeY0;
            } 
			else 
			{
                tileSizeX = TileSizeX1;
                tileSizeY = TileSizeY1;
            }
            float2 uvTile = UV.xy;
            uvTile.x *= tileSizeX;
            uvTile.y *= tileSizeY;

            // Calculate the transition factor for snow
            float snowFactor = 0.0;
            if (Position.y >= SnowHeight)
			{
                float snowDistance = Position.y - SnowHeight;
                snowFactor = saturate(snowDistance / SnowTransitionWidth);
            }

            // Apply the tile sizes to the UV coordinates (Snow Layer)
            float2 uvSnow = UV.xy;
            uvSnow.x *= TileSizeX2;
            uvSnow.y *= TileSizeY2;

            // Calculate the transition factor for sand
            float sandFactor = 0.0;
            if (Position.y <= SandHeight)
			{
                float sandDistance = SandHeight - Position.y;
                sandFactor = saturate(sandDistance / SandTransitionWidth);
            }

            // Apply the tile sizes to the UV coordinates (Sand Layer)
            float2 uvSand = UV.xy;
            uvSand.x *= TileSizeX3;
            uvSand.y *= TileSizeY3;

            // Sample textures for each layer
            float4 texColor0 = SAMPLE_TEXTURE2D(MainTex0,Sampler, uvTile);
            float4 texColor1 = SAMPLE_TEXTURE2D(MainTex1,Sampler, uvTile);
            float4 texColor2 = SAMPLE_TEXTURE2D(MainTex2,Sampler, uvSnow);
            float4 texColor3 = SAMPLE_TEXTURE2D(MainTex3,Sampler, uvSand);

            // Calculate the final color by combining the colors of the levels, snow, and sand
            float4 finalColor = lerp(texColor0 * Color0, texColor1 * Color1, smoothstep(MaxAngle0 - TransitionWidth0, MaxAngle0, slopeAngle));
            finalColor = lerp(finalColor, texColor2 * Color2, snowFactor);
            finalColor = lerp(finalColor, texColor3 * Color3, sandFactor);

            Out = finalColor.rgb;
}