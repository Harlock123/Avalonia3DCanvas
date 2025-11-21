using System.Globalization;
using System.IO;

namespace Avalonia3DCanvas;

public static class ModelPLYLoader
{
    public static Mesh3D Load(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = new StreamReader(stream);

        var mesh = new Mesh3D();
        int vertexCount = 0;
        int faceCount = 0;
        bool isBinary = false;
        var properties = new List<string>();

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                continue;

            if (parts[0] == "format")
            {
                if (parts.Length > 1 && parts[1] != "ascii")
                    isBinary = true;
            }
            else if (parts[0] == "element")
            {
                if (parts.Length >= 3)
                {
                    if (parts[1] == "vertex")
                        vertexCount = int.Parse(parts[2]);
                    else if (parts[1] == "face")
                        faceCount = int.Parse(parts[2]);
                }
            }
            else if (parts[0] == "property" && parts.Length >= 3)
            {
                properties.Add(parts[2]);
            }
            else if (parts[0] == "end_header")
            {
                break;
            }
        }

        if (isBinary)
        {
            throw new NotSupportedException("Binary PLY format is not supported. Please use ASCII PLY format.");
        }

        for (int i = 0; i < vertexCount; i++)
        {
            line = reader.ReadLine();
            if (line == null) break;

            var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
                float y = float.Parse(parts[1], CultureInfo.InvariantCulture);
                float z = float.Parse(parts[2], CultureInfo.InvariantCulture);
                mesh.Vertices.Add(new Vector3D(x, y, z));
            }
        }

        for (int i = 0; i < faceCount; i++)
        {
            line = reader.ReadLine();
            if (line == null) break;

            var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) continue;

            int count = int.Parse(parts[0]);
            if (count < 3 || parts.Length < count + 1) continue;

            var indices = new List<int>();
            for (int j = 1; j <= count; j++)
            {
                indices.Add(int.Parse(parts[j]));
            }

            for (int j = 1; j < indices.Count - 1; j++)
            {
                mesh.Faces.Add((indices[0], indices[j], indices[j + 1]));
            }
        }

        return mesh;
    }
}
