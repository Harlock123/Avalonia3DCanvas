namespace Avalonia3DCanvas;

public struct Vector3D
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Vector3D(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3D operator +(Vector3D a, Vector3D b)
        => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3D operator -(Vector3D a, Vector3D b)
        => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3D operator *(Vector3D v, float scalar)
        => new(v.X * scalar, v.Y * scalar, v.Z * scalar);

    public static Vector3D operator /(Vector3D v, float scalar)
        => new(v.X / scalar, v.Y / scalar, v.Z / scalar);

    public float Length()
        => MathF.Sqrt(X * X + Y * Y + Z * Z);

    public Vector3D Normalize()
    {
        float length = Length();
        return length > 0 ? this / length : this;
    }

    public static float Dot(Vector3D a, Vector3D b)
        => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public static Vector3D Cross(Vector3D a, Vector3D b)
        => new(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );
}
