using System;
using System.Collections.Generic;
using System.Linq;
using DataTypes;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class BorderLine
{
    private float3? _startPoint;

    private float _totalLength;
    public List<LineWithCenter> Segments { get; } = new();

    public Line this[int index] => Segments[index];

    public void AddSegment(LineWithCenter segment, float3 center)
    {
        if (Segments.Count > 0)
        {
            var lastSegment = Segments[^1];

            if (math.distance(lastSegment.End, segment.Start) > 0.01f)
                Segments.Add(new LineWithCenter(center, new Line(lastSegment.End, segment.Start)));
        }
        else
            Segments.Add(segment);

        _totalLength = Segments.Sum(s => s.Length);
    }

    public void AddSegment(float3 b, float3 center)
    {
        if (Segments.Count == 0)
        {
            if (_startPoint == null)
                _startPoint = b;
            else if (math.distance(_startPoint.Value, b) > 0.01f)
                AddLine(new LineWithCenter(center, b, _startPoint.Value));
        }
        else
        {
            var lastSegment = Segments.Last();
            var pointExist = Segments.Any(l => l.IsPointBetween(b));

            if (!pointExist)
            {
                var startPoint = math.distance(lastSegment.End, b) < math.distance(lastSegment.Start, b)
                    ? lastSegment.End
                    : lastSegment.Start;
                Segments.Add(new LineWithCenter(center, new Line(startPoint, b)));
            }
        }
        
        _totalLength = Segments.Sum(s => s.Length);
    }

    public void Lock()
    {
        Segments.Add(
            new LineWithCenter(Segments.Last().CenterOfHexTriangle, Segments.Last().End, Segments.Last().Start));
    }

    public void AddLine(LineWithCenter line)
    {
        Segments.Add(line);
        _totalLength = Segments.Sum(s => s.Length);
    }

    public NativeArray<Line> ToNativeArray(Allocator allocator = Allocator.Temp)
    {
        var result = new NativeArray<Line>(Segments.Count, allocator);

        for (var i = 0; i < Segments.Count; i++)
            result[i] = Segments[i];

        return result;
    }

    public List<Line> ToList()
    {
        var result = new List<Line>(Segments.Count);

        for (var i = 0; i < Segments.Count; i++)
            result.Add(Segments[i]);

        return result;
    }

    public void Draw(Color color, float duration = 10)
    {
        foreach (var segment in Segments)
            segment.Draw(color, duration);
    }

    public void Draw(Color colorA, Color colorB, float duration = 10)
    {
        for (var i = 0; i < Segments.Count; i++)
        {
            var segment = Segments[i];
            var color = i % 2 == 0 ? colorA : colorB;
            segment.Draw(color, duration);
        }
    }

    public BorderLine Order()
    {
        for (var i = 0; i < Segments.Count; i++)
        {
            var currentSegment = Segments[i];
            var nextSegment = Segments[(i + 1) % Segments.Count];

            if (math.distance(currentSegment.End, nextSegment.Start) > math.EPSILON)
            {
                nextSegment.Flip();
                Segments[(i + 1) % Segments.Count] = nextSegment;
            }
        }

        return this;
    }

    public void SetHeight(float height)
    {
        for (var i = 0; i < Segments.Count; i++)
        {
            var segment = Segments[i];
            segment.SetHeight(height);
            Segments[i] = segment;
        }
    }
    
    public void SetHeight(float height, float offset)
    {
        for (var i = 0; i < Segments.Count; i++)
        {
            var koef = ((float) i / Segments.Count - .5f) * math.PI * 2;
            
            var segment = Segments[i];
            segment.SetHeight(height + offset * koef);
            Segments[i] = segment;
        }
    }

    public float3 InterpolatePointOnLine(float dist)
    {
        if (dist < 0 || dist > 1)
            throw new ArgumentOutOfRangeException(nameof(dist), "Distance must be between 0 and 1.");

        var targetDistance = _totalLength * dist;

        float accumulatedLength = 0;

        foreach (var segment in Segments)
        {
            var segmentLength = segment.Length;
            if (accumulatedLength + segmentLength >= targetDistance)
            {
                var remainingDistance = targetDistance - accumulatedLength;
                var interpolationRatio = remainingDistance / segmentLength;

                return math.lerp(segment.Start, segment.End, interpolationRatio);
            }

            accumulatedLength += segmentLength;
        }

        // Fallback in case of calculation error (shouldn't be reached)
        return Segments.Last().End;
    }

    public Rect GetBounds()
    {
        var minX = Segments.Min(line => line.Start.x);
        var maxX = Segments.Max(line => line.Start.x);
        var minZ = Segments.Min(line => line.Start.z);
        var maxZ = Segments.Max(line => line.Start.z);

        return new Rect(minX, minZ, maxX - minX, maxZ - minZ);
    }

    public static bool Collinear(float3 start1, float3 end1, float3 start2, float3 end2,
        float angleThresholdDegrees = 10f)
    {
        // Calculate direction vectors for the lines
        var dir1 = math.normalize(end1 - start1); // Normalize to avoid scale issues
        var dir2 = math.normalize(end2 - start2);

        // Calculate the angle in degrees between the two direction vectors
        var dotProduct = math.dot(dir1, dir2);
        var angleInRadians = math.acos(math.clamp(dotProduct, -1f, 1f)); // Clamp to avoid precision errors
        var angleInDegrees = math.degrees(angleInRadians);

        return angleInDegrees > angleThresholdDegrees;
    }
}