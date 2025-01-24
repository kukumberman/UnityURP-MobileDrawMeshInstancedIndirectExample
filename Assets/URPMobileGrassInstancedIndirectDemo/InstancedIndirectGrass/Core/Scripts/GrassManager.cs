using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GrassManager : MonoBehaviour
{
    public static GrassManager instance = null;

    [SerializeField]
    private InstancedIndirectGrassRenderer[] _renderers;

    private Dictionary<string, GrassEntry> _grassMap;

    private void Awake()
    {
        _grassMap = _renderers.ToDictionary(
            x => x.Id,
            x => new GrassEntry
            {
                Id = x.Id,
                Renderer = x,
                Containers = new List<IGrassContainer>()
            }
        );
    }

    private void OnEnable()
    {
        instance = this;
    }

    private void Update()
    {
        foreach (var item in _grassMap.Values)
        {
            UpdateIfNeeded(item);
        }
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
        if (!_grassMap.ContainsKey(container.Id))
        {
            return;
        }

        var entry = _grassMap[container.Id];

        if (!entry.Containers.Contains(container))
        {
            entry.Containers.Add(container);

            entry.RequireUpdate = true;
        }
    }

    public void RemoveContainer(IGrassContainer container)
    {
        if (!_grassMap.ContainsKey(container.Id))
        {
            return;
        }

        var entry = _grassMap[container.Id];

        var removed = entry.Containers.Remove(container);

        if (removed)
        {
            entry.RequireUpdate = true;
        }
    }

    private void UpdateIfNeeded(GrassEntry entry)
    {
        foreach (var container in entry.Containers)
        {
            if (container.RequiresUpdate)
            {
                entry.RequireUpdate = true;
                break;
            }
        }

        if (entry.RequireUpdate)
        {
            ForceListUpdate(entry);

            entry.RequireUpdate = false;
        }
    }

    private void ForceListUpdate(GrassEntry entry)
    {
        var count = 0;

        foreach (var container in entry.Containers)
        {
            count += container.PositionsRef.Count;
        }

        var list = new List<Vector3>(count);

        foreach (var container in entry.Containers)
        {
            list.AddRange(container.PositionsRef);
        }

        entry.Renderer.SetGrassPositions(list);
    }

    [ContextMenu(nameof(CacheRenderers))]
    private void CacheRenderers()
    {
        var components = new List<InstancedIndirectGrassRenderer>();

        GetComponents(components);
        GetComponentsInChildren(false, components);

        _renderers = components.ToArray();
    }
}

public class GrassEntry
{
    public string Id;
    public InstancedIndirectGrassRenderer Renderer;
    public List<IGrassContainer> Containers;
    public bool RequireUpdate;
}
