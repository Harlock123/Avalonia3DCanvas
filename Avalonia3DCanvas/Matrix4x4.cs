namespace Avalonia3DCanvas;

public class Matrix4x4
{
    private readonly float[,] _matrix = new float[4, 4];

    public Matrix4x4()
    {
        MakeIdentity();
    }

    public void MakeIdentity()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                _matrix[i, j] = i == j ? 1 : 0;
            }
        }
    }

    public static Matrix4x4 CreateRotationX(float angleRadians)
    {
        var matrix = new Matrix4x4();
        float cos = MathF.Cos(angleRadians);
        float sin = MathF.Sin(angleRadians);

        matrix._matrix[1, 1] = cos;
        matrix._matrix[1, 2] = -sin;
        matrix._matrix[2, 1] = sin;
        matrix._matrix[2, 2] = cos;

        return matrix;
    }

    public static Matrix4x4 CreateRotationY(float angleRadians)
    {
        var matrix = new Matrix4x4();
        float cos = MathF.Cos(angleRadians);
        float sin = MathF.Sin(angleRadians);

        matrix._matrix[0, 0] = cos;
        matrix._matrix[0, 2] = sin;
        matrix._matrix[2, 0] = -sin;
        matrix._matrix[2, 2] = cos;

        return matrix;
    }

    public static Matrix4x4 CreateRotationZ(float angleRadians)
    {
        var matrix = new Matrix4x4();
        float cos = MathF.Cos(angleRadians);
        float sin = MathF.Sin(angleRadians);

        matrix._matrix[0, 0] = cos;
        matrix._matrix[0, 1] = -sin;
        matrix._matrix[1, 0] = sin;
        matrix._matrix[1, 1] = cos;

        return matrix;
    }

    public static Matrix4x4 CreateScale(float sx, float sy, float sz)
    {
        var matrix = new Matrix4x4();
        matrix._matrix[0, 0] = sx;
        matrix._matrix[1, 1] = sy;
        matrix._matrix[2, 2] = sz;
        return matrix;
    }

    public static Matrix4x4 CreateTranslation(float tx, float ty, float tz)
    {
        var matrix = new Matrix4x4();
        matrix._matrix[0, 3] = tx;
        matrix._matrix[1, 3] = ty;
        matrix._matrix[2, 3] = tz;
        return matrix;
    }

    public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
    {
        var result = new Matrix4x4();
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                result._matrix[i, j] = 0;
                for (int k = 0; k < 4; k++)
                {
                    result._matrix[i, j] += a._matrix[i, k] * b._matrix[k, j];
                }
            }
        }
        return result;
    }

    public Vector3D Transform(Vector3D v)
    {
        float x = v.X * _matrix[0, 0] + v.Y * _matrix[0, 1] + v.Z * _matrix[0, 2] + _matrix[0, 3];
        float y = v.X * _matrix[1, 0] + v.Y * _matrix[1, 1] + v.Z * _matrix[1, 2] + _matrix[1, 3];
        float z = v.X * _matrix[2, 0] + v.Y * _matrix[2, 1] + v.Z * _matrix[2, 2] + _matrix[2, 3];
        float w = v.X * _matrix[3, 0] + v.Y * _matrix[3, 1] + v.Z * _matrix[3, 2] + _matrix[3, 3];

        if (w != 0 && w != 1)
        {
            x /= w;
            y /= w;
            z /= w;
        }

        return new Vector3D(x, y, z);
    }
}
