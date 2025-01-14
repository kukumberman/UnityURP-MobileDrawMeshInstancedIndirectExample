using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GrassManager : MonoBehaviour
{
    public static GrassManager instance = null;

    [SerializeField]
    private InstancedIndirectGrassRenderer _renderer;

    private List<IGrassContainer> _grassContainers = new();

    private bool _requireUpdate = false;

    private void OnEnable()
    {
        instance = this;
    }

    private void Update()
    {
        UpdateIfNeeded();
    }

    public static void Add(IGrassContainer container)
    {
        if (instance != null)
        {
            instance.AddContainer(container);
        }
    }

    public static void Remove(IGrassContainer container)
    {
        if (instance != null)
        {
            instance.RemoveContainer(container);
        }
    }

    public void AddContainer(IGrassContainer container)
    {
        if (!_grassContainers.Contains(container))
        {
            _grassContainers.Add(container);

            _requireUpdate = true;
        }
    }

    public void RemoveContainer(IGrassContainer container)
    {
        var removed = _grassContainers.Remove(container);

        if (removed)
        {
            _requireUpdate = true;
        }
    }

    private void UpdateIfNeeded()
    {
        foreach (var container in _grassContainers)
        {
            if (container.RequiresUpdate)
            {
                _requireUpdate = true;
            }
        }

        if (_requireUpdate)
        {
            ForceListUpdate();

            _requireUpdate = false;
        }
    }

    private void ForceListUpdate()
    {
        var count = 0;

        foreach (var container in _grassContainers)
        {
            count += container.PositionsRef.Count;
        }

        var list = new List<Vector3>(count);

        foreach (var container in _grassContainers)
        {
            list.AddRange(container.PositionsRef);
        }

        _renderer.SetGrassPositions(list);
    }
}
