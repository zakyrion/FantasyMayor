using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct ApplyVectorForcesJob : IJob
{
    public int Resolution;
    public TerrainHeightmap Heightmap;
    public NativeList<CircleEmitter> CircleEmitters;
    public float NoiseScale;
    private const float AMPLITUDE = 1f;
    private const float OVERLAP_KOEF = .25f;

    public void Execute()
    {
        var circleCount = CircleEmitters.Length;

        for (var x = 0; x < Resolution; x++)
        {
            for (var y = 0; y < Resolution; y++)
            {
                float height = 0;
                float acamulativeHeight = 0;
                var count = 0;

                for (var i = 0; i < circleCount; i++)
                {
                    var position = new float2(x + Resolution / 2f, y + Resolution / 2f) * NoiseScale;
                    var period = new float2(Resolution * 2, Resolution * 2);
                    var noiseShift = (noise.pnoise(position, period) * AMPLITUDE + 1f) / 2f * 7;

                    var emitter = CircleEmitters[i];
                    var dist = math.distance(emitter.Position, new float2(x, y));

                    dist += noiseShift;

                    var influence = Falloff.CalculateFalloff(dist, emitter.PlatoRadius, emitter.FalloffRadius,
                        emitter.FalloffType);

                    height = math.max(height, influence);

                    if (influence >= 0.01f)
                    {
                        acamulativeHeight += influence;
                        count++;
                    }
                }

                var heightToSet = 0f;
                if (count > 1)
                    heightToSet = height + acamulativeHeight / count * OVERLAP_KOEF;
                else
                    heightToSet = height;

                Heightmap[x, y] = math.clamp(heightToSet, 0, .9f);
            }
        }

        Heightmap.Normalize();
    }
}

[BurstCompile]
public struct ParallelApplyVectorForcesJob : IJobParallelFor
{
    public int Resolution;
    [NativeDisableParallelForRestriction] public TerrainHeightmap Heightmap;
    [ReadOnly] public NativeArray<CircleEmitter> CircleEmitters;
    public float NoiseScale;
    private const float AMPLITUDE = 1f;
    private const float OVERLAP_KOEF = .25f;

    // Для кожного пікселя карти висот
    public void Execute(int index)
    {
        // Обчислюємо x та y з одновимірного індексу
        int x = index % Resolution;
        int y = index / Resolution;
        
        float height = 0;
        float acamulativeHeight = 0;
        var count = 0;

        int circleCount = CircleEmitters.Length;
        for (var i = 0; i < circleCount; i++)
        {
            var position = new float2(x + Resolution / 2f, y + Resolution / 2f) * NoiseScale;
            var period = new float2(Resolution * 2, Resolution * 2);
            var noiseShift = (noise.pnoise(position, period) * AMPLITUDE + 1f) / 2f * 7;

            var emitter = CircleEmitters[i];
            var dist = math.distance(emitter.Position, new float2(x, y));

            dist += noiseShift;

            var influence = Falloff.CalculateFalloff(dist, emitter.PlatoRadius, emitter.FalloffRadius,
                emitter.FalloffType);

            height = math.max(height, influence);

            if (influence >= 0.01f)
            {
                acamulativeHeight += influence;
                count++;
            }
        }

        var heightToSet = 0f;
        if (count > 1)
            heightToSet = height + acamulativeHeight / count * OVERLAP_KOEF;
        else
            heightToSet = height;

        Heightmap[x, y] = math.clamp(heightToSet, 0, .9f);
    }
}