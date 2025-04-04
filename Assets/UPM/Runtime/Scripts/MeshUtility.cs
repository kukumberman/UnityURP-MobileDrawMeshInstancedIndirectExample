using UnityEngine;
using UnityEngine.Rendering;

public static class MeshUtility
{
    public static int GetMeshByteSize(Mesh mesh)
    {
        var totalBytesPerVertex = 0;

        foreach (var item in mesh.GetVertexAttributes())
        {
            totalBytesPerVertex += item.dimension * GetAttributeByteSize(item.format);
        }

        var totalBytesPerMesh = totalBytesPerVertex * mesh.vertexCount;

        return totalBytesPerMesh;
    }

    public static int GetAttributeByteSize(VertexAttributeFormat format)
    {
        switch (format)
        {
            case VertexAttributeFormat.Float32:
            case VertexAttributeFormat.SInt32:
            case VertexAttributeFormat.UInt32:
                return 4;
            case VertexAttributeFormat.Float16:
            case VertexAttributeFormat.SInt16:
            case VertexAttributeFormat.SNorm16:
            case VertexAttributeFormat.UInt16:
            case VertexAttributeFormat.UNorm16:
                return 2;
            case VertexAttributeFormat.SInt8:
            case VertexAttributeFormat.SNorm8:
            case VertexAttributeFormat.UInt8:
            case VertexAttributeFormat.UNorm8:
                return 1;
        }

        return 0;
    }
}
