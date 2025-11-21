namespace Avalonia3DCanvas;

public class Mesh3D
{
    public List<Vector3D> Vertices { get; set; } = new();
    public List<(int, int, int)> Faces { get; set; } = new();

    public void GetBounds(out Vector3D min, out Vector3D max)
    {
        if (Vertices.Count == 0)
        {
            min = new Vector3D(0, 0, 0);
            max = new Vector3D(0, 0, 0);
            return;
        }

        min = Vertices[0];
        max = Vertices[0];

        foreach (var vertex in Vertices)
        {
            min = new Vector3D(
                MathF.Min(min.X, vertex.X),
                MathF.Min(min.Y, vertex.Y),
                MathF.Min(min.Z, vertex.Z)
            );
            max = new Vector3D(
                MathF.Max(max.X, vertex.X),
                MathF.Max(max.Y, vertex.Y),
                MathF.Max(max.Z, vertex.Z)
            );
        }
    }

    public Vector3D GetCenter()
    {
        GetBounds(out var min, out var max);
        return new Vector3D(
            (min.X + max.X) / 2,
            (min.Y + max.Y) / 2,
            (min.Z + max.Z) / 2
        );
    }

    public float GetMaxDimension()
    {
        GetBounds(out var min, out var max);
        float width = max.X - min.X;
        float height = max.Y - min.Y;
        float depth = max.Z - min.Z;
        return MathF.Max(MathF.Max(width, height), depth);
    }
}
