using UnityEngine;

public class GridPlacementDemoComponent : MonoBehaviour
{
    [SerializeField]
    private GridPlacement3D _gridPlacement = new();

    private Grid3D _grid = new();

    private void OnDrawGizmos()
    {
        var origin = transform.position;

        var size = _gridPlacement.ChunkSize;

        _grid.GridSize = _gridPlacement.GridSize;

        var length = _gridPlacement.TotalCount();

        for (int i = 0; i < length; i++)
        {
            var gridPos = _grid.IndexToGrid(i);
            var worldPos = _gridPlacement.GetPosition(gridPos);
            var center = origin + worldPos;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
