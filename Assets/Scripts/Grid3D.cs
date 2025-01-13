using UnityEngine;

public class Grid3D
{
    public Vector3Int GridSize;

    public Vector3Int IndexToGrid(int index)
    {
        return new Vector3Int(
            index % GridSize.x,
            (index / GridSize.x) % GridSize.y,
            (index / (GridSize.x * GridSize.y))
        );
    }

    public int GridToIndex(Vector3Int grid)
    {
        return GridToIndex(grid.x, grid.y, grid.z);
    }

    public int GridToIndex(int x, int y, int z)
    {
        return z * (GridSize.x * GridSize.y) + y * GridSize.x + x;
    }
}
