using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstancedIndirectGrassPosDefine : MonoBehaviour
{
    private enum QuantityType
    {
        InstanceCount,
        Density,
    }

    [SerializeField]
    private QuantityType _type;

    [Range(1, 40_000_000)]
    [SerializeField]
    private int _instanceCount = 1_000_000;

    [SerializeField]
    private float _density;

    private int _cacheCount = -1;

    private void Start()
    {
        UpdatePosIfNeeded();
    }

    private void Update()
    {
        UpdatePosIfNeeded();
    }

    private void OnDrawGizmos()
    {
        var size = transform.lossyScale * 2;
        size.y = 0;
        Gizmos.DrawWireCube(transform.position, size);
    }

    private int GetCount()
    {
        if (_type == QuantityType.InstanceCount)
        {
            return _instanceCount;
        }

        if (_type == QuantityType.Density)
        {
            var scale = transform.lossyScale;
            return Mathf.FloorToInt(scale.x * scale.z * _density);
        }

        return 0;
    }

    private void UpdatePosIfNeeded()
    {
        var count = GetCount();

        if (count == _cacheCount)
            return;

        Debug.Log("UpdatePos (Slow)");

        //same seed to keep grass visual the same
        UnityEngine.Random.InitState(123);

        //auto keep density the same
        //float scale = Mathf.Sqrt((instanceCount / 4)) / 2f;
        //transform.localScale = new Vector3(scale, transform.localScale.y, scale);

        //////////////////////////////////////////////////////////////////////////
        //can define any posWS in this section, random is just an example
        //////////////////////////////////////////////////////////////////////////
        List<Vector3> positions = new List<Vector3>(count);
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = Vector3.zero;

            pos.x = UnityEngine.Random.Range(-1f, 1f) * transform.lossyScale.x;
            pos.z = UnityEngine.Random.Range(-1f, 1f) * transform.lossyScale.z;

            //transform to posWS in C#
            pos += transform.position;

            positions.Add(new Vector3(pos.x, pos.y, pos.z));
        }

        //send all posWS to renderer
        InstancedIndirectGrassRenderer.instance.SetGrassPositions(positions);
        _cacheCount = positions.Count;
    }
}
