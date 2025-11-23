using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Avalonia3DCanvas;

public static class ModelLWOLoader
{
    public static Mesh3D Load(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = new BinaryReader(stream);

        // Read FORM header
        var formId = ReadString(reader, 4);
        if (formId != "FORM")
            throw new InvalidDataException("Not a valid LWO file: missing FORM header");

        var fileSize = ReadBigEndianUInt32(reader);

        // Read LWO2 identifier
        var lwoId = ReadString(reader, 4);
        if (lwoId != "LWO2")
            throw new InvalidDataException("Only LWO2 format is supported");

        var mesh = new Mesh3D();
        var vertices = new List<Vector3D>();
        var tags = new List<string>();

        // Read chunks
        while (stream.Position < stream.Length)
        {
            var chunkId = ReadString(reader, 4);
            var chunkSize = ReadBigEndianUInt32(reader);
            var chunkEnd = stream.Position + chunkSize;

            switch (chunkId)
            {
                case "PNTS": // Points (vertices)
                    ReadPoints(reader, (int)chunkSize, vertices);
                    break;

                case "POLS": // Polygons
                    ReadPolygons(reader, (int)chunkSize, vertices, mesh);
                    break;

                case "TAGS": // Surface tags
                    ReadTags(reader, (int)chunkSize, tags);
                    break;

                case "PTAG": // Polygon tags (assigns surfaces to polygons)
                case "SURF": // Surface definition
                case "CLIP": // Image clip
                case "ENVL": // Envelope
                    // Skip these for now - they're for materials/textures
                    stream.Position = chunkEnd;
                    break;

                default:
                    // Unknown chunk, skip it
                    stream.Position = chunkEnd;
                    break;
            }

            // IFF chunks are word-aligned (2 bytes)
            if (chunkSize % 2 != 0)
                reader.ReadByte();
        }

        return mesh;
    }

    private static void ReadPoints(BinaryReader reader, int chunkSize, List<Vector3D> vertices)
    {
        int pointCount = chunkSize / 12; // Each point is 3 floats (12 bytes)

        for (int i = 0; i < pointCount; i++)
        {
            float x = ReadBigEndianFloat(reader);
            float y = ReadBigEndianFloat(reader);
            float z = ReadBigEndianFloat(reader);

            vertices.Add(new Vector3D(x, y, z));
        }
    }

    private static void ReadPolygons(BinaryReader reader, int chunkSize, List<Vector3D> vertices, Mesh3D mesh)
    {
        long chunkEnd = reader.BaseStream.Position + chunkSize;

        // Read polygon type (4 bytes)
        var polyType = ReadString(reader, 4);

        // We primarily care about FACE polygons
        if (polyType != "FACE")
        {
            // Skip unsupported polygon types
            reader.BaseStream.Position = chunkEnd;
            return;
        }

        while (reader.BaseStream.Position < chunkEnd)
        {
            // Read vertex count (variable index)
            ushort vertexCount = ReadVariableIndex(reader);

            if (vertexCount < 3)
                continue;

            var indices = new List<int>();

            for (int i = 0; i < vertexCount; i++)
            {
                int index = (int)ReadVariableIndex(reader);
                if (index < vertices.Count)
                {
                    // Store vertex in mesh if not already there
                    int meshIndex = mesh.Vertices.IndexOf(vertices[index]);
                    if (meshIndex == -1)
                    {
                        meshIndex = mesh.Vertices.Count;
                        mesh.Vertices.Add(vertices[index]);
                    }
                    indices.Add(meshIndex);
                }
            }

            // Triangulate polygon (fan triangulation)
            if (indices.Count >= 3)
            {
                for (int i = 1; i < indices.Count - 1; i++)
                {
                    mesh.Faces.Add((indices[0], indices[i], indices[i + 1]));
                }
            }
        }
    }

    private static void ReadTags(BinaryReader reader, int chunkSize, List<string> tags)
    {
        long chunkEnd = reader.BaseStream.Position + chunkSize;

        while (reader.BaseStream.Position < chunkEnd)
        {
            var tag = ReadNullTerminatedString(reader);
            tags.Add(tag);
        }
    }

    // LWO uses variable-length indices
    private static ushort ReadVariableIndex(BinaryReader reader)
    {
        ushort index = ReadBigEndianUInt16(reader);

        // If high bit is set, it's a 4-byte index
        if ((index & 0xFF00) == 0xFF00)
        {
            // Read next 2 bytes
            ushort lowBytes = ReadBigEndianUInt16(reader);
            return (ushort)(((index & 0x00FF) << 8) | (lowBytes & 0xFFFF));
        }

        return index;
    }

    private static string ReadNullTerminatedString(BinaryReader reader)
    {
        var bytes = new List<byte>();
        byte b;

        while ((b = reader.ReadByte()) != 0)
        {
            bytes.Add(b);
        }

        // Strings are word-aligned
        if (bytes.Count % 2 == 0)
            reader.ReadByte(); // Skip padding

        return Encoding.ASCII.GetString(bytes.ToArray());
    }

    private static string ReadString(BinaryReader reader, int length)
    {
        var bytes = reader.ReadBytes(length);
        return Encoding.ASCII.GetString(bytes);
    }

    private static uint ReadBigEndianUInt32(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    private static ushort ReadBigEndianUInt16(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(2);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt16(bytes, 0);
    }

    private static float ReadBigEndianFloat(BinaryReader reader)
    {
        var bytes = reader.ReadBytes(4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToSingle(bytes, 0);
    }
}
