using System.IO;

namespace Avalonia3DCanvas;

public static class Model3DSWriter
{
    private const ushort MAIN3DS = 0x4D4D;
    private const ushort EDIT3DS = 0x3D3D;
    private const ushort EDIT_OBJECT = 0x4000;
    private const ushort OBJ_TRIMESH = 0x4100;
    private const ushort TRI_VERTEXL = 0x4110;
    private const ushort TRI_FACEL = 0x4120;

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
        using var stream = File.Create(filePath);
        using var writer = new BinaryWriter(stream);

        long mainChunkStart = stream.Position;
        writer.Write(MAIN3DS);
        writer.Write((uint)0);

        long editChunkStart = stream.Position;
        writer.Write(EDIT3DS);
        writer.Write((uint)0);

        long objectChunkStart = stream.Position;
        writer.Write(EDIT_OBJECT);
        writer.Write((uint)0);

        WriteString(writer, "Object");

        long meshChunkStart = stream.Position;
        writer.Write(OBJ_TRIMESH);
        writer.Write((uint)0);

        WriteVertices(writer, mesh);
        WriteFaces(writer, mesh);

        UpdateChunkLength(stream, meshChunkStart);
        UpdateChunkLength(stream, objectChunkStart);
        UpdateChunkLength(stream, editChunkStart);
        UpdateChunkLength(stream, mainChunkStart);
    }

    private static void WriteString(BinaryWriter writer, string text)
    {
        foreach (char c in text)
        {
            writer.Write((byte)c);
        }
        writer.Write((byte)0);
    }

    private static void WriteVertices(BinaryWriter writer, Mesh3D mesh)
    {
        long chunkStart = writer.BaseStream.Position;
        writer.Write(TRI_VERTEXL);
        writer.Write((uint)0);

        writer.Write((ushort)mesh.Vertices.Count);

        foreach (var vertex in mesh.Vertices)
        {
            writer.Write(vertex.X);
            writer.Write(vertex.Y);
            writer.Write(vertex.Z);
        }

        UpdateChunkLength(writer.BaseStream, chunkStart);
    }

    private static void WriteFaces(BinaryWriter writer, Mesh3D mesh)
    {
        long chunkStart = writer.BaseStream.Position;
        writer.Write(TRI_FACEL);
        writer.Write((uint)0);

        writer.Write((ushort)mesh.Faces.Count);

        foreach (var face in mesh.Faces)
        {
            writer.Write((ushort)face.Item1);
            writer.Write((ushort)face.Item2);
            writer.Write((ushort)face.Item3);
            writer.Write((ushort)0);
        }

        UpdateChunkLength(writer.BaseStream, chunkStart);
    }

    private static void UpdateChunkLength(Stream stream, long chunkStart)
    {
        long currentPos = stream.Position;
        uint length = (uint)(currentPos - chunkStart);

        stream.Seek(chunkStart + 2, SeekOrigin.Begin);
        using var writer = new BinaryWriter(stream, System.Text.Encoding.Default, true);
        writer.Write(length);
        stream.Seek(currentPos, SeekOrigin.Begin);
    }
}
