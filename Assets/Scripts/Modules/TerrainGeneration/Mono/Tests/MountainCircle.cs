using UnityEngine;

public class MountainCircle : MonoBehaviour
{
    public float radius = 5f;
    public float baseNoiseFactor = 1f;
    public int segments = 100;
    public float noiseScale = 0.5f; // Adjust this to change the frequency of the noise

    void Start()
    {
        LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.positionCount = segments + 1;

        for (int i = 0; i <= segments; i++)
        {
            float theta = (float)i / segments * 2 * Mathf.PI;
            float randomNoiseFactor = baseNoiseFactor + Random.Range(-0.5f, 0.5f); // Randomize the noise factor
            float perlinNoise = Mathf.PerlinNoise(noiseScale * Mathf.Cos(theta), noiseScale * Mathf.Sin(theta));
            float modifiedRadius = radius + perlinNoise * randomNoiseFactor;
            Vector3 position = new Vector3(modifiedRadius * Mathf.Cos(theta), modifiedRadius * Mathf.Sin(theta), 0);
            lineRenderer.SetPosition(i, position);
        }
    }
}