using UnityEngine;

public interface ICoordinateConverter
{
    Vector3 GridToWorld(Vector3Int gridPosition);

    Vector3Int WorldToGrid(Vector3 pos);
}
