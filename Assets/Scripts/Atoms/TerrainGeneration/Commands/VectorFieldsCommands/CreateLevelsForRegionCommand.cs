using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class CreateLevelsForRegionCommand
{
    private readonly int _resolution;

    public CreateLevelsForRegionCommand(int resolution)
    {
        _resolution = resolution;
    }

    public async UniTask<TerrainHeightmap> Execute(Stripe stripe, float2 pointInside, float maxHeight)
    {
        var terrainHeightmap = new TerrainHeightmap(_resolution, Allocator.TempJob);

        var job = new CreateLevelsForRegionJob
        {
            Resolution = _resolution,
            PointInside = pointInside,
            Heightmap = terrainHeightmap,
            MaxHeight = maxHeight,
            Region = stripe.Rect(),
            Triangles = stripe.ToNativeArray(Allocator.TempJob)
        };

        job.Execute();

        //await GenerateTerrainVisualization();

        return terrainHeightmap;

        async Task GenerateTerrainVisualization()
        {

            var triangles = stripe.Triangles();

            var duration = 1f;

            for (int i = 0; i < triangles.Count; i++)
            {
                triangles[i].DrawOnFlat(Color.magenta, duration);
                var rect = triangles[i].Rect();

                var bottomLeft = new Vector3(rect.xMin, 0, rect.yMin);
                var bottomRight = new Vector3(rect.xMax, 0, rect.yMin);
                var topLeft = new Vector3(rect.xMin, 0, rect.yMax);
                var topRight = new Vector3(rect.xMax, 0, rect.yMax);
            
                Debug.Log($"[skh] Rect width: {rect.width}, height {rect.height}");

                Debug.DrawRay(bottomLeft, Vector3.up * 10f, Color.green, duration);
                Debug.DrawRay(bottomRight, Vector3.up * 10f, Color.green, duration);
                Debug.DrawRay(topLeft, Vector3.up * 10f, Color.green, duration);
                Debug.DrawRay(topRight, Vector3.up * 10f, Color.green, duration);

                await UniTask.WaitForSeconds(duration);
            }
        }
    }
}