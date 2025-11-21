using System.Globalization;
using System.IO;
using System.Text;

namespace Avalonia3DCanvas;

public static class ModelFBXWriter
{
    public static void WriteCube(string filePath, float size = 1.0f)
    {
        var mesh = CreateCubeMesh(size);
        Write(filePath, mesh);
    }

    private static Mesh3D CreateCubeMesh(float size)
    {
        var mesh = new Mesh3D();
        float half = size / 2;

        mesh.Vertices.AddRange(new[]
        {
            new Vector3D(-half, -half, -half),
            new Vector3D(half, -half, -half),
            new Vector3D(half, half, -half),
            new Vector3D(-half, half, -half),
            new Vector3D(-half, -half, half),
            new Vector3D(half, -half, half),
            new Vector3D(half, half, half),
            new Vector3D(-half, half, half)
        });

        mesh.Faces.AddRange(new[]
        {
            (0, 2, 1), (0, 3, 2),
            (4, 5, 6), (4, 6, 7),
            (0, 1, 5), (0, 5, 4),
            (1, 2, 6), (1, 6, 5),
            (2, 3, 7), (2, 7, 6),
            (3, 0, 4), (3, 4, 7)
        });

        return mesh;
    }

    public static void Write(string filePath, Mesh3D mesh)
    {
        var sb = new StringBuilder();

        sb.AppendLine("; FBX 7.4.0 project file");
        sb.AppendLine("; Created by Avalonia3DCanvas");
        sb.AppendLine();
        sb.AppendLine("FBXHeaderExtension:  {");
        sb.AppendLine("\tFBXHeaderVersion: 1003");
        sb.AppendLine("\tFBXVersion: 7400");
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("Definitions:  {");
        sb.AppendLine("\tVersion: 100");
        sb.AppendLine("\tCount: 1");
        sb.AppendLine("\tObjectType: \"Geometry\" {");
        sb.AppendLine("\t\tCount: 1");
        sb.AppendLine("\t}");
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("Objects:  {");
        sb.AppendLine("\tGeometry: 1000000, \"Geometry::\", \"Mesh\" {");

        sb.Append("\t\tVertices: *");
        sb.Append(mesh.Vertices.Count * 3);
        sb.AppendLine(" {");
        sb.Append("\t\t\ta: ");

        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            var v = mesh.Vertices[i];
            sb.Append(v.X.ToString("F6", CultureInfo.InvariantCulture));
            sb.Append(",");
            sb.Append(v.Y.ToString("F6", CultureInfo.InvariantCulture));
            sb.Append(",");
            sb.Append(v.Z.ToString("F6", CultureInfo.InvariantCulture));

            if (i < mesh.Vertices.Count - 1)
                sb.Append(",");
        }

        sb.AppendLine();
        sb.AppendLine("\t\t}");

        sb.Append("\t\tPolygonVertexIndex: *");
        sb.Append(mesh.Faces.Count * 3);
        sb.AppendLine(" {");
        sb.Append("\t\t\ta: ");

        for (int i = 0; i < mesh.Faces.Count; i++)
        {
            var face = mesh.Faces[i];
            sb.Append(face.Item1);
            sb.Append(",");
            sb.Append(face.Item2);
            sb.Append(",");
            sb.Append(-(face.Item3 + 1));

            if (i < mesh.Faces.Count - 1)
                sb.Append(",");
        }

        sb.AppendLine();
        sb.AppendLine("\t\t}");

        sb.AppendLine("\t\tGeometryVersion: 124");
        sb.AppendLine("\t}");
        sb.AppendLine("}");

        File.WriteAllText(filePath, sb.ToString());
    }
}
