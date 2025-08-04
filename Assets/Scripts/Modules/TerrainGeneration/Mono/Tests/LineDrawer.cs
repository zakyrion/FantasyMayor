using Unity.Mathematics;
using UnityEngine;

public class LineDrawer : MonoBehaviour
{
    public float lineLength = 10f;

    private void OnDrawGizmos()
    {
        DrawLine(60f, lineLength);
    }

    private void DrawLine(float angleDegree, float length)
    {
        var angleRad = angleDegree * Mathf.Deg2Rad;
        var slope = math.sqrt(3); // m in y = mx + c

        // Start and end points
        var start = new Vector3(transform.position.x, 0, 0);
        var end = new Vector3(transform.position.x + length, 0, slope * length);

        Gizmos.DrawLine(start, end);
    }
}