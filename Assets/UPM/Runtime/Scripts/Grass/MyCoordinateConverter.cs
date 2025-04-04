using UnityEngine;

public class MyCoordinateConverter : ICoordinateConverter
{
    private Vector3 _min;
    private Vector3 _max;
    private Vector3Int _cellCount;

    public MyCoordinateConverter(Vector3 min, Vector3 max, Vector3Int cellCount)
    {
        _min = min;
        _max = max;
        _cellCount = cellCount;
    }

    public Vector3 GridToWorld(Vector3Int gridPosition)
    {
        Vector3 centerPosWS = new Vector3(
            gridPosition.x + 0.5f,
            gridPosition.y + 0.5f,
            gridPosition.z + 0.5f
        );
        centerPosWS.x = Mathf.Lerp(_min.x, _max.x, centerPosWS.x / _cellCount.x);
        centerPosWS.y = Mathf.Lerp(_min.y, _max.y, centerPosWS.y / _cellCount.y);
        centerPosWS.z = Mathf.Lerp(_min.z, _max.z, centerPosWS.z / _cellCount.z);

        return centerPosWS;
    }

    public Vector3Int WorldToGrid(Vector3 pos)
    {
        //find cellID
        int xID = Mathf.Min(
            _cellCount.x - 1,
            Mathf.FloorToInt(Mathf.InverseLerp(_min.x, _max.x, pos.x) * _cellCount.x)
        ); //use min to force within 0~[cellCountX-1]
        int yID = Mathf.Min(
            _cellCount.y - 1,
            Mathf.FloorToInt(Mathf.InverseLerp(_min.y, _max.y, pos.y) * _cellCount.y)
        );
        int zID = Mathf.Min(
            _cellCount.z - 1,
            Mathf.FloorToInt(Mathf.InverseLerp(_min.z, _max.z, pos.z) * _cellCount.z)
        ); //use min to force within 0~[cellCountZ-1]

        return new Vector3Int(xID, yID, zID);
    }
}
