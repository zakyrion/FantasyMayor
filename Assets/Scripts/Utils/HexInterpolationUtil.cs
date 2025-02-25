using System;
using Unity.Mathematics;

/// <summary>
/// Utility class for hexagonal interpolation calculations.
/// </summary>
public static class HexInterpolationUtil
{
    /// <summary>
    /// Converts a position in 3D space to coordinates on a hexagonal texture.
    /// </summary>
    /// <param name="position">The position in 3D space.</param>
    /// <param name="center">The center position of the hexagonal texture.</param>
    /// <param name="textureSize">The size of the hexagonal texture.</param>
    /// <param name="bigHexHalfRadius">The half radius of the big hexagon.</param>
    /// <returns>The coordinates on the hexagonal texture.</returns>
    public static int2 PositionToCoords(float3 position, float3 center, int textureSize, float bigHexHalfRadius)
    {
        var hexHalfHeight = math.sqrt(3) / 2f * bigHexHalfRadius;
        var heightToCenter = math.abs((center - position).z);

        var width = math.lerp(bigHexHalfRadius * 2, bigHexHalfRadius, heightToCenter / hexHalfHeight);
        var height = hexHalfHeight * 2;

        var xBorder = center.x - width / 2f;
        var yBorder = center.z - hexHalfHeight;

        var fromBorderX = position.x - xBorder;
        var fromBorderY = position.z - yBorder;

        var xCenter = (int) math.floor(fromBorderX / width * textureSize);
        var yCenter = (int) math.floor(fromBorderY / height * textureSize);

        return new int2(xCenter, yCenter);
    }

    /// <summary>
    /// Converts a position in 3D space to coordinates on a hexagonal texture.
    /// </summary>
    /// <param name="position">The position in 3D space.</param>
    /// <param name="center">The center position of the hexagonal texture.</param>
    /// <param name="textureSize">The size of the hexagonal texture.</param>
    /// <param name="bigHexHalfRadius">The half radius of the big hexagon.</param>
    /// <returns>The coordinates on the hexagonal texture as a tuple containing the minimum coordinates, next coordinates, and a coefficient for interpolation.</returns>
    public static (int2 min, int2 next, float2 koef) PositionToCoordsTuple(float3 position, float3 center,
        int textureSize, float bigHexHalfRadius)
    {
        textureSize--;
        var hexHalfHeight = math.sqrt(3) / 2f * bigHexHalfRadius;
        var heightToCenter = math.abs((center - position).z);

        var width = math.lerp(bigHexHalfRadius * 2, bigHexHalfRadius, heightToCenter / hexHalfHeight);
        var height = hexHalfHeight * 2;

        var xBorder = center.x - width / 2f;
        var yBorder = center.z - hexHalfHeight;

        var fromBorderX = position.x - xBorder;
        var fromBorderY = position.z - yBorder;

        var xCenter = (int) math.floor(fromBorderX / width * textureSize);
        var yCenter = (int) math.floor(fromBorderY / height * textureSize);
        
        var xNext = math.min(textureSize + 1, xCenter + 1);
        var yNext = math.min(textureSize + 1, yCenter + 1);

        var xDelta = fromBorderX / width * textureSize - xCenter;
        var yDelta = fromBorderY / height * textureSize - yCenter;

        return (new int2(xCenter, yCenter), new int2(xNext, yNext), new float2(xDelta, yDelta));
    }

    /// <summary>
    /// Converts coordinates on a hexagonal texture to a position in 3D space.
    /// </summary>
    /// <param name="coords">The coordinates on the hexagonal texture.</param>
    /// <param name="center">The center position of the hexagonal texture.</param>
    /// <param name="textureSize">The size of the hexagonal texture.</param>
    /// <param name="bigHexHalfRadius">The half radius of the big hexagon.</param>
    /// <returns>The position in 3D space.</returns>
    public static float3 CoordToPosition(int2 coords, float3 center, int textureSize, float bigHexHalfRadius)
    {
        var hexHalfHeight = math.sqrt(3) / 2f * bigHexHalfRadius;
        var height = hexHalfHeight * 2;

        var yBorder = center.z - hexHalfHeight;
        var positionZ = yBorder + coords.y / (float) textureSize * height;

        // Calculate the height from the center of the hex to the given y coordinate
        var heightToCenter = math.abs(center.z - positionZ);

        // Calculate the total width of the hex at the given height
        var width = math.lerp(bigHexHalfRadius * 2, bigHexHalfRadius, heightToCenter / hexHalfHeight);
        var xBorder = center.x - width / 2f;
        var positionX = xBorder + coords.x / (float) textureSize * width;

        // Assuming y is up/down in Unity's coordinate system, and the hex lies on the xz plane
        return new float3(positionX, 0, positionZ);
    }

    /// <summary>
    /// Determines whether a given point is inside a triangle.
    /// </summary>
    /// <param name="p">The point to check.</param>
    /// <param name="a">The first vertex of the triangle.</param>
    /// <param name="b">The second vertex of the triangle.</param>
    /// <param name="c">The third vertex of the triangle.</param>
    /// <returns>True if the point is inside the triangle, false otherwise.</returns>
    public static bool IsInside(float3 p, float3 a, float3 b, float3 c)
    {
        var v0 = c - a;
        var v1 = b - a;
        var v2 = p - a;

        var dot00 = math.dot(v0, v0);
        var dot01 = math.dot(v0, v1);
        var dot02 = math.dot(v0, v2);
        var dot11 = math.dot(v1, v1);
        var dot12 = math.dot(v1, v2);

        var invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        var v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return u >= 0 && v >= 0 && u + v < 1;
    }

    /// <summary>
    /// Determines whether a point is inside a triangle in 2D space.
    /// </summary>
    /// <param name="p">The point to check.</param>
    /// <param name="a">The first vertex of the triangle.</param>
    /// <param name="b">The second vertex of the triangle.</param>
    /// <param name="c">The third vertex of the triangle.</param>
    /// <returns>True if the point is inside the triangle, otherwise false.</returns>
    public static bool IsInside(float2 p, float2 a, float2 b, float2 c)
    {
        var v0 = c - a;
        var v1 = b - a;
        var v2 = p - a;

        var dot00 = math.dot(v0, v0);
        var dot01 = math.dot(v0, v1);
        var dot02 = math.dot(v0, v2);
        var dot11 = math.dot(v1, v1);
        var dot12 = math.dot(v1, v2);

        var invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        var v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return u >= 0 && v >= 0 && u + v < 1;
    }

    /// <summary>
    /// Interpolates a value based on the position and the values of three points.
    /// </summary>
    /// <param name="point">The position to interpolate the value for.</param>
    /// <param name="A">The first point.</param>
    /// <param name="B">The second point.</param>
    /// <param name="C">The third point.</param>
    /// <param name="valA">The value at the first point.</param>
    /// <param name="valB">The value at the second point.</param>
    /// <param name="valC">The value at the third point.</param>
    /// <returns>The interpolated value.</returns>
    public static float2 InterpolateValue(float3 point, float3 A, float3 B, float3 C, float2 valA, float2 valB,
        float2 valC)
    {
        if (math.distance(point.xz, A.xz) < 0.001f)
        {
            return valA;
        }

        if (math.distance(point.xz, B.xz) < 0.001f)
        {
            return valB;
        }

        if (math.distance(point.xz, C.xz) < 0.001f)
        {
            return valC;
        }

        //TODO check if point is on the edge

        /*if (math.distance(point, A) + math.distance(point, B) - math.distance(A, B) < 0.001f)
        {
            var coef = math.distance(A.xz, point.xz) / math.distance(A.xz, B.xz);
            return math.lerp(new float2(0.5f, 0.5f), valB, coef);
        }
        else if (math.distance(point, A) + math.distance(point, C) - math.distance(A, C) < 0.001f)
        {
            var coef = math.distance(A.xz, point.xz) / math.distance(A.xz, B.xz);
            return math.lerp(new float2(0.5f, 0.5f), valC, coef);
        }
        else*/
        {
            var intersectPoint = GetIntersectPoint(A, B, C, point);

            var coef = math.distance(A.xz, intersectPoint.Item1.xz) / math.distance(C.xz, A.xz);

            var UVa = math.lerp(valA, valB, coef);
            var UVb = math.lerp(valA, valC, coef);

            var coefUV = math.distance(intersectPoint.Item1.xz, new float2(point.x, point.z)) /
                         math.distance(intersectPoint.Item1.xz, intersectPoint.Item2.xz);

            // Interpolate the value at P based on distances to A, B, and C
            return math.lerp(UVa, UVb, coefUV);
        }
    }

    /// <summary>
    /// Interpolates a value based on the position of a point within a triangle defined by points A, B, and C.
    /// </summary>
    /// <param name="point">The position of the point to interpolate the value for.</param>
    /// <param name="A">The first vertex of the triangle.</param>
    /// <param name="B">The second vertex of the triangle.</param>
    /// <param name="C">The third vertex of the triangle.</param>
    /// <param name="valA">The value corresponding to point A.</param>
    /// <param name="valB">The value corresponding to point B.</param>
    /// <param name="valC">The value corresponding to point C.</param>
    /// <returns>The interpolated value at the given point.</returns>
    public static float InterpolateValue(float3 point, float3 A, float3 B, float3 C, float valA, float valB,
        float valC)
    {
        if (math.distance(point.xz, A.xz) < 0.001f)
        {
            return valA;
        }

        if (math.distance(point.xz, B.xz) < 0.001f)
        {
            return valB;
        }

        if (math.distance(point.xz, C.xz) < 0.001f)
        {
            return valC;
        }

        var intersectPoint = GetIntersectPoint(A, B, C, point);

        var coef = math.distance(A.xz, intersectPoint.Item1.xz) / math.distance(C.xz, A.xz);

        var UVa = math.lerp(valA, valB, coef);
        var UVb = math.lerp(valA, valC, coef);

        var coefUV = math.distance(intersectPoint.Item1.xz, new float2(point.x, point.z)) /
                     math.distance(intersectPoint.Item1.xz, intersectPoint.Item2.xz);

        // Interpolate the value at P based on distances to A, B, and C
        return math.lerp(UVa, UVb, coefUV);
    }

    /// <summary>
    /// Converts UV coordinates to a position in 3D space.
    /// </summary>
    /// <param name="point">The UV coordinates.</param>
    /// <param name="uvA">The UV coordinates of point A.</param>
    /// <param name="uvB">The UV coordinates of point B.</param>
    /// <param name="uvC">The UV coordinates of point C.</param>
    /// <param name="positionA">The position of point A in 3D space.</param>
    /// <param name="positionB">The position of point B in 3D space.</param>
    /// <param name="positionC">The position of point C in 3D space.</param>
    /// <returns>The position in 3D space corresponding to the given UV coordinates.</returns>
    public static float3 FromUVToPosition(float2 point, float2 uvA, float2 uvB, float2 uvC, float3 positionA,
        float3 positionB,
        float3 positionC)
    {
        if (math.distance(point, uvA) < 0.001f)
        {
            return positionA;
        }

        if (math.distance(point, uvB) < 0.001f)
        {
            return positionB;
        }

        if (math.distance(point, uvC) < 0.001f)
        {
            return positionC;
        }

        var intersectPoint = GetIntersectPoint(uvA, uvB, uvC, point);

        var coef = math.distance(uvA, intersectPoint.Item1) / math.distance(uvC, uvA);

        var posA = math.lerp(positionA, positionB, coef);
        var posB = math.lerp(positionA, positionC, coef);

        var coefUV = math.distance(intersectPoint.Item1, point) /
                     math.distance(intersectPoint.Item1, intersectPoint.Item2);

        // Interpolate the value at P based on distances to A, B, and C
        return math.lerp(posA, posB, coefUV);
    }

    /// <summary>
    /// Calculates the intersection points between a triangle and a point in 3D space.
    /// </summary>
    /// <param name="a">The first vertex of the triangle.</param>
    /// <param name="b">The second vertex of the triangle.</param>
    /// <param name="c">The third vertex of the triangle.</param>
    /// <param name="point">The point in 3D space.</param>
    /// <returns>
    /// A tuple containing two intersection points between the triangle and the point.
    /// The first item is the intersection point with the second vertex of the triangle.
    /// The second item is the intersection point with the third vertex of the triangle.
    /// </returns>
    public static (float3, float3) GetIntersectPoint(float3 a, float3 b, float3 c, float3 point)
    {
        var directionA = math.normalize(b - c);
        var directionB = math.normalize(c - b);

        var intersectionB = FindIntersection(a.xz, b.xz, point.xz, (point + directionA * 100f).xz);
        var intersectionC = FindIntersection(a.xz, c.xz, point.xz, (point + directionB * 100f).xz);

        return new ValueTuple<float3, float3>(new float3(intersectionB.x, 0, intersectionB.y),
            new float3(intersectionC.x, 0, intersectionC.y));
    }

    /// <summary>
    /// Calculates the intersection points between three given 2D vectors and another given 2D vector.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <param name="c">The third vector.</param>
    /// <param name="point">The fourth vector to intersect with the others.</param>
    /// <returns>A tuple containing the two intersection points as float2 values.</returns>
    public static (float2, float2) GetIntersectPoint(float2 a, float2 b, float2 c, float2 point)
    {
        var directionA = math.normalize(b - c);
        var directionB = math.normalize(c - b);

        var intersectionB = FindIntersection(a, b, point, point + directionA * 100f);
        var intersectionC = FindIntersection(a, c, point, point + directionB * 100f);

        return new ValueTuple<float2, float2>(intersectionB, intersectionC);
    }

    /// <summary>
    /// Finds the intersection point between two lines in 2D space.
    /// </summary>
    /// <param name="A1">The starting point of the first line.</param>
    /// <param name="B1">The ending point of the first line.</param>
    /// <param name="A2">The starting point of the second line.</param>
    /// <param name="B2">The ending point of the second line.</param>
    /// <returns>The intersection point between the two lines.</returns>
    private static float2 FindIntersection(float2 A1, float2 B1, float2 A2, float2 B2)
    {
        var result = new float2(float.NaN, float.NaN); // Initialize as NaN

        var a = B1 - A1;
        var b = B2 - A2;

        var cross = Cross(a, b);

        if (math.abs(cross) < 1e-8f)
        {
            return result; // Lines are parallel or coincident, no unique intersection
        }

        var c = A2 - A1;
        var t = Cross(c, b) / cross;

        result = A1 + t * a;

        return result;
    }

    public static float Cross(float2 u, float2 v) => u.x * v.y - u.y * v.x;
    
    public static int2 UVToCoords(float2 uv, int textureSize)
    {
        return new int2((int) math.floor(uv.x * textureSize), (int) math.floor(uv.y * textureSize));
    }
}