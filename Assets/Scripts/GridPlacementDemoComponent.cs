using UnityEngine;

public class GridPlacementDemoComponent : MonoBehaviour
{
    [SerializeField]
    private GridPlacement3D _gridPlacement = new();

    [SerializeField]
    private Transform _point;

    private Grid3D _grid = new();

    private void OnDrawGizmos()
    {
        var origin = transform.position;
        _gridPlacement.Origin = origin;

        var size = _gridPlacement.ChunkSize;

        _grid.GridSize = _gridPlacement.GridSize;

        var length = _gridPlacement.TotalCount();

        for (int i = 0; i < length; i++)
        {
            var gridPos = _grid.IndexToGrid(i);
            var worldPos = _gridPlacement.GetPosition(gridPos);
            var center = worldPos;
            Gizmos.DrawWireCube(center, size);
        }

        var bounds = _gridPlacement.GetBounds();

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(bounds.center, bounds.size + Vector3.one * 1e-1f);

        if (_point != null)
        {
            float radius = 0.5f;
            var worldPos = _point.position;

            if (_gridPlacement.WorldToGrid(worldPos, out var gridPos))
            {
                var worldPosSnap = _gridPlacement.GetPosition(gridPos);
                var center = worldPosSnap;

                Gizmos.color = Color.green;
                Gizmos.DrawSphere(center, radius);
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(worldPos, radius);
        }
    }
}
