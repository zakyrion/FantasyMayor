using System;
using Unity.Mathematics;
using UnityEngine;

namespace DataTypes
{
    public struct Line
    {
        private const float EPSILON = 0.01f;
        public float3 A { get; }
        public float3 B { get; }

        public Line(float3 a, float3 b)
        {
            A = a;
            B = b;
        }

        public void Draw(Color color, float duration = 10)
        {
            Debug.DrawLine(A, B, color, duration);
        }

        public static bool operator ==(Line a, Line b) =>
            (math.distance(a.A, b.A) <= math.EPSILON && math.distance(a.B, b.B) <= math.EPSILON) ||
            (math.distance(a.A, b.B) <= math.EPSILON && math.distance(a.B, b.A) <= math.EPSILON);

        public static bool operator !=(Line a, Line b) => !(a == b);

        public bool CanBeSegment(Line line) => math.distance(A, line.A) <= EPSILON ||
                                               math.distance(A, line.B) <= EPSILON ||
                                               math.distance(B, line.A) <= EPSILON ||
                                               math.distance(B, line.B) <= EPSILON;

        public bool Intersect(float3 start, float3 end)
        {
            var p = A.xz;
            var r = (B - A).xz;
            var q = start.xz;
            var s = (end - start).xz;

            var rxs = Cross2D(r, s);
            var qpxr = Cross2D(q - p, r);

            // If r x s = 0 and (q − p) x r = 0, then the two lines are collinear
            if (rxs == 0 && qpxr == 0) return false; // Lines are collinear

            // If r x s = 0 and (q − p) x r ≠ 0 , then the two lines are parallel and non-intersecting
            if (rxs == 0 && !(qpxr == 0)) return false; // Lines are parallel

            // t = (q − p) x s / (r x s)
            var t = Cross2D(q - p, s) / rxs;

            // u = (q − p) x r / (r x s)
            var u = Cross2D(q - p, r) / rxs;

            return 0 <= t && t <= 1 && 0 <= u && u <= 1;
        }

        public float3 Intersection(float3 lineStart, float3 lineEnd)
        {
            var p = A.xz;
            var r = (B - A).xz;
            var q = lineStart.xz;
            var s = (lineEnd - lineStart).xz;

            var rxs = Cross2D(r, s);
            var qpxr = Cross2D(q - p, r);

            // If r x s = 0 and (q − p) x r = 0, then the two lines are collinear
            if (rxs == 0 && qpxr == 0) throw new InvalidOperationException("The lines are collinear.");

            // If r x s = 0 and (q − p) x r ≠ 0 , then the two lines are parallel and non-intersecting
            if (rxs == 0 && !(qpxr == 0))
                throw new InvalidOperationException("The lines are parallel and do not intersect.");

            // t = (q − p) x s / (r x s)
            var t = Cross2D(q - p, s) / rxs;

            // u = (q − p) x r / (r x s)
            var u = Cross2D(q - p, r) / rxs;

            if (0 <= t && t <= 1 && 0 <= u && u <= 1)
            {
                // If 0 <= t <= 1 and 0 <= u <= 1, the intersection point is p + t r
                var intersectionPoint = A + t * (B - A);

                // We set the y coordinate of the intersection point by linear interpolation of the y-coordinates of the line segment
                intersectionPoint.y = math.lerp(A.y, B.y, t);

                return intersectionPoint;
            }

            throw new InvalidOperationException("The lines do not intersect within the segments.");
        }

        public static bool Intersection(float3 lineStartA, float3 lineEndA, float3 lineStartB, float3 lineEndB, ref float3 result)
        {
            var p = lineStartB.xz;
            var r = (lineEndB - lineStartB).xz;
            var q = lineStartA.xz;
            var s = (lineEndA - lineStartA).xz;

            var rxs = Cross2D(r, s);
            var qpxr = Cross2D(q - p, r);

            // If r x s = 0 and (q − p) x r = 0, then the two lines are collinear
            if (rxs == 0 && qpxr == 0) throw new InvalidOperationException("The lines are collinear.");

            // If r x s = 0 and (q − p) x r ≠ 0 , then the two lines are parallel and non-intersecting
            if (rxs == 0 && !(qpxr == 0))
                throw new InvalidOperationException("The lines are parallel and do not intersect.");

            // t = (q − p) x s / (r x s)
            var t = Cross2D(q - p, s) / rxs;

            // u = (q − p) x r / (r x s)
            var u = Cross2D(q - p, r) / rxs;

            if (0 <= t && t <= 1 && 0 <= u && u <= 1)
            {
                // If 0 <= t <= 1 and 0 <= u <= 1, the intersection point is p + t r
                var intersectionPoint = lineStartB + t * (lineEndB - lineStartB);

                // We set the y coordinate of the intersection point by linear interpolation of the y-coordinates of the line segment
                intersectionPoint.y = math.lerp(lineStartB.y, lineEndB.y, t);

                result = intersectionPoint;
                return true;
            }

            return false;
        }


        public bool IsPointOnLine(float3 point)
        {
            var cross = Cross2D(point.xz - A.xz, B.xz - A.xz);
            return math.abs(cross) < 0.01f;
        }

        public bool IsPointBetween(float3 point) =>
            math.abs(math.distance(A, B) -
                     (math.distance(A, point) +
                      math.distance(B, point))) <= .01f;

        public bool UpdatePointsY(ref float3 point)
        {
            var cross = Cross2D(point.xz - A.xz, B.xz - A.xz);
            if (math.abs(cross) < 0.01f)
            {
                var lerp = math.distance(A.xz, point.xz) / math.distance(A, B);
                var y = math.lerp(A.y, B.y, lerp);
                point.y = y;

                return true;
            }

            return false;
        }

        public bool Intersect(Line line) => Intersect(line.A, line.B);

        public float Cross2D() => A.x * B.y - A.y * B.x;

        private static float Cross2D(float2 a, float2 b) => a.x * b.y - a.y * b.x;
    }

    public struct LineWithCenter
    {
        public float3 CenterOfHexTriangle { get; }
        public Line Line { get; private set; }

        public float3 Start => Line.A;
        public float3 End => Line.B;

        public float3 A => Line.A;
        public float3 B => Line.B;

        public float Length => math.distance(Start, End);

        public LineWithCenter(float3 centerOfHexTriangle, Line line, bool setHeight = false)
        {
            CenterOfHexTriangle = centerOfHexTriangle;

            if (setHeight)
            {
                var newA = line.A;
                var newB = line.B;

                newA.y = centerOfHexTriangle.y;
                newB.y = centerOfHexTriangle.y;

                line = new Line(newA, newB);
            }

            Line = line;
        }

        public LineWithCenter(float3 centerOfHexTriangle, float3 a, float3 b, bool setHeight = false)
        {
            CenterOfHexTriangle = centerOfHexTriangle;

            if (setHeight)
            {
                a.y = centerOfHexTriangle.y;
                b.y = centerOfHexTriangle.y;
            }

            Line = new Line(a, b);
        }

        public void Draw(Color color, float duration = 10)
        {
            Line.Draw(color, duration);
        }

        public void Flip()
        {
            Line = new Line(Line.B, Line.A);
        }

        public bool HaveSameCenter(LineWithCenter line) =>
            math.distance(CenterOfHexTriangle, line.CenterOfHexTriangle) < 0.01f;

        public float3 Intersection(LineWithCenter line) => Line.Intersection(line.Start, line.End);

        public static implicit operator Line(LineWithCenter l) => l.Line;

        public static bool operator ==(LineWithCenter a, LineWithCenter b) => a.Line == b.Line;

        public static bool operator !=(LineWithCenter a, LineWithCenter b) => !(a == b);

        public bool CanBeSegment(LineWithCenter line) => Line.CanBeSegment(line);

        public void SetHeight(float height)
        {
            Line = new Line(new float3(Line.A.x, height, Line.A.z), new float3(Line.B.x, height, Line.B.z));
        }

        public bool IsPointOnLine(float3 point) => Line.IsPointOnLine(point);

        public bool IsPointBetween(float3 point) => Line.IsPointBetween(point);
    }
}