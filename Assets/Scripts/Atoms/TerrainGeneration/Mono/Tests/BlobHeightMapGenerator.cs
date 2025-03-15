using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BlobHeightMapGenerator : MonoBehaviour
{
    [Header("Налаштування сітки")] public int resolution = 100; // Кількість сегментів сітки

    public float size = 10f; // Розмір площини

    [Header("Налаштування Blob")] public float blobRadius = 3f; // Базовий радіус блоба

    public float potentialStrength = 5f; // Сила потенційного поля
    public float potentialFalloff = 1f; // Зниження потенціалу з відстанню

    [Header("Налаштування шуму")] public float noiseScale = 0.5f; // Масштаб шуму (чим менше, тим більш грубі деталі)

    public float noiseAmplitude = 1f; // Амплітуда шуму (впливає на силу деформації)

    private Mesh mesh;
    private int[] triangles;
    private Vector3[] vertices;

    private void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        CreateMesh();
        ApplyHeightMap();
    }

    // Створення базової сітки
    private void CreateMesh()
    {
        vertices = new Vector3[(resolution + 1) * (resolution + 1)];
        for (int y = 0, i = 0; y <= resolution; y++)
        {
            for (var x = 0; x <= resolution; x++)
            {
                var xPos = ((float) x / resolution - 0.5f) * size;
                var zPos = ((float) y / resolution - 0.5f) * size;
                vertices[i] = new Vector3(xPos, 0f, zPos);
                i++;
            }
        }

        triangles = new int[resolution * resolution * 6];
        var ti = 0;
        var vi = 0;
        for (var y = 0; y < resolution; y++)
        {
            for (var x = 0; x < resolution; x++)
            {
                triangles[ti] = vi;
                triangles[ti + 1] = vi + resolution + 1;
                triangles[ti + 2] = vi + 1;

                triangles[ti + 3] = vi + 1;
                triangles[ti + 4] = vi + resolution + 1;
                triangles[ti + 5] = vi + resolution + 2;

                ti += 6;
                vi++;
            }

            vi++;
        }
    }

    // Застосування карти висот із додаванням шуму
    private void ApplyHeightMap()
    {
        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = vertices[i];

            // Обчислення базової відстані від центру площини
            var r = new Vector2(vertex.x, vertex.z).magnitude;

            // Додавання перлін-шуму для натуральності.
            // Зсуваємо координати, щоб уникнути негативних значень для шуму.
            var noise = Mathf.PerlinNoise((vertex.x + size / 2f) * noiseScale, (vertex.z + size / 2f) * noiseScale) *
                        noiseAmplitude;

            // Модифікована відстань з шумом
            var modifiedR = r + noise;

            // Обчислення SDF для круга з модифікованою відстанню
            var sdf = modifiedR - blobRadius;

            // Потенційне поле, яке зменшується з відстанню
            var potential = potentialStrength / (1f + modifiedR * potentialFalloff);

            // Використання функції smoothstep для плавного переходу
            var smoothFactor = SmoothStep(0f, 1f, Mathf.Clamp01(-sdf + 0.5f));

            // Остаточна висота вершини
            var height = Mathf.Clamp01(potential * smoothFactor);
            vertex.y = height;
            vertices[i] = vertex;
        }

        // Оновлення сітки з новими вершинами
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    // Функція smoothstep для створення плавного переходу
    private float SmoothStep(float edge0, float edge1, float x)
    {
        x = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return x * x * (3 - 2 * x);
    }
}