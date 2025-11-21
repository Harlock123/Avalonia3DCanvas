using System.Globalization;
using System.IO;
using System.Text;

namespace Avalonia3DCanvas;

public static class ModelSTLLoader
{
    public static Mesh3D Load(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = new BinaryReader(stream);

        var header = Encoding.ASCII.GetString(reader.ReadBytes(5));
        stream.Seek(0, SeekOrigin.Begin);

        if (header.StartsWith("solid", StringComparison.OrdinalIgnoreCase))
        {
            var content = File.ReadAllText(filePath);
            if (content.Contains("vertex") && content.Contains("facet"))
            {
                return LoadASCII(filePath);
            }
        }

        stream.Seek(0, SeekOrigin.Begin);
        return LoadBinary(stream);
    }

    private static Mesh3D LoadASCII(string filePath)
    {
        var mesh = new Mesh3D();
        var lines = File.ReadAllLines(filePath);
        var currentFaceVertices = new List<Vector3D>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                continue;

            if (parts[0] == "vertex" && parts.Length >= 4)
            {
                float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                currentFaceVertices.Add(new Vector3D(x, y, z));
            }
            else if (parts[0] == "endfacet")
            {
                if (currentFaceVertices.Count == 3)
                {
                    int i0 = mesh.Vertices.Count;
                    mesh.Vertices.AddRange(currentFaceVertices);
                    mesh.Faces.Add((i0, i0 + 1, i0 + 2));
                }
                currentFaceVertices.Clear();
            }
        }

        return mesh;
    }

    private static Mesh3D LoadBinary(Stream stream)
    {
        var mesh = new Mesh3D();
        using var reader = new BinaryReader(stream);

        reader.ReadBytes(80);
        uint triangleCount = reader.ReadUInt32();

        for (int i = 0; i < triangleCount; i++)
        {
            reader.ReadSingle();
            reader.ReadSingle();
            reader.ReadSingle();

            int i0 = mesh.Vertices.Count;

            for (int j = 0; j < 3; j++)
            {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                mesh.Vertices.Add(new Vector3D(x, y, z));
            }

            mesh.Faces.Add((i0, i0 + 1, i0 + 2));

            reader.ReadUInt16();
        }

        return mesh;
    }
}
