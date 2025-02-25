using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class ContourGenerator : MonoBehaviour
{
    // Розміри області
    public int gridWidth = 50;
    public int gridHeight = 50;
    public float cellSize = 0.2f;
    public float isoLevel = 0.5f;

    private RBFInterpolator _interpolator;

    // Приклад RBF‑інтерполяції: тут можна використати свій RBFInterpolator, який повертає значення F(x,y)
    // Для демонстрації будемо створювати просте перлин‑шумове поле
    private float[,] scalarField;

    private void Start()
    {
        var hexMesh = HexMeshUtil.CreateFlatBasedHex(1);
        var values = new List<float>();

        var float2Vertices = hexMesh.vertices.Skip(1) // Ignore the first element
            .Select(v => new float2(v.x, v.z)) // Select x and y as a Vector2
            .ToList();

        for (var y = 0; y < float2Vertices.Count; y++)
            values[y] = Random.value;

        _interpolator = new RBFInterpolator(float2Vertices, values);
        GenerateScalarField();
        var segments = MarchingSquares.GenerateSegments(scalarField, cellSize, isoLevel);

        // Виводимо кількість отриманих сегментів
        Debug.Log("Отримано сегментів: " + segments.Count);
    }

    // Для візуалізації ізоліній малюємо їх у OnDrawGizmos
    private void OnDrawGizmos()
    {
        if (scalarField == null)
            return;

        var segments = MarchingSquares.GenerateSegments(scalarField, cellSize, isoLevel);
        Gizmos.color = Color.red;
        foreach (var seg in segments)
        {
            var p1 = new Vector3(seg.Item1.x, seg.Item1.y, 0);
            var p2 = new Vector3(seg.Item2.x, seg.Item2.y, 0);
            Gizmos.DrawLine(p1, p2);
        }
    }

    /// <summary>
    ///     Генерує сітку значень (scalar field).
    ///     Тут для прикладу використано перлин‑шум, але замість цього можна використовувати результати RBF‑інтерполяції.
    /// </summary>
    private void GenerateScalarField()
    {
        scalarField = new float[gridWidth, gridHeight];

        for (var i = 0; i < gridWidth; i++)
        {
            for (var j = 0; j < gridHeight; j++)
            {
                // Обчислення координат у просторі
                var x = i * cellSize;
                var y = j * cellSize;
                // Наприклад, використаємо перлін‑шум (або свій RBF‑метод)
                //scalarField[i, j] = Mathf.PerlinNoise(x, y);
                scalarField[i, j] = _interpolator.Evaluate(new float2(x, y));
            }
        }
    }
}

public static class MarchingSquares
{
    /// <summary>
    ///     Генерує ізолінійні сегменти із сітки значень.
    ///     grid – двовимірний масив значень (розмір: gridWidth x gridHeight)
    ///     cellSize – відстань між сусідніми точками сітки
    ///     isoLevel – ізольований рівень (значення, для якого шукаємо контур)
    ///     Повертає список лінійних сегментів (як пара Vector2)
    /// </summary>
    public static List<(Vector2, Vector2)> GenerateSegments(float[,] grid, float cellSize, float isoLevel)
    {
        var segments = new List<(Vector2, Vector2)>();

        var gridWidth = grid.GetLength(0);
        var gridHeight = grid.GetLength(1);

        // Проходимо по кожній клітинці сітки (зауважте, що клітинка утворюється 4-ма сусідніми точками)
        for (var i = 0; i < gridWidth - 1; i++)
        {
            for (var j = 0; j < gridHeight - 1; j++)
            {
                // Значення чотирьох кутів клітинки
                var v0 = grid[i, j];
                var v1 = grid[i + 1, j];
                var v2 = grid[i + 1, j + 1];
                var v3 = grid[i, j + 1];

                // Обчислюємо індекс клітинки: кожен кут перевіряємо порівняно з isoLevel
                var cellIndex = 0;
                if (v0 >= isoLevel) cellIndex |= 1; // нижній лівий
                if (v1 >= isoLevel) cellIndex |= 2; // нижній правий
                if (v2 >= isoLevel) cellIndex |= 4; // верхній правий
                if (v3 >= isoLevel) cellIndex |= 8; // верхній лівий

                // Якщо клітинка повністю всередині або зовні isoLevel – пропускаємо
                if (cellIndex == 0 || cellIndex == 15)
                    continue;

                // Розраховуємо позиції кутів клітинки
                var bottomLeft = new Vector2(i * cellSize, j * cellSize);
                var bottomRight = new Vector2((i + 1) * cellSize, j * cellSize);
                var topRight = new Vector2((i + 1) * cellSize, (j + 1) * cellSize);
                var topLeft = new Vector2(i * cellSize, (j + 1) * cellSize);

                // Обчислюємо точки перетину на межах клітинки
                Vector2? edge0 = null, edge1 = null, edge2 = null, edge3 = null;
                // Ребро 0: між bottomLeft та bottomRight
                if ((cellIndex & 1) != (cellIndex & 2))
                    edge0 = Interpolate(bottomLeft, bottomRight, v0, v1, isoLevel);
                // Ребро 1: між bottomRight та topRight
                if ((cellIndex & 2) != (cellIndex & 4))
                    edge1 = Interpolate(bottomRight, topRight, v1, v2, isoLevel);
                // Ребро 2: між topRight та topLeft
                if ((cellIndex & 4) != (cellIndex & 8))
                    edge2 = Interpolate(topRight, topLeft, v2, v3, isoLevel);
                // Ребро 3: між topLeft та bottomLeft
                if ((cellIndex & 8) != (cellIndex & 1))
                    edge3 = Interpolate(topLeft, bottomLeft, v3, v0, isoLevel);

                // Таблиця для Marching Squares: залежно від cellIndex формуємо сегменти.
                // Для простоти розглянемо базові випадки (де в клітинці утворюється один сегмент).
                switch (cellIndex)
                {
                    case 1:
                    case 14:
                        segments.Add((edge3.Value, edge0.Value));
                        break;
                    case 2:
                    case 13:
                        segments.Add((edge0.Value, edge1.Value));
                        break;
                    case 3:
                    case 12:
                        segments.Add((edge3.Value, edge1.Value));
                        break;
                    case 4:
                    case 11:
                        segments.Add((edge1.Value, edge2.Value));
                        break;
                    case 6:
                    case 9:
                        segments.Add((edge0.Value, edge2.Value));
                        break;
                    case 7:
                    case 8:
                        segments.Add((edge3.Value, edge2.Value));
                        break;
                    case 5:
                        // Випадок «роздвоєння»: може бути два сегменти, але для спрощення беремо два.
                        segments.Add((edge3.Value, edge0.Value));
                        segments.Add((edge1.Value, edge2.Value));
                        break;
                    case 10:
                        segments.Add((edge0.Value, edge1.Value));
                        segments.Add((edge3.Value, edge2.Value));
                        break;
                }
            }
        }

        return segments;
    }

    /// <summary>
    ///     Лінійна інтерполяція для знаходження точки перетину між двома точками p1 та p2.
    /// </summary>
    private static Vector2 Interpolate(Vector2 p1, Vector2 p2, float v1, float v2, float isoLevel)
    {
        var t = (isoLevel - v1) / (v2 - v1);
        return p1 + t * (p2 - p1);
    }
}