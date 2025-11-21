using System.IO;

namespace Avalonia3DCanvas;

public static class Model3DSLoader
{
    private const ushort MAIN3DS = 0x4D4D;
    private const ushort EDIT3DS = 0x3D3D;
    private const ushort EDIT_OBJECT = 0x4000;
    private const ushort OBJ_TRIMESH = 0x4100;
    private const ushort TRI_VERTEXL = 0x4110;
    private const ushort TRI_FACEL = 0x4120;

    public static Mesh3D Load(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = new BinaryReader(stream);

        var mesh = new Mesh3D();

        ReadChunk(reader, mesh);

        return mesh;
    }

    private static void ReadChunk(BinaryReader reader, Mesh3D mesh)
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            if (reader.BaseStream.Position + 6 > reader.BaseStream.Length)
                break;

            ushort chunkId = reader.ReadUInt16();
            uint chunkLength = reader.ReadUInt32();
            long nextChunkPos = reader.BaseStream.Position + chunkLength - 6;

            switch (chunkId)
            {
                case MAIN3DS:
                case EDIT3DS:
                case EDIT_OBJECT:
                    ReadChunk(reader, mesh);
                    break;

                case OBJ_TRIMESH:
                    ReadTriMesh(reader, mesh, nextChunkPos);
                    break;

                case TRI_VERTEXL:
                    ReadVertices(reader, mesh);
                    break;

                case TRI_FACEL:
                    ReadFaces(reader, mesh);
                    break;

                default:
                    if (nextChunkPos <= reader.BaseStream.Length)
                        reader.BaseStream.Seek(nextChunkPos, SeekOrigin.Begin);
                    break;
            }

            if (reader.BaseStream.Position >= nextChunkPos && nextChunkPos <= reader.BaseStream.Length)
            {
                reader.BaseStream.Seek(nextChunkPos, SeekOrigin.Begin);
            }
        }
    }

    private static void ReadTriMesh(BinaryReader reader, Mesh3D mesh, long endPos)
    {
        while (reader.BaseStream.Position < endPos && reader.BaseStream.Position < reader.BaseStream.Length)
        {
            if (reader.BaseStream.Position + 6 > reader.BaseStream.Length)
                break;

            ushort chunkId = reader.ReadUInt16();
            uint chunkLength = reader.ReadUInt32();
            long nextChunkPos = reader.BaseStream.Position + chunkLength - 6;

            switch (chunkId)
            {
                case TRI_VERTEXL:
                    ReadVertices(reader, mesh);
                    break;

                case TRI_FACEL:
                    ReadFaces(reader, mesh);
                    break;

                default:
                    if (nextChunkPos <= reader.BaseStream.Length)
                        reader.BaseStream.Seek(nextChunkPos, SeekOrigin.Begin);
                    break;
            }
        }
    }

    private static void ReadVertices(BinaryReader reader, Mesh3D mesh)
    {
        ushort vertexCount = reader.ReadUInt16();

        for (int i = 0; i < vertexCount; i++)
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();

            mesh.Vertices.Add(new Vector3D(x, y, z));
        }
    }

    private static void ReadFaces(BinaryReader reader, Mesh3D mesh)
    {
        ushort faceCount = reader.ReadUInt16();

        for (int i = 0; i < faceCount; i++)
        {
            ushort v1 = reader.ReadUInt16();
            ushort v2 = reader.ReadUInt16();
            ushort v3 = reader.ReadUInt16();
            reader.ReadUInt16(); // face flags

            mesh.Faces.Add((v1, v2, v3));
        }
    }
}
