using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Avalonia3DCanvas;

public static class ModelLWOWriter
{
    public static void Write(string filePath, Mesh3D mesh, string surfaceName = "Default")
    {
        if (mesh == null)
            throw new ArgumentNullException(nameof(mesh));

        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Write chunks to memory first to calculate total size
        WriteTagsChunk(writer, new[] { surfaceName });
        WritePointsChunk(writer, mesh);
        WritePolygonsChunk(writer, mesh);
        WritePTagChunk(writer, mesh, 0); // Assign all polygons to surface 0
        WriteSurfaceChunk(writer, surfaceName);

        // Now write the complete file with header
        using var fileStream = File.Create(filePath);
        using var fileWriter = new BinaryWriter(fileStream);

        // Write FORM header
        WriteString(fileWriter, "FORM");
        WriteBigEndianUInt32(fileWriter, (uint)(stream.Length + 4)); // +4 for LWO2 identifier
        WriteString(fileWriter, "LWO2");

        // Write all chunks
        stream.Position = 0;
        stream.CopyTo(fileStream);
    }

    private static void WriteTagsChunk(BinaryWriter writer, string[] tags)
    {
        using var chunkStream = new MemoryStream();
        using var chunkWriter = new BinaryWriter(chunkStream);

        foreach (var tag in tags)
        {
            WriteNullTerminatedString(chunkWriter, tag);
        }

        WriteChunk(writer, "TAGS", chunkStream.ToArray());
    }

    private static void WritePointsChunk(BinaryWriter writer, Mesh3D mesh)
    {
        using var chunkStream = new MemoryStream();
        using var chunkWriter = new BinaryWriter(chunkStream);

        foreach (var vertex in mesh.Vertices)
        {
            WriteBigEndianFloat(chunkWriter, vertex.X);
            WriteBigEndianFloat(chunkWriter, vertex.Y);
            WriteBigEndianFloat(chunkWriter, vertex.Z);
        }

        WriteChunk(writer, "PNTS", chunkStream.ToArray());
    }

    private static void WritePolygonsChunk(BinaryWriter writer, Mesh3D mesh)
    {
        using var chunkStream = new MemoryStream();
        using var chunkWriter = new BinaryWriter(chunkStream);

        // Write polygon type
        WriteString(chunkWriter, "FACE");

        // Write each face as a polygon
        foreach (var face in mesh.Faces)
        {
            // Write vertex count (3 for triangles)
            WriteVariableIndex(chunkWriter, 3);

            // Write vertex indices
            WriteVariableIndex(chunkWriter, (ushort)face.Item1);
            WriteVariableIndex(chunkWriter, (ushort)face.Item2);
            WriteVariableIndex(chunkWriter, (ushort)face.Item3);
        }

        WriteChunk(writer, "POLS", chunkStream.ToArray());
    }

    private static void WritePTagChunk(BinaryWriter writer, Mesh3D mesh, ushort surfaceIndex)
    {
        using var chunkStream = new MemoryStream();
        using var chunkWriter = new BinaryWriter(chunkStream);

        // Write tag type (SURF for surface assignment)
        WriteString(chunkWriter, "SURF");

        // Assign all polygons to the same surface
        for (int i = 0; i < mesh.Faces.Count; i++)
        {
            WriteVariableIndex(chunkWriter, (ushort)i);
            WriteBigEndianUInt16(chunkWriter, surfaceIndex);
        }

        WriteChunk(writer, "PTAG", chunkStream.ToArray());
    }

    private static void WriteSurfaceChunk(BinaryWriter writer, string surfaceName)
    {
        using var chunkStream = new MemoryStream();
        using var chunkWriter = new BinaryWriter(chunkStream);

        // Write surface name
        WriteNullTerminatedString(chunkWriter, surfaceName);

        // Write source (empty for now)
        WriteNullTerminatedString(chunkWriter, "");

        // Write basic surface attributes (optional - you can add color, etc.)
        // For simplicity, we'll write a basic white color
        WriteSubChunk(chunkWriter, "COLR", w =>
        {
            WriteBigEndianFloat(w, 0.8f); // R
            WriteBigEndianFloat(w, 0.8f); // G
            WriteBigEndianFloat(w, 0.8f); // B
            WriteVariableIndex(w, 0); // Envelope (none)
        });

        WriteChunk(writer, "SURF", chunkStream.ToArray());
    }

    private static void WriteSubChunk(BinaryWriter writer, string id, Action<BinaryWriter> writeContent)
    {
        using var subStream = new MemoryStream();
        using var subWriter = new BinaryWriter(subStream);

        writeContent(subWriter);

        WriteString(writer, id);
        WriteBigEndianUInt16(writer, (ushort)subStream.Length);
        writer.Write(subStream.ToArray());
    }

    private static void WriteChunk(BinaryWriter writer, string id, byte[] data)
    {
        WriteString(writer, id);
        WriteBigEndianUInt32(writer, (uint)data.Length);
        writer.Write(data);

        // Word-align (2 bytes)
        if (data.Length % 2 != 0)
            writer.Write((byte)0);
    }

    private static void WriteVariableIndex(BinaryWriter writer, ushort index)
    {
        // For indices < 65280, use 2 bytes
        if (index < 0xFF00)
        {
            WriteBigEndianUInt16(writer, index);
        }
        else
        {
            // For larger indices, use 4 bytes with 0xFF00 prefix
            WriteBigEndianUInt16(writer, (ushort)(0xFF00 | (index >> 16)));
            WriteBigEndianUInt16(writer, (ushort)(index & 0xFFFF));
        }
    }

    private static void WriteNullTerminatedString(BinaryWriter writer, string str)
    {
        var bytes = Encoding.ASCII.GetBytes(str);
        writer.Write(bytes);
        writer.Write((byte)0); // Null terminator

        // Word-align
        if (bytes.Length % 2 == 0)
            writer.Write((byte)0);
    }

    private static void WriteString(BinaryWriter writer, string str)
    {
        var bytes = Encoding.ASCII.GetBytes(str);
        writer.Write(bytes);
    }

    private static void WriteBigEndianUInt32(BinaryWriter writer, uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        writer.Write(bytes);
    }

    private static void WriteBigEndianUInt16(BinaryWriter writer, ushort value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        writer.Write(bytes);
    }

    private static void WriteBigEndianFloat(BinaryWriter writer, float value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        writer.Write(bytes);
    }
}
