// https://github.com/kukumberman/Unity-ShopAdventure/blob/main/Assets/Scripts/Shared/GridPlacement.cs

using System;
using UnityEngine;

[Serializable]
public sealed class GridPlacement3D
{
    public Vector3Int GridSize = Vector3Int.one;
    public Vector3 Pivot = new Vector3(0.5f, 0.5f, 0.5f);
    public Vector3 ChunkSize = Vector3.one * 3;
    public float Spacing = 0;

    public int TotalCount()
    {
        return GridSize.x * GridSize.y * GridSize.z;
    }

    public Vector3 GetPosition(Vector3Int grid)
    {
        return GetPosition(grid.x, grid.y, grid.z);
    }

    public Vector3 GetPosition(int x, int y, int z)
    {
        Vector3 pos = new Vector3(x, y, z);
        pos -= Vector3.Scale(GridSize, Pivot);
        pos += Vector3.one * 0.5f;

        var halfSpacing = Spacing * 0.5f;

        Vector3 spacingOffset = new Vector3(x, y, z) * Spacing;
        spacingOffset -= Vector3.Scale(GridSize, Pivot) * Spacing;
        spacingOffset += Vector3.one * halfSpacing;
        spacingOffset += new Vector3(
            Mathf.Lerp(-halfSpacing, halfSpacing, Pivot.x),
            Mathf.Lerp(-halfSpacing, halfSpacing, Pivot.y),
            Mathf.Lerp(-halfSpacing, halfSpacing, Pivot.z)
        );

        pos.Scale(ChunkSize);
        pos += spacingOffset;
        return pos;
    }

    public Vector3 GetPositionWithoutPivot(int x, int y, int z)
    {
        Vector3 xyz = new Vector3(x, y, z);
        Vector3 centerOffset = Vector3.one * 0.5f;
        Vector3 gridOffset = new Vector3(
            -GridSize.x * 0.5f,
            -GridSize.y * 0.5f,
            -GridSize.z * 0.5f
        );
        Vector3 pos = gridOffset + xyz + centerOffset;
        Vector3 position = Vector3.Scale(pos, ChunkSize) + pos * Spacing;
        return position;
    }

    public Vector3 GetTotalSize()
    {
        Vector3 size = Vector3.zero;
        size.x = GridSize.x * ChunkSize.x + (GridSize.x - 1) * Spacing;
        size.y = GridSize.y * ChunkSize.y + (GridSize.y - 1) * Spacing;
        size.z = GridSize.z * ChunkSize.z + (GridSize.z - 1) * Spacing;
        return size;
    }

    public Vector3 GetCenter()
    {
        var size = GetTotalSize();

        var halfSize = size * 0.5f;

        var center = Vector3.zero;

        center.x = Mathf.Lerp(-halfSize.x, halfSize.x, 1f - Pivot.x);
        center.y = Mathf.Lerp(-halfSize.y, halfSize.y, 1f - Pivot.y);
        center.y = Mathf.Lerp(-halfSize.z, halfSize.z, 1f - Pivot.z);

        return center;
    }
}
