using System.Globalization;
using System.IO;
using System.Text;

namespace Avalonia3DCanvas;

public static class ModelOBJWriter
{
    public static void Write(string filePath, Mesh3D mesh)
    {
        if (mesh == null)
            throw new ArgumentNullException(nameof(mesh));

        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

        // Write header comment
        writer.WriteLine("# Wavefront OBJ file");
        writer.WriteLine("# Exported from Avalonia 3D Canvas");
        writer.WriteLine($"# Vertices: {mesh.Vertices.Count}");
        writer.WriteLine($"# Faces: {mesh.Faces.Count}");
        writer.WriteLine();

        // Write vertices
        foreach (var vertex in mesh.Vertices)
        {
            // Use InvariantCulture to ensure consistent decimal separator
            writer.WriteLine($"v {vertex.X.ToString("F6", CultureInfo.InvariantCulture)} {vertex.Y.ToString("F6", CultureInfo.InvariantCulture)} {vertex.Z.ToString("F6", CultureInfo.InvariantCulture)}");
        }

        writer.WriteLine();

        // Write faces (OBJ uses 1-based indexing)
        foreach (var face in mesh.Faces)
        {
            writer.WriteLine($"f {face.Item1 + 1} {face.Item2 + 1} {face.Item3 + 1}");
        }
    }

    public static void WriteWithName(string filePath, Mesh3D mesh, string objectName)
    {
        if (mesh == null)
            throw new ArgumentNullException(nameof(mesh));

        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

        // Write header comment
        writer.WriteLine("# Wavefront OBJ file");
        writer.WriteLine("# Exported from Avalonia 3D Canvas");
        writer.WriteLine($"# Vertices: {mesh.Vertices.Count}");
        writer.WriteLine($"# Faces: {mesh.Faces.Count}");
        writer.WriteLine();

        // Write object name
        if (!string.IsNullOrEmpty(objectName))
        {
            writer.WriteLine($"o {objectName}");
            writer.WriteLine();
        }

        // Write vertices
        foreach (var vertex in mesh.Vertices)
        {
            writer.WriteLine($"v {vertex.X.ToString("F6", CultureInfo.InvariantCulture)} {vertex.Y.ToString("F6", CultureInfo.InvariantCulture)} {vertex.Z.ToString("F6", CultureInfo.InvariantCulture)}");
        }

        writer.WriteLine();

        // Write faces (OBJ uses 1-based indexing)
        foreach (var face in mesh.Faces)
        {
            writer.WriteLine($"f {face.Item1 + 1} {face.Item2 + 1} {face.Item3 + 1}");
        }
    }
}
