using Unity.Burst;
using Unity.Mathematics;

public static class Falloff
{
    public static float CalculateFalloff(float x, float start, float end, FalloffType type = FalloffType.Smooth)
    {
        return type switch
        {
            FalloffType.Linear => LinearFalloff(x, start, end),
            FalloffType.Smooth => SmoothFalloff(x, start, end),
            FalloffType.Exponential => ExponentialFalloff(x, start, end),
            FalloffType.Sinusoidal => SinusoidalFalloff(x, start, end),
            FalloffType.Parabolic => ParabolicFalloff(x, start, end),
            _ => LinearFalloff(x, start, end)
        };
    }

    public static float LinearFalloff(float x, float start, float end)
    {
        // Якщо значення менше або дорівнює start, повертаємо 1
        if (x <= start) return 1.0f;
        // Якщо значення більше або дорівнює end, повертаємо 0
        if (x >= end) return 0.0f;

        // Інакше обчислюємо плавний перехід
        return 1.0f - (x - start) / (end - start);
    }

    // 2. Згладжений (Smooth Step) falloff
    // Використовує кубічну функцію для більш плавного переходу на краях
    public static float SmoothFalloff(float x, float start, float end)
    {
        // Якщо значення менше або дорівнює start, повертаємо 1
        if (x <= start) return 1.0f;
        // Якщо значення більше або дорівнює end, повертаємо 0
        if (x >= end) return 0.0f;

        // Нормалізуємо значення до діапазону [0,1]
        var t = 1.0f - (x - start) / (end - start);

        // Застосовуємо формулу smooth step: 3t^2 - 2t^3
        return t * t * (3.0f - 2.0f * t);
    }

    // 3. Експоненціальний falloff
    // Дає більш різкий спад на початку і повільніший в кінці
    public static float ExponentialFalloff(float x, float start, float end)
    {
        // Якщо значення менше або дорівнює start, повертаємо 1
        if (x <= start) return 1.0f;
        // Якщо значення більше або дорівнює end, повертаємо 0
        if (x >= end) return 0.0f;

        // Нормалізуємо значення до діапазону [0,1]
        var t = (x - start) / (end - start);

        // Експоненціальна функція: e^(-5t)
        return math.exp(-5.0f * t);
    }

    public static float SinusoidalFalloff(float x, float start, float end)
    {
        // Якщо значення менше або дорівнює start, повертаємо 1
        if (x <= start) return 1.0f;
        // Якщо значення більше або дорівнює end, повертаємо 0
        if (x >= end) return 0.0f;

        // Нормалізуємо значення до діапазону [0,1]
        var t = (x - start) / (end - start);

        // Використовуємо синус для згладжування: cos(t * PI/2)
        return math.cos(t * math.PI * 0.5f);
    }

    // 2. Параболічний falloff
    // Нелінійний спад за параболічним законом
    public static float ParabolicFalloff(float x, float start, float end)
    {
        // Якщо значення менше або дорівнює start, повертаємо 1
        if (x <= start) return 1.0f;
        // Якщо значення більше або дорівнює end, повертаємо 0
        if (x >= end) return 0.0f;

        // Нормалізуємо значення до діапазону [0,1]
        var t = (x - start) / (end - start);

        // Застосовуємо параболічну функцію: 1 - t^2
        return 1.0f - t * t;
    }
}