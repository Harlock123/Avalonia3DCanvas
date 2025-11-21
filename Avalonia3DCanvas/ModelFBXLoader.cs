using System.Globalization;
using System.IO;
using System.Text;

namespace Avalonia3DCanvas;

public static class ModelFBXLoader
{
    public static Mesh3D Load(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = new BinaryReader(stream);

        if (stream.Length < 18)
        {
            return LoadASCII(filePath);
        }

        var header = Encoding.ASCII.GetString(reader.ReadBytes(18));
        stream.Seek(0, SeekOrigin.Begin);

        if (header.Contains("Kaydara FBX Binary"))
        {
            throw new NotSupportedException("Binary FBX format is not supported. Please use ASCII FBX format or convert to OBJ/STL.");
        }

        return LoadASCII(filePath);
    }

    private static Mesh3D LoadASCII(string filePath)
    {
        var mesh = new Mesh3D();
        var content = File.ReadAllText(filePath);

        ExtractAllGeometry(content, mesh);

        if (mesh.Vertices.Count == 0)
        {
            throw new InvalidDataException("No geometry data found in FBX file. The file may be empty, binary format, or have an unsupported structure. Try converting to OBJ format for better compatibility.");
        }

        return mesh;
    }

    private static void ExtractAllGeometry(string content, Mesh3D mesh)
    {
        var lines = content.Split('\n');
        int lineIndex = 0;

        while (lineIndex < lines.Length)
        {
            var trimmed = lines[lineIndex].Trim();

            if (trimmed.StartsWith("Geometry:") || trimmed.Contains("Type: \"Mesh\""))
            {
                lineIndex = ProcessGeometryBlock(lines, lineIndex, mesh);
            }
            else
            {
                lineIndex++;
            }
        }
    }

    private static int ProcessGeometryBlock(string[] lines, int startIndex, Mesh3D mesh)
    {
        int vertexOffset = mesh.Vertices.Count;
        var vertices = new List<Vector3D>();
        var polygonIndices = new List<int>();

        int i = startIndex;
        int braceDepth = 0;
        bool inBlock = false;

        while (i < lines.Length)
        {
            var trimmed = lines[i].Trim();

            if (trimmed.Contains("{"))
            {
                braceDepth++;
                inBlock = true;
            }

            if (trimmed.StartsWith("Vertices:"))
            {
                i = ExtractVerticesFromBlock(lines, i + 1, vertices);
                continue;
            }

            if (trimmed.StartsWith("PolygonVertexIndex:"))
            {
                i = ExtractPolygonIndicesFromBlock(lines, i + 1, polygonIndices);
                continue;
            }

            if (trimmed.Contains("}"))
            {
                braceDepth--;
                if (inBlock && braceDepth == 0)
                {
                    break;
                }
            }

            i++;
        }

        mesh.Vertices.AddRange(vertices);

        var currentPoly = new List<int>();
        foreach (var index in polygonIndices)
        {
            if (index < 0)
            {
                int actualIndex = (-index) - 1 + vertexOffset;
                currentPoly.Add(actualIndex);

                if (currentPoly.Count >= 3)
                {
                    for (int j = 1; j < currentPoly.Count - 1; j++)
                    {
                        if (currentPoly[0] < mesh.Vertices.Count &&
                            currentPoly[j] < mesh.Vertices.Count &&
                            currentPoly[j + 1] < mesh.Vertices.Count)
                        {
                            mesh.Faces.Add((currentPoly[0], currentPoly[j], currentPoly[j + 1]));
                        }
                    }
                }
                currentPoly.Clear();
            }
            else
            {
                currentPoly.Add(index + vertexOffset);
            }
        }

        return i + 1;
    }

    private static int ExtractVerticesFromBlock(string[] lines, int startIndex, List<Vector3D> vertices)
    {
        var vertexData = new List<float>();
        int i = startIndex;

        while (i < lines.Length)
        {
            var trimmed = lines[i].Trim();

            if (trimmed.StartsWith("}") || (!trimmed.StartsWith("a:") && !trimmed.StartsWith("*")))
            {
                break;
            }

            if (trimmed.StartsWith("a:") || trimmed.StartsWith("*"))
            {
                var dataStr = trimmed;
                if (dataStr.StartsWith("a:"))
                {
                    dataStr = dataStr.Substring(2).Trim();
                }
                else if (dataStr.StartsWith("*"))
                {
                    var colonIndex = dataStr.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        dataStr = dataStr.Substring(colonIndex + 1).Trim();
                    }
                }

                var parts = dataStr.Split(',');
                foreach (var part in parts)
                {
                    var cleaned = part.Trim();
                    if (!string.IsNullOrEmpty(cleaned) && float.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                    {
                        vertexData.Add(value);
                    }
                }
            }

            i++;
        }

        for (int j = 0; j + 2 < vertexData.Count; j += 3)
        {
            vertices.Add(new Vector3D(vertexData[j], vertexData[j + 1], vertexData[j + 2]));
        }

        return i;
    }

    private static int ExtractPolygonIndicesFromBlock(string[] lines, int startIndex, List<int> indices)
    {
        int i = startIndex;

        while (i < lines.Length)
        {
            var trimmed = lines[i].Trim();

            if (trimmed.StartsWith("}") || (!trimmed.StartsWith("a:") && !trimmed.StartsWith("*")))
            {
                break;
            }

            if (trimmed.StartsWith("a:") || trimmed.StartsWith("*"))
            {
                var dataStr = trimmed;
                if (dataStr.StartsWith("a:"))
                {
                    dataStr = dataStr.Substring(2).Trim();
                }
                else if (dataStr.StartsWith("*"))
                {
                    var colonIndex = dataStr.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        dataStr = dataStr.Substring(colonIndex + 1).Trim();
                    }
                }

                var parts = dataStr.Split(',');
                foreach (var part in parts)
                {
                    var cleaned = part.Trim();
                    if (!string.IsNullOrEmpty(cleaned) && int.TryParse(cleaned, out int value))
                    {
                        indices.Add(value);
                    }
                }
            }

            i++;
        }

        return i;
    }
}
