using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PlaneMeshGenerator
{
    public Func<Vector3, Vector3> VertexPositionTransformer;

    public float SquareSize = 1;
    public Vector2 Pivot = new Vector2(0.5f, 0.5f);

    // "Value in range [1..255]. Unity can not handle greater than 65536 vertices per mesh"
    public int Resolution = 1;

    private Mesh _mesh = null;

    public Mesh Generate()
    {
        if (_mesh == null)
        {
            _mesh = new Mesh();
        }

        var resolution = Resolution;
        var stepDistance = SquareSize / resolution;

        var offset2d = Vector2.Scale(new Vector2(SquareSize, SquareSize), Pivot);

        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();

        for (int y = 0, height = resolution + 1; y < height; y++)
        {
            for (int x = 0, width = resolution + 1; x < width; x++)
            {
                var vertex = new Vector3(x * stepDistance, 0, y * stepDistance);
                vertex.x -= offset2d.x;
                vertex.z -= offset2d.y;

                if (VertexPositionTransformer != null)
                {
                    vertex = VertexPositionTransformer.Invoke(vertex);
                }

                vertices.Add(vertex);

                var xy = new Vector2(x, y);
                var uv = new Vector2(xy.x / resolution, xy.y / resolution);
                uvs.Add(uv);
            }
        }

        var triangles = new List<int>();

        // Thanks to ChatGPT
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int startIndex = y * (resolution + 1) + x;

                triangles.Add(startIndex);
                triangles.Add(startIndex + resolution + 1);
                triangles.Add(startIndex + 1);

                triangles.Add(startIndex + resolution + 1);
                triangles.Add(startIndex + resolution + 2);
                triangles.Add(startIndex + 1);
            }
        }

        _mesh.vertices = vertices.ToArray();
        _mesh.uv = uvs.ToArray();
        _mesh.triangles = triangles.ToArray();

        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();
        _mesh.RecalculateTangents();

        return _mesh;
    }
}
