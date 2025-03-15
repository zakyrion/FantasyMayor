using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Job для створення маски висоти для заданої області.
/// Виконує побудову меж (border), заливку області (fill) та дистанційне перетворення (distance transform).
/// </summary>
[BurstCompile]
public struct CreateHeightMaskForRegionJob : IJob
{
    private const int FILL_PIXEL_VAL = 1;
    
    /// <summary>
    /// Роздільна здатність маски (маска має розмір Resolution x Resolution пікселів).
    /// </summary>
    public int Resolution;
    
    /// <summary>
    /// Масив сегментів, що представляють лінію межі регіону.
    /// </summary>
    public NativeArray<SpotSegment> BorderLine;
    
    /// <summary>
    /// Точка, що гарантовано знаходиться всередині регіону. Використовується як стартова для заливки.
    /// </summary>
    public float2 PointInside;
    
    /// <summary>
    /// Масив, що представляє бінарну маску висоти місцевості. Під час виконання джобу оновлюється.
    /// </summary>
    public TerrainHeightmap BinaryMask;
    
    /// <summary>
    /// Прямокутна область у світових координатах, для якої створюється маска.
    /// </summary>
    public Rect Rect;
    
    /// <summary>
    /// Максимальне значення для нормалізації після дистанційного перетворення.
    /// </summary>
    public float ClampValue;
    
    // Внутрішня змінна, що зберігає роздільну здатність у вигляді float2.
    private float2 _resolution;

    /// <summary>
    /// Головний метод, що виконує послідовність операцій:
    /// 1. Додавання меж,
    /// 2. Заливку області,
    /// 3. Дистанційне перетворення.
    /// </summary>
    public void Execute()
    {
        _resolution = new int2(Resolution, Resolution);

        AddBorders();
        Fill();
        DistanceTransform();
    }

    /// <summary>
    /// Здійснює дистанційне перетворення бінарної маски.
    /// Для кожного заповненого пікселя обчислюється відстань до найближчої межі, після чого значення нормалізуються.
    /// </summary>
    private void DistanceTransform()
    {
        // Створення таблиці сумованих областей для швидких запитів.
        var summedTable = BinaryMask.ToSummedTable(Allocator.Temp);
        var maxDist = Resolution / 2;

        // Проходимо по всіх пікселях маски.
        for (var y = 0; y < Resolution; y++)
        {
            for (var x = 0; x < Resolution; x++)
            {
                var key = new int2(x, y);

                if (BinaryMask[key] > 0)
                {
                    // Виконуємо бінарний пошук у таблиці сум для визначення відстані.
                    var step = summedTable.BinarySearch(key, maxDist, 0, FILL_PIXEL_VAL);
                    BinaryMask[key] = step;
                }
            }
        }

        // Нормалізація значень маски з використанням заданого ClampValue.
        BinaryMask.Normalize(maxClamp: ClampValue);
    }

    /// <summary>
    /// Здійснює заливку внутрішньої області, починаючи з точки, що гарантовано знаходиться всередині регіону.
    /// Використовується алгоритм пошуку в ширину (BFS) для заповнення суміжних пікселів.
    /// </summary>
    private void Fill()
    {
        // Перетворення точки всередині регіону з координат прямокутника у піксельні координати.
        var pointInside = math.remap(Rect.min, Rect.max, new float2(), _resolution, PointInside);
        var pixel = new int2((int) math.floor(pointInside.x), (int) math.floor(pointInside.y));

        // Ініціалізація черги для BFS, починаючи з обчисленого пікселя.
        var queue = new NativeQueue<int2>(Allocator.TempJob);
        queue.Enqueue(pixel);

        // Продовжуємо заливку до тих пір, поки черга не стане порожньою.
        while (queue.Count > 0)
        {
            var currentPosition = queue.Dequeue();

            if (ShouldPixelBeFilled(currentPosition))
            {
                // Заповнюємо піксель.
                BinaryMask[currentPosition] = FILL_PIXEL_VAL;

                // Додаємо сусідні пікселі до черги.
                queue.Enqueue(new int2(currentPosition.x + 1, currentPosition.y));
                queue.Enqueue(new int2(currentPosition.x - 1, currentPosition.y));
                queue.Enqueue(new int2(currentPosition.x, currentPosition.y + 1));
                queue.Enqueue(new int2(currentPosition.x, currentPosition.y - 1));
            }
        }
    }

    /// <summary>
    /// Перевіряє, чи повинен бути заповнений даний піксель.
    /// Піксель заповнюється лише якщо він знаходиться в межах області та ще не заповнений.
    /// </summary>
    /// <param name="position">Позиція пікселя у бінарній масці.</param>
    /// <returns>True, якщо піксель має бути заповнений, інакше false.</returns>
    private bool ShouldPixelBeFilled(int2 position)
    {
        // Не заповнюємо, якщо піксель вже заповнений.
        if (BinaryMask.ContainsKey(position) && BinaryMask[position] >= FILL_PIXEL_VAL)
        {
            return false;
        }

        // Перевірка, що піксель знаходиться в межах допустимих координат.
        return position.x >= 0 && position.x < Resolution &&
               position.y >= 0 && position.y < Resolution;
    }

    /// <summary>
    /// Додає лінії меж (border) до бінарної маски.
    /// Для кожного сегмента лінії виконується перетворення координат з простору прямокутника у піксельний простір,
    /// після чого використовується алгоритм Брезенхема для малювання лінії.
    /// </summary>
    private void AddBorders()
    {
        // Починаємо з першого сегмента.
        var startPoint = BorderLine[0];

        // Проходимо через усі сегменти лінії.
        for (var i = 0; i < BorderLine.Length; i++)
        {
            var nextPoint = BorderLine[(i + 1) % BorderLine.Length];

            // Перетворення координат з простору Rect у піксельний простір.
            var pointOnMap = math.remap(Rect.min, Rect.max, new float2(0, 0), _resolution,
                startPoint.Position.xz);

            var nextPointOnMap = math.remap(Rect.min, Rect.max, new float2(0, 0), _resolution,
                nextPoint.Position.xz);

            // Обмеження координат, щоб вони не виходили за межі маски.
            pointOnMap = math.clamp(pointOnMap, new float2(0, 0), _resolution);
            nextPointOnMap = math.clamp(nextPointOnMap, new float2(0, 0), _resolution);

            // Отримання цілочисельних координат пікселів.
            var x0 = (int) math.floor(pointOnMap.x);
            var y0 = (int) math.floor(pointOnMap.y);
            var x1 = (int) math.floor(nextPointOnMap.x);
            var y1 = (int) math.floor(nextPointOnMap.y);

            // Обчислення різниці та напрямків для алгоритму Брезенхема.
            var dx = math.abs(x1 - x0);
            var dy = math.abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx - dy;

            // Малювання лінії між двома точками.
            while (true)
            {
                BinaryMask[new int2(x0, y0)] = FILL_PIXEL_VAL;
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }

            // Перехід до наступного сегмента.
            startPoint = nextPoint;
        }
    }
}