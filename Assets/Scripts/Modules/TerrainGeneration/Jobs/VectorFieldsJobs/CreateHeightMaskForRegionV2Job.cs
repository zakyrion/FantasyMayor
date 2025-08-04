using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Job для створення маски висоти для заданої області.
/// Реалізовано оптимізоване додавання меж (border), заливку області за допомогою сканлайн-алгоритму та
/// двопрохідний алгоритм трансформації відстаней із нормалізацією.
/// </summary>
[BurstCompile]
public struct CreateHeightMaskForRegionV2Job : IJob
{
    private const int FILL_PIXEL_VAL = 1;

    /// <summary>
    /// Роздільна здатність маски (масив має розмір Resolution x Resolution).
    /// </summary>
    public int Resolution;
    
    /// <summary>
    /// Масив сегментів, що представляють лінію межі регіону.
    /// </summary>
    public NativeArray<SpotSegment> BorderLine;
    
    /// <summary>
    /// Точка, що гарантовано знаходиться всередині регіону (в світових координатах).
    /// Використовується для старту заповнення.
    /// </summary>
    public float2 PointInside;
    
    /// <summary>
    /// Бінарна маска місцевості, що заповнюється під час виконання job.
    /// Припускається, що структура підтримує індексацію через int2, метод ContainsKey та операцію присвоєння.
    /// </summary>
    public TerrainHeightmap BinaryMask;
    
    /// <summary>
    /// Прямокутна область (у світових координатах), для якої створюється маска.
    /// </summary>
    public Rect Rect;
    
    /// <summary>
    /// Значення, до якого нормалізуються результати обчислення відстаней.
    /// </summary>
    public float ClampValue;

    // Внутрішня змінна, що зберігає роздільну здатність у вигляді float2.
    private float2 _resolution;

    /// <summary>
    /// Головний метод, що послідовно викликає:
    /// 1. AddBorders() – малювання меж,
    /// 2. FillScanline() – заповнення внутрішньої області,
    /// 3. DistanceTransformTwoPass() – обчислення та нормалізацію відстаней.
    /// </summary>
    public void Execute()
    {
        _resolution = new float2(Resolution, Resolution);

        AddBorders();
        FillScanline();
        DistanceTransformTwoPass();
    }

    /// <summary>
    /// Додає межі регіону до маски, використовуючи алгоритм Брезенхема для малювання лінії між сегментами.
    /// Координати перетворюються з простору Rect у піксельний простір.
    /// </summary>
    private void AddBorders()
    {
        var startPoint = BorderLine[0];

        for (var i = 0; i < BorderLine.Length; i++)
        {
            var nextPoint = BorderLine[(i + 1) % BorderLine.Length];

            // Перетворення координат з простору Rect у піксельний простір.
            var pointOnMap = math.remap(Rect.min, Rect.max, new float2(0, 0), _resolution, startPoint.Position.xz);
            var nextPointOnMap = math.remap(Rect.min, Rect.max, new float2(0, 0), _resolution, nextPoint.Position.xz);

            // Обмеження координат, щоб вони не виходили за межі маски.
            pointOnMap = math.clamp(pointOnMap, new float2(0, 0), _resolution);
            nextPointOnMap = math.clamp(nextPointOnMap, new float2(0, 0), _resolution);

            var x0 = (int)math.floor(pointOnMap.x);
            var y0 = (int)math.floor(pointOnMap.y);
            var x1 = (int)math.floor(nextPointOnMap.x);
            var y1 = (int)math.floor(nextPointOnMap.y);

            var dx = math.abs(x1 - x0);
            var dy = math.abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx - dy;

            // Малювання лінії між (x0,y0) та (x1,y1)
            while (true)
            {
                BinaryMask[new int2(x0, y0)] = FILL_PIXEL_VAL;
                if (x0 == x1 && y0 == y1)
                    break;

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

            startPoint = nextPoint;
        }
    }

    /// <summary>
    /// Заповнює внутрішню область регіону, використовуючи сканлайн-алгоритм заливки.
    /// Точка заповнення переводиться з координат Rect у піксельний простір, після чого обчислюється горизонтальний діапазон для заливки.
    /// Для кожного заповненого пікселя додаються сусідні пікселі з рядків зверху та знизу.
    /// </summary>
    private void FillScanline()
    {
        // Перетворення точки всередині регіону в піксельні координати.
        var pointInsidePixels = math.remap(Rect.min, Rect.max, new float2(0, 0), _resolution, PointInside);
        int2 startPixel = new int2((int)math.floor(pointInsidePixels.x), (int)math.floor(pointInsidePixels.y));

        // Використовуємо стек (NativeList) для зберігання пікселів для обробки.
        var pixelStack = new NativeList<int2>(Allocator.Temp);
        pixelStack.Add(startPixel);

        while (pixelStack.Length > 0)
        {
            // Забираємо останній елемент із стеку
            int2 pixel = pixelStack[pixelStack.Length - 1];
            pixelStack.RemoveAt(pixelStack.Length - 1);

            // Знаходимо горизонтальний діапазон, який потрібно заповнити
            int xLeft = pixel.x;
            int xRight = pixel.x;

            while (xLeft > 0 && ShouldPixelBeFilled(new int2(xLeft - 1, pixel.y)))
                xLeft--;

            while (xRight < Resolution - 1 && ShouldPixelBeFilled(new int2(xRight + 1, pixel.y)))
                xRight++;

            // Заповнюємо знайдений діапазон та додаємо сусідні рядки до стеку
            for (int x = xLeft; x <= xRight; x++)
            {
                int2 current = new int2(x, pixel.y);
                if (ShouldPixelBeFilled(current))
                {
                    BinaryMask[current] = FILL_PIXEL_VAL;

                    if (pixel.y > 0 && ShouldPixelBeFilled(new int2(x, pixel.y - 1)))
                        pixelStack.Add(new int2(x, pixel.y - 1));

                    if (pixel.y < Resolution - 1 && ShouldPixelBeFilled(new int2(x, pixel.y + 1)))
                        pixelStack.Add(new int2(x, pixel.y + 1));
                }
            }
        }

        pixelStack.Dispose();
    }

    /// <summary>
    /// Обчислює відстань від кожного пікселя до найближчої межі за допомогою двопрохідного алгоритму (Манхеттенська відстань)
    /// та нормалізує результати так, що максимальне значення рівне ClampValue.
    /// </summary>
    private void DistanceTransformTwoPass()
    {
        // Створення тимчасового масиву для зберігання відстаней.
        NativeArray<int> distance = new NativeArray<int>(Resolution * Resolution, Allocator.Temp);

        // Ініціалізація: пікселі, що заповнені (містять FILL_PIXEL_VAL), отримують значення 0, інші – "безкінечність".
        for (int y = 0; y < Resolution; y++)
        {
            for (int x = 0; x < Resolution; x++)
            {
                int index = y * Resolution + x;
                distance[index] = (BinaryMask[new int2(x, y)] == FILL_PIXEL_VAL) ? 0 : int.MaxValue / 2;
            }
        }

        // Перший прохід: зверху-вниз, зліва-направо.
        for (int y = 0; y < Resolution; y++)
        {
            for (int x = 0; x < Resolution; x++)
            {
                int index = y * Resolution + x;
                if (x > 0)
                    distance[index] = math.min(distance[index], distance[y * Resolution + (x - 1)] + 1);
                if (y > 0)
                    distance[index] = math.min(distance[index], distance[(y - 1) * Resolution + x] + 1);
            }
        }

        // Другий прохід: знизу-вгору, справа-наліво.
        for (int y = Resolution - 1; y >= 0; y--)
        {
            for (int x = Resolution - 1; x >= 0; x--)
            {
                int index = y * Resolution + x;
                if (x < Resolution - 1)
                    distance[index] = math.min(distance[index], distance[y * Resolution + (x + 1)] + 1);
                if (y < Resolution - 1)
                    distance[index] = math.min(distance[index], distance[(y + 1) * Resolution + x] + 1);
            }
        }

        // Знаходимо максимальне значення відстані для нормалізації.
        int maxDistance = 0;
        for (int i = 0; i < distance.Length; i++)
        {
            if (distance[i] > maxDistance)
                maxDistance = distance[i];
        }

        // Розрахунок коефіцієнта нормалізації: максимальне значення перетворюється в ClampValue.
        float scale = (maxDistance > 0) ? ClampValue / maxDistance : 1f;

        // Запис нормалізованих значень назад у BinaryMask.
        for (int y = 0; y < Resolution; y++)
        {
            for (int x = 0; x < Resolution; x++)
            {
                int index = y * Resolution + x;
                BinaryMask[new int2(x, y)] = scale * distance[index];
            }
        }

        distance.Dispose();
    }

    /// <summary>
    /// Перевіряє, чи знаходиться піксель у межах області та чи не було він заповнений раніше.
    /// </summary>
    /// <param name="position">Позиція пікселя у бінарній масці.</param>
    /// <returns>True, якщо піксель можна заповнити, інакше false.</returns>
    private bool ShouldPixelBeFilled(int2 position)
    {
        if (position.x < 0 || position.x >= Resolution || position.y < 0 || position.y >= Resolution)
            return false;
        if (BinaryMask.ContainsKey(position) && BinaryMask[position] >= FILL_PIXEL_VAL)
            return false;
        return true;
    }
}