using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia3DCanvas;

// Helper class to extract paths from StreamGeometry
internal class PathExtractorContext : IGeometryContext
{
    public List<List<Point>> Paths { get; } = new();
    private List<Point>? _currentPath;
    private Point _currentPoint;

    public void BeginFigure(Point startPoint, bool isFilled)
    {
        _currentPath = new List<Point> { startPoint };
        _currentPoint = startPoint;
    }

    public void LineTo(Point point)
    {
        _currentPath?.Add(point);
        _currentPoint = point;
    }

    public void CubicBezierTo(Point point1, Point point2, Point point3)
    {
        // Approximate with line segments
        var points = ApproximateCubicBezier(_currentPoint, point1, point2, point3, 10);
        _currentPath?.AddRange(points.Skip(1));
        _currentPoint = point3;
    }

    public void QuadraticBezierTo(Point point1, Point point2)
    {
        // Approximate with line segments
        var points = ApproximateQuadraticBezier(_currentPoint, point1, point2, 10);
        _currentPath?.AddRange(points.Skip(1));
        _currentPoint = point2;
    }

    public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection)
    {
        // Simple approximation - just line to the end point
        // A proper implementation would calculate arc points
        _currentPath?.Add(point);
        _currentPoint = point;
    }

    public void EndFigure(bool isClosed)
    {
        if (_currentPath != null && _currentPath.Count >= 3)
        {
            if (isClosed && _currentPath[0] != _currentPath[_currentPath.Count - 1])
            {
                _currentPath.Add(_currentPath[0]);
            }
            Paths.Add(_currentPath);
        }
        _currentPath = null;
    }

    public void SetFillRule(FillRule fillRule)
    {
        // Not needed for path extraction
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    private static List<Point> ApproximateCubicBezier(Point p0, Point p1, Point p2, Point p3, int segments)
    {
        var points = new List<Point>();
        for (int i = 0; i <= segments; i++)
        {
            double t = i / (double)segments;
            double t2 = t * t;
            double t3 = t2 * t;
            double mt = 1 - t;
            double mt2 = mt * mt;
            double mt3 = mt2 * mt;

            double x = mt3 * p0.X + 3 * mt2 * t * p1.X + 3 * mt * t2 * p2.X + t3 * p3.X;
            double y = mt3 * p0.Y + 3 * mt2 * t * p1.Y + 3 * mt * t2 * p2.Y + t3 * p3.Y;

            points.Add(new Point(x, y));
        }
        return points;
    }

    private static List<Point> ApproximateQuadraticBezier(Point p0, Point p1, Point p2, int segments)
    {
        var points = new List<Point>();
        for (int i = 0; i <= segments; i++)
        {
            double t = i / (double)segments;
            double t2 = t * t;
            double mt = 1 - t;
            double mt2 = mt * mt;

            double x = mt2 * p0.X + 2 * mt * t * p1.X + t2 * p2.X;
            double y = mt2 * p0.Y + 2 * mt * t * p1.Y + t2 * p2.Y;

            points.Add(new Point(x, y));
        }
        return points;
    }
}

public static class TextMeshGenerator
{
    public static Mesh3D GenerateTextMesh(string text, string fontFamily, double fontSize, double extrusionDepth)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Text cannot be null or empty", nameof(text));

        // Use SkiaSharp for reliable text-to-path conversion
        return GenerateTextMeshWithSkia(text, fontFamily, fontSize, extrusionDepth);
    }

    private static Mesh3D GenerateTextMeshWithSkia(string text, string fontFamily, double fontSize, double extrusionDepth)
    {
        using var typeface = SKTypeface.FromFamilyName(fontFamily);

        // Use the older SKPaint API for SkiaSharp 2.88.x
        #pragma warning disable CS0618 // Type or member is obsolete
        using var paint = new SKPaint
        {
            Typeface = typeface,
            TextSize = (float)fontSize,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        using var path = paint.GetTextPath(text, 0, 0);
        #pragma warning restore CS0618

        if (path == null)
        {
            System.Diagnostics.Debug.WriteLine("SkiaSharp GetTextPath returned null");
            return new Mesh3D();
        }

        System.Diagnostics.Debug.WriteLine($"SkiaSharp path bounds: {path.Bounds}");

        var paths = ExtractPathsFromSkPath(path);
        System.Diagnostics.Debug.WriteLine($"Extracted {paths.Count} paths from SkiaSharp");

        return PathsToMesh(paths, extrusionDepth);
    }

    private static List<List<Point>> ExtractPathsFromSkPath(SKPath skPath)
    {
        var paths = new List<List<Point>>();
        var currentPath = new List<Point>();

        using var iterator = skPath.CreateIterator(false);
        var points = new SKPoint[4];
        SKPathVerb verb;

        while ((verb = iterator.Next(points)) != SKPathVerb.Done)
        {
            switch (verb)
            {
                case SKPathVerb.Move:
                    if (currentPath.Count > 0)
                    {
                        if (currentPath.Count >= 3)
                            paths.Add(currentPath);
                        currentPath = new List<Point>();
                    }
                    currentPath.Add(new Point(points[0].X, points[0].Y));
                    break;

                case SKPathVerb.Line:
                    currentPath.Add(new Point(points[1].X, points[1].Y));
                    break;

                case SKPathVerb.Quad:
                    // Quadratic bezier - approximate with line segments (increased from 10 to 20 for smoother curves)
                    var quadPoints = ApproximateQuadraticBezierSK(points[0], points[1], points[2], 20);
                    currentPath.AddRange(quadPoints.Skip(1));
                    break;

                case SKPathVerb.Cubic:
                    // Cubic bezier - approximate with line segments (increased from 10 to 20 for smoother curves)
                    var cubicPoints = ApproximateCubicBezierSK(points[0], points[1], points[2], points[3], 20);
                    currentPath.AddRange(cubicPoints.Skip(1));
                    break;

                case SKPathVerb.Close:
                    if (currentPath.Count > 0 && currentPath[0] != currentPath[currentPath.Count - 1])
                    {
                        currentPath.Add(currentPath[0]);
                    }
                    break;
            }
        }

        if (currentPath.Count >= 3)
            paths.Add(currentPath);

        return paths;
    }

    private static List<Point> ApproximateQuadraticBezierSK(SKPoint p0, SKPoint p1, SKPoint p2, int segments)
    {
        var points = new List<Point>();
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float t2 = t * t;
            float mt = 1 - t;
            float mt2 = mt * mt;

            float x = mt2 * p0.X + 2 * mt * t * p1.X + t2 * p2.X;
            float y = mt2 * p0.Y + 2 * mt * t * p1.Y + t2 * p2.Y;

            points.Add(new Point(x, y));
        }
        return points;
    }

    private static List<Point> ApproximateCubicBezierSK(SKPoint p0, SKPoint p1, SKPoint p2, SKPoint p3, int segments)
    {
        var points = new List<Point>();
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float t2 = t * t;
            float t3 = t2 * t;
            float mt = 1 - t;
            float mt2 = mt * mt;
            float mt3 = mt2 * mt;

            float x = mt3 * p0.X + 3 * mt2 * t * p1.X + 3 * mt * t2 * p2.X + t3 * p3.X;
            float y = mt3 * p0.Y + 3 * mt2 * t * p1.Y + 3 * mt * t2 * p2.Y + t3 * p3.Y;

            points.Add(new Point(x, y));
        }
        return points;
    }

    private static Mesh3D PathsToMesh(List<List<Point>> paths, double extrusionDepth)
    {
        var mesh = new Mesh3D();
        float depth = (float)extrusionDepth;

        foreach (var path in paths)
        {
            if (path.Count < 3)
                continue;

            int baseIndex = mesh.Vertices.Count;

            // Add front face vertices (z = 0)
            foreach (var point in path)
            {
                mesh.Vertices.Add(new Vector3D((float)point.X, (float)point.Y, 0));
            }

            // Add back face vertices (z = depth)
            foreach (var point in path)
            {
                mesh.Vertices.Add(new Vector3D((float)point.X, (float)point.Y, depth));
            }

            int vertexCount = path.Count;

            // ONLY create side faces (no front/back faces)
            // This avoids triangulation issues with concave polygons
            // and creates a clean wireframe extrusion effect
            for (int i = 0; i < vertexCount - 1; i++)
            {
                int frontCurr = baseIndex + i;
                int frontNext = baseIndex + i + 1;
                int backCurr = baseIndex + vertexCount + i;
                int backNext = baseIndex + vertexCount + i + 1;

                // Create two triangles for each quad on the side
                mesh.Faces.Add((frontCurr, backCurr, frontNext));
                mesh.Faces.Add((frontNext, backCurr, backNext));
            }
        }

        return mesh;
    }

    private static Mesh3D GeometryToMesh(Geometry geometry, double extrusionDepth)
    {
        var mesh = new Mesh3D();
        var paths = ExtractPaths(geometry);

        System.Diagnostics.Debug.WriteLine($"Extracted {paths.Count} paths from geometry");

        if (paths.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("No paths found in geometry!");
            return mesh;
        }

        float depth = (float)extrusionDepth;

        // For each closed path, create extruded geometry
        foreach (var path in paths)
        {
            if (path.Count < 3)
                continue;

            // Get the base vertex index for this path
            int baseIndex = mesh.Vertices.Count;

            // Add front face vertices (z = 0)
            foreach (var point in path)
            {
                mesh.Vertices.Add(new Vector3D((float)point.X, (float)point.Y, 0));
            }

            // Add back face vertices (z = depth)
            foreach (var point in path)
            {
                mesh.Vertices.Add(new Vector3D((float)point.X, (float)point.Y, depth));
            }

            int vertexCount = path.Count;

            // Triangulate front face (simple fan triangulation from first vertex)
            for (int i = 1; i < vertexCount - 1; i++)
            {
                mesh.Faces.Add((baseIndex, baseIndex + i + 1, baseIndex + i));
            }

            // Triangulate back face (reversed winding)
            int backBase = baseIndex + vertexCount;
            for (int i = 1; i < vertexCount - 1; i++)
            {
                mesh.Faces.Add((backBase, backBase + i, backBase + i + 1));
            }

            // Create side faces (quads as two triangles)
            for (int i = 0; i < vertexCount; i++)
            {
                int next = (i + 1) % vertexCount;

                int frontCurr = baseIndex + i;
                int frontNext = baseIndex + next;
                int backCurr = baseIndex + vertexCount + i;
                int backNext = baseIndex + vertexCount + next;

                // Two triangles for each side quad
                mesh.Faces.Add((frontCurr, backCurr, frontNext));
                mesh.Faces.Add((frontNext, backCurr, backNext));
            }
        }

        return mesh;
    }

    private static List<List<Point>> ExtractPaths(Geometry geometry)
    {
        var paths = new List<List<Point>>();

        System.Diagnostics.Debug.WriteLine($"ExtractPaths called with geometry type: {geometry.GetType().Name}");

        if (geometry is PathGeometry pathGeometry)
        {
            System.Diagnostics.Debug.WriteLine("Processing as PathGeometry");
            return ExtractPathsFromPathGeometry(pathGeometry);
        }
        else if (geometry is GeometryGroup geometryGroup)
        {
            System.Diagnostics.Debug.WriteLine($"Processing as GeometryGroup with {geometryGroup.Children.Count} children");
            foreach (var child in geometryGroup.Children)
            {
                paths.AddRange(ExtractPaths(child));
            }
        }
        else if (geometry is StreamGeometry streamGeometry)
        {
            System.Diagnostics.Debug.WriteLine("Processing as StreamGeometry");
            System.Diagnostics.Debug.WriteLine($"Geometry bounds: {streamGeometry.Bounds}");

            // StreamGeometry in Avalonia is write-only by design for performance
            // It doesn't expose its path data for reading
            // Note: Text rendering uses SkiaSharp directly (see GenerateTextMeshWithSkia)
            // so this code path is not used for text mesh generation

            System.Diagnostics.Debug.WriteLine("Warning: StreamGeometry path extraction not supported.");
            System.Diagnostics.Debug.WriteLine("For text rendering, use CreateTextMesh() which uses SkiaSharp.");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Unknown geometry type: {geometry.GetType().Name}");
        }

        return paths;
    }

    private static List<List<Point>> ExtractPathsFromPathGeometry(PathGeometry? pathGeometry)
    {
        var paths = new List<List<Point>>();

        if (pathGeometry?.Figures == null)
            return paths;

        foreach (var figure in pathGeometry.Figures)
        {
            var path = new List<Point>();
            var currentPoint = figure.StartPoint;
            path.Add(currentPoint);

            if (figure.Segments == null)
                continue;

            foreach (var segment in figure.Segments)
            {
                if (segment is LineSegment lineSegment)
                {
                    currentPoint = lineSegment.Point;
                    path.Add(currentPoint);
                }
                else if (segment is PolyLineSegment polyLineSegment)
                {
                    foreach (var point in polyLineSegment.Points)
                    {
                        currentPoint = point;
                        path.Add(currentPoint);
                    }
                }
                else if (segment is BezierSegment bezierSegment)
                {
                    // Approximate bezier curve with line segments
                    var points = ApproximateBezier(currentPoint, bezierSegment.Point1,
                        bezierSegment.Point2, bezierSegment.Point3, 10);
                    path.AddRange(points.Skip(1)); // Skip first point as it's already added
                    currentPoint = bezierSegment.Point3;
                }
                else if (segment is QuadraticBezierSegment quadBezierSegment)
                {
                    // Approximate quadratic bezier
                    var points = ApproximateQuadraticBezier(currentPoint,
                        quadBezierSegment.Point1, quadBezierSegment.Point2, 10);
                    path.AddRange(points.Skip(1));
                    currentPoint = quadBezierSegment.Point2;
                }
                else if (segment is PolyBezierSegment polyBezierSegment)
                {
                    if (polyBezierSegment.Points == null)
                        continue;

                    for (int i = 0; i < polyBezierSegment.Points.Count; i += 3)
                    {
                        if (i + 2 < polyBezierSegment.Points.Count)
                        {
                            var points = ApproximateBezier(currentPoint,
                                polyBezierSegment.Points[i],
                                polyBezierSegment.Points[i + 1],
                                polyBezierSegment.Points[i + 2], 10);
                            path.AddRange(points.Skip(1));
                            currentPoint = polyBezierSegment.Points[i + 2];
                        }
                    }
                }
                else if (segment is ArcSegment arcSegment)
                {
                    // Approximate arc with line segments
                    var points = ApproximateArc(currentPoint, arcSegment, 20);
                    path.AddRange(points.Skip(1));
                    currentPoint = arcSegment.Point;
                }
            }

            // Close the path if needed
            if (figure.IsClosed && path.Count > 0)
            {
                if (path[0] != path[path.Count - 1])
                {
                    path.Add(path[0]);
                }
            }

            if (path.Count >= 3)
            {
                paths.Add(path);
            }
        }

        return paths;
    }

    private static List<Point> ApproximateBezier(Point p0, Point p1, Point p2, Point p3, int segments)
    {
        var points = new List<Point>();
        for (int i = 0; i <= segments; i++)
        {
            double t = i / (double)segments;
            double t2 = t * t;
            double t3 = t2 * t;
            double mt = 1 - t;
            double mt2 = mt * mt;
            double mt3 = mt2 * mt;

            double x = mt3 * p0.X + 3 * mt2 * t * p1.X + 3 * mt * t2 * p2.X + t3 * p3.X;
            double y = mt3 * p0.Y + 3 * mt2 * t * p1.Y + 3 * mt * t2 * p2.Y + t3 * p3.Y;

            points.Add(new Point(x, y));
        }
        return points;
    }

    private static List<Point> ApproximateQuadraticBezier(Point p0, Point p1, Point p2, int segments)
    {
        var points = new List<Point>();
        for (int i = 0; i <= segments; i++)
        {
            double t = i / (double)segments;
            double t2 = t * t;
            double mt = 1 - t;
            double mt2 = mt * mt;

            double x = mt2 * p0.X + 2 * mt * t * p1.X + t2 * p2.X;
            double y = mt2 * p0.Y + 2 * mt * t * p1.Y + t2 * p2.Y;

            points.Add(new Point(x, y));
        }
        return points;
    }

    private static List<Point> ApproximateArc(Point startPoint, ArcSegment arc, int segments)
    {
        // Simple linear approximation for arcs
        // A proper implementation would calculate the actual arc points
        var points = new List<Point>();
        for (int i = 0; i <= segments; i++)
        {
            double t = i / (double)segments;
            double x = startPoint.X + t * (arc.Point.X - startPoint.X);
            double y = startPoint.Y + t * (arc.Point.Y - startPoint.Y);
            points.Add(new Point(x, y));
        }
        return points;
    }
}
