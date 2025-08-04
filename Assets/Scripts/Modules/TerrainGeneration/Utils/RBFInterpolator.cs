using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class RBFInterpolator
{
    // Список центрів (точок), де відомі значення
    private readonly List<float2> centers;

    // Значення в цих точках (наприклад, висоти)
    private readonly List<float> values;

    // Параметр для гаусової функції
    public float epsilon = 1.0f;

    // Розраховані коефіцієнти w_i
    private float[] weights;

    /// <summary>
    ///     Конструктор приймає список точок і відповідних значень.
    /// </summary>
    public RBFInterpolator(List<float2> points, List<float> values, float epsilon = 1.0f)
    {
        if (points.Count != values.Count)
            throw new ArgumentException("Кількість точок та значень має бути однаковою.");
        centers = points;
        this.values = values;
        this.epsilon = epsilon;
        ComputeWeights();
    }

    /// <summary>
    ///     Гауссова радіальна базисна функція: phi(r) = exp[-(epsilon * r)^2]
    /// </summary>
    private float Phi(float r) => Mathf.Exp(-Mathf.Pow(epsilon * r, 2));

    /// <summary>
    ///     Обчислення коефіцієнтів w_i шляхом розв'язання системи A*w = b.
    /// </summary>
    private void ComputeWeights()
    {
        var N = centers.Count;
        var A = new float[N, N];

        // Формуємо матрицю A
        for (var i = 0; i < N; i++)
        {
            for (var j = 0; j < N; j++)
            {
                var r = Vector2.Distance(centers[i], centers[j]);
                A[i, j] = Phi(r);
            }
        }

        // Вектор b з значеннями
        var b = values.ToArray();

        // Розв'язуємо систему A * weights = b
        weights = SolveLinearSystem(A, b);
    }

    /// <summary>
    ///     Обчислює значення F(x,y) = sum(w_i * Phi(||(x,y) - (x_i,y_i)||))
    /// </summary>
    public float Evaluate(float2 pos)
    {
        var result = 0f;
        for (var i = 0; i < centers.Count; i++)
        {
            var r = Vector2.Distance(pos, centers[i]);
            result += weights[i] * Phi(r);
        }

        return result;
    }

    /// <summary>
    ///     Простіший розв'язувач системи лінійних рівнянь методом Гауссового виключення.
    ///     Примітка: оптимально підходить для невеликих систем.
    /// </summary>
    private float[] SolveLinearSystem(float[,] A, float[] b)
    {
        var n = b.Length;
        // Формуємо розширену матрицю розміром n x (n+1)
        var mat = new float[n, n + 1];
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++) mat[i, j] = A[i, j];
            mat[i, n] = b[i];
        }

        // Прямий хід алгоритму Гаусса
        for (var i = 0; i < n; i++)
        {
            // Знаходимо рядок з максимальним елементом в стовпці i (для стабільності)
            var maxRow = i;
            for (var k = i + 1; k < n; k++)
            {
                if (Mathf.Abs(mat[k, i]) > Mathf.Abs(mat[maxRow, i]))
                    maxRow = k;
            }

            // Обмінюємо рядки
            for (var k = i; k < n + 1; k++)
            {
                var tmp = mat[i, k];
                mat[i, k] = mat[maxRow, k];
                mat[maxRow, k] = tmp;
            }

            // Перевірка на сингулярність
            if (Mathf.Abs(mat[i, i]) < 1e-6f)
            {
                Debug.LogError("Матриця є сингулярною або майже сингулярною.");
                return null;
            }

            // Нормалізація поточного рядка
            var div = mat[i, i];
            for (var k = i; k < n + 1; k++)
                mat[i, k] /= div;

            // Обнуляємо елементи нижче поточного рядка
            for (var k = i + 1; k < n; k++)
            {
                var factor = mat[k, i];
                for (var j = i; j < n + 1; j++) mat[k, j] -= factor * mat[i, j];
            }
        }

        // Зворотній хід
        var x = new float[n];
        for (var i = n - 1; i >= 0; i--)
        {
            var sum = 0f;
            for (var j = i + 1; j < n; j++) sum += mat[i, j] * x[j];
            x[i] = mat[i, n] - sum;
        }

        return x;
    }
}