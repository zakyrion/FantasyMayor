using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
///     Статичний клас для розміщення емітерів у колі з обмеженим перекриттям
///     Використовує Unity.Mathematics та структури DOTS (float2, float3)
/// </summary>
public static class EmitterPacker
{
    /// <summary>
    ///     Метод для розміщення емітерів з випадковими радіусами у колі
    /// </summary>
    /// <param name="containerCenter">Центр контейнера</param>
    /// <param name="containerRadius">Радіус контейнера</param>
    /// <param name="minEmitterRadius">Мінімальний радіус емітера</param>
    /// <param name="maxEmitterRadius">Максимальний радіус емітера</param>
    /// <param name="numberOfEmitters">Кількість емітерів для розміщення</param>
    /// <param name="maxOverlapPercentage">Максимальне перекриття (від 0 до 1)</param>
    /// <returns>Список розміщених емітерів</returns>
    public static List<EmitterData> PlaceEmitters(
        int2 containerCenter,
        float containerRadius,
        float minEmitterRadius,
        float maxEmitterRadius,
        int numberOfEmitters,
        float maxOverlapPercentage)
    {
        var emitters = new List<EmitterData>();

        // Кількість спроб для розміщення кожного емітера
        var maxAttempts = 100;

        for (var i = 0; i < numberOfEmitters; i++)
        {
            var placedSuccessfully = false;

            // Генеруємо випадковий радіус для цього емітера
            var emitterRadius = minEmitterRadius + Random.value * (maxEmitterRadius - minEmitterRadius);

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Генеруємо випадкову позицію всередині контейнера,
                // використовуючи полярні координати для рівномірного розподілу по площі
                var angle = Random.value * math.PI * 2;
                var r = containerRadius * math.sqrt(Random.value);

                var candidatePosition = containerCenter + new int2(
                    (int) (r * math.cos(angle)),
                    (int) (r * math.sin(angle))
                );

                // Перевіряємо, чи не виходить емітер за межі контейнера
                if (math.distance(containerCenter, candidatePosition) + emitterRadius > containerRadius)
                    continue;

                // Перевіряємо перекриття з існуючими емітерами
                var overlapsExcessively = false;

                foreach (var existingEmitter in emitters)
                {
                    var distance = math.distance(candidatePosition, existingEmitter.Position);

                    // Мінімальна допустима відстань - це сума радіусів, помножена на (1 - maxOverlapPercentage)
                    var minDistance = (emitterRadius + existingEmitter.Radius) * (1 - maxOverlapPercentage);

                    if (distance < minDistance)
                    {
                        overlapsExcessively = true;
                        break;
                    }
                }

                if (!overlapsExcessively)
                {
                    emitters.Add(new EmitterData(candidatePosition, emitterRadius));
                    placedSuccessfully = true;
                    break;
                }
            }

            // Якщо не вдалося розмістити емітер після всіх спроб,
            // можливо, простір вже заповнений
            if (!placedSuccessfully)
            {
                Debug.LogWarning(
                    $"Не вдалося розмістити емітер {i + 1}/{numberOfEmitters}. Можливо, вже не вистачає місця.");
                break;
            }
        }

        Debug.Log($"Розміщено {emitters.Count} емітерів з {numberOfEmitters} запланованих.");
        return emitters;
    }

    /// <summary>
    ///     Покращений метод для розміщення емітерів з використанням спірального алгоритму
    ///     Спочатку розміщує більші емітери, потім менші для кращого заповнення
    /// </summary>
    public static List<EmitterData> PlaceEmittersImproved(
        int2 containerCenter,
        float containerRadius,
        float minEmitterRadius,
        float maxEmitterRadius,
        int numberOfEmitters,
        float maxOverlapPercentage)
    {
        var emitters = new List<EmitterData>();

        // Використовуємо спіральний алгоритм з золотим кутом
        var goldenAngle = math.PI * (3 - math.sqrt(5)); // ~2.4 радіани

        // Генеруємо і сортуємо радіуси від більшого до меншого
        var radii = new List<float>();
        for (var i = 0; i < numberOfEmitters; i++)
        {
            var radius = minEmitterRadius + Random.value * (maxEmitterRadius - minEmitterRadius);
            radii.Add(radius);
        }

        radii.Sort((a, b) => b.CompareTo(a)); // Сортуємо від більшого до меншого

        for (var i = 0; i < radii.Count; i++)
        {
            var emitterRadius = radii[i];
            var placed = false;

            // Обчислюємо кількість кілець спіралі на основі співвідношення радіусів
            var spiralIterations = Mathf.CeilToInt(containerRadius / emitterRadius) * 15;

            for (var j = 0; j < spiralIterations && !placed; j++)
            {
                var t = (float) j / spiralIterations;
                var angle = j * goldenAngle;

                // Відстань від центру поступово збільшується
                var radius = t * (containerRadius - emitterRadius);

                var candidatePosition = containerCenter + new int2(
                    (int) (radius * math.cos(angle)),
                    (int) (radius * math.sin(angle))
                );

                // Перевіряємо, чи не виходить емітер за межі контейнера
                if (math.distance(containerCenter, candidatePosition) + emitterRadius > containerRadius)
                    continue;

                // Перевіряємо перекриття з існуючими емітерами
                var overlapsExcessively = false;

                foreach (var existingEmitter in emitters)
                {
                    var distance = math.distance(candidatePosition, existingEmitter.Position);
                    var minDistance = (emitterRadius + existingEmitter.Radius) * (1 - maxOverlapPercentage);

                    if (distance < minDistance)
                    {
                        overlapsExcessively = true;
                        break;
                    }
                }

                if (!overlapsExcessively)
                {
                    emitters.Add(new EmitterData(candidatePosition, emitterRadius));
                    placed = true;
                }
            }

            // Якщо не вдалося розмістити емітер, додаємо додаткові спроби з невеликими зрушеннями
            if (!placed)
            {
                var adjustmentAttempts = 50;
                for (var attempt = 0; attempt < adjustmentAttempts && !placed; attempt++)
                {
                    // Випадковий кут та відстань від центру
                    var angle = Random.value * math.PI * 2;
                    var radius = Random.value * (containerRadius - emitterRadius);

                    var candidatePosition = containerCenter + new int2(
                        (int) (radius * math.cos(angle)),
                        (int) (radius * math.sin(angle))
                    );

                    // Перевіряємо перекриття
                    var overlapsExcessively = false;
                    foreach (var existingEmitter in emitters)
                    {
                        var distance = math.distance(candidatePosition, existingEmitter.Position);
                        var minDistance = (emitterRadius + existingEmitter.Radius) * (1 - maxOverlapPercentage);

                        if (distance < minDistance)
                        {
                            overlapsExcessively = true;
                            break;
                        }
                    }

                    if (!overlapsExcessively)
                    {
                        emitters.Add(new EmitterData(candidatePosition, emitterRadius));
                        placed = true;
                    }
                }

                if (!placed)
                    Debug.LogWarning(
                        $"Не вдалося розмістити емітер {i + 1} з радіусом {emitterRadius}. Недостатньо місця.");
            }
        }

        Debug.Log($"Розміщено {emitters.Count} емітерів з {numberOfEmitters} запланованих.");
        return emitters;
    }

    /// <summary>
    ///     Метод для перевірки, чи можна розмістити емітер в даній позиції
    /// </summary>
    public static bool CanPlaceEmitterAt(
        float2 position,
        float radius,
        float2 containerCenter,
        float containerRadius,
        List<EmitterData> existingEmitters,
        float maxOverlapPercentage)
    {
        // Перевіряємо, чи не виходить емітер за межі контейнера
        if (math.distance(containerCenter, position) + radius > containerRadius)
            return false;

        // Перевіряємо перекриття з існуючими емітерами
        foreach (var existingEmitter in existingEmitters)
        {
            var distance = math.distance(position, existingEmitter.Position);
            var minDistance = (radius + existingEmitter.Radius) * (1 - maxOverlapPercentage);

            if (distance < minDistance)
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Метод для візуалізації розміщених емітерів (можна викликати з OnDrawGizmos)
    /// </summary>
    public static void DrawEmitters(
        List<EmitterData> emitters,
        float3 containerCenter,
        float containerRadius,
        float minEmitterRadius,
        float maxEmitterRadius)
    {
        // Малюємо контейнер
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(containerCenter, containerRadius);

        // Малюємо емітери
        foreach (var emitter in emitters)
        {
            // Встановлюємо колір на основі радіусу (від зеленого до червоного)
            var normalizedRadius = Mathf.InverseLerp(minEmitterRadius, maxEmitterRadius, emitter.Radius);
            Gizmos.color = Color.Lerp(Color.green, Color.red, normalizedRadius);

            var position = new float3(emitter.Position.x, emitter.Position.y, containerCenter.z);
            Gizmos.DrawWireSphere(position, emitter.Radius);
        }
    }

    /// <summary>
    ///     Приклад використання класу
    /// </summary>
    public static void ExampleUsage()
    {
        var center = new int2(0, 0);
        var containerRadius = 10f;
        var minRadius = 0.5f;
        var maxRadius = 2.0f;
        var count = 30;
        var maxOverlap = 0.6f; // 60% перекриття

        // Використовуємо покращений алгоритм розміщення
        var emitters = PlaceEmittersImproved(
            center,
            containerRadius,
            minRadius,
            maxRadius,
            count,
            maxOverlap
        );

        // Можна отримати дані про кожен емітер:
        foreach (var emitter in emitters)
            Debug.Log($"Емітер: позиція = ({emitter.Position.x}, {emitter.Position.y}), радіус = {emitter.Radius}");

        // Для візуалізації викликаємо DrawEmitters у методі OnDrawGizmos MonoBehaviour компонента
    }
}