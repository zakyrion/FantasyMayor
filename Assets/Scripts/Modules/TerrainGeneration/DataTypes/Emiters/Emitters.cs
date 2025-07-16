using Unity.Mathematics;

/// <summary>
///     All emitters are living in texture space
/// </summary>
public struct CircleEmitter
{
    public int2 Position;
    public float PlatoRadius;
    public float FalloffRadius;
    public FalloffType FalloffType;
    public int Level;

    public static EmitterData ToEmitterData(CircleEmitter emitter) => new(emitter.Position, emitter.PlatoRadius);

    public static CircleEmitter FromEmitterData(EmitterData data, float falloffRadius, FalloffType falloffType) =>
        new()
        {
            Position = data.Position,
            PlatoRadius = data.Radius,
            FalloffRadius = data.Radius + falloffRadius,
            FalloffType = falloffType
        };
}

/// <summary>
///     Структура для зберігання даних про емітер
/// </summary>
public struct EmitterData
{
    public int2 Position;
    public float Radius;

    public EmitterData(int2 pos, float rad)
    {
        Position = pos;
        Radius = rad;
    }
}

public enum FalloffType
{
    Linear,
    Smooth,
    Parabolic,
    
    Exponential = 1000,
    Sinusoidal,
}