using System;
using System.Collections.Generic;
using UnityEngine;

public class InstancedIndirectGrassPosDefine : MonoBehaviour, IGrassContainer
{
    public Func<Vector3, Vector3> VertexPositionTransformer;

    private enum QuantityType
    {
        InstanceCount,
        Density,
    }

    [SerializeField]
    private GrassScriptableObject _grass;

    [SerializeField]
    private QuantityType _type;

    [SerializeField]
    private int _instanceCount = 1_000_000;

    [SerializeField]
    private float _density;

    [SerializeField]
    private int _seed;

    [SerializeField]
    private float _heightOffset;

    private int _cacheCount = -1;

    private bool _requiresUpdate;
    private List<Vector3> _positions;

    string IGrassContainer.Id => _grass.Id;

    IReadOnlyList<Vector3> IGrassContainer.PositionsRef => _positions;

    bool IGrassContainer.RequiresUpdate => _requiresUpdate;

    private void OnEnable()
    {
        UpdatePosIfNeeded();

        GrassManager.Add(this);
    }

    private void OnDisable()
    {
        GrassManager.Remove(this);
    }

    private void Update()
    {
        UpdatePosIfNeeded();
    }

    private void OnDrawGizmos()
    {
        var size = transform.lossyScale;
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

        _requiresUpdate = count != _cacheCount;

        if (!_requiresUpdate)
            return;

        //same seed to keep grass visual the same
        var random = new System.Random(_seed);

        //auto keep density the same
        //float scale = Mathf.Sqrt((instanceCount / 4)) / 2f;
        //transform.localScale = new Vector3(scale, transform.localScale.y, scale);
        var currentScale = transform.lossyScale;
        var halfScale = currentScale * 0.5f;
        var origin = transform.position;

        //////////////////////////////////////////////////////////////////////////
        //can define any posWS in this section, random is just an example
        //////////////////////////////////////////////////////////////////////////
        _positions = new List<Vector3>(count);
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = Vector3.zero;

            pos.x = Mathf.Lerp(-1f, 1f, (float)random.NextDouble()) * halfScale.x;
            pos.z = Mathf.Lerp(-1f, 1f, (float)random.NextDouble()) * halfScale.z;

            //transform to posWS in C#
            pos += origin;

            _positions.Add(new Vector3(pos.x, pos.y, pos.z));
        }

        if (VertexPositionTransformer != null)
        {
            for (int i = 0; i < count; i++)
            {
                _positions[i] = VertexPositionTransformer.Invoke(_positions[i]);
            }
        }

        for (int i = 0; i < count; i++)
        {
            var pos = _positions[i];
            pos.y += _heightOffset;
            _positions[i] = pos;
        }

        _cacheCount = _positions.Count;
    }
}
