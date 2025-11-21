using System.Globalization;
using System.IO;

namespace Avalonia3DCanvas;

public static class ModelOBJLoader
{
    public static Mesh3D Load(string filePath)
    {
        var mesh = new Mesh3D();
        var lines = File.ReadAllLines(filePath);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                continue;

            var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                continue;

            switch (parts[0])
            {
                case "v" when parts.Length >= 4:
                    {
                        float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                        float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                        mesh.Vertices.Add(new Vector3D(x, y, z));
                        break;
                    }

                case "f" when parts.Length >= 4:
                    {
                        var indices = new List<int>();
                        for (int i = 1; i < parts.Length; i++)
                        {
                            var indexPart = parts[i].Split('/')[0];
                            if (int.TryParse(indexPart, out int index))
                            {
                                int vertexIndex = index > 0 ? index - 1 : mesh.Vertices.Count + index;
                                indices.Add(vertexIndex);
                            }
                        }

                        if (indices.Count >= 3)
                        {
                            for (int i = 1; i < indices.Count - 1; i++)
                            {
                                mesh.Faces.Add((indices[0], indices[i], indices[i + 1]));
                            }
                        }
                        break;
                    }
            }
        }

        return mesh;
    }
}
