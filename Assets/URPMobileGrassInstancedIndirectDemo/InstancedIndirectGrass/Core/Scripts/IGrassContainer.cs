using System.Collections.Generic;
using UnityEngine;

public interface IGrassContainer
{
    string Id { get; }

    IReadOnlyList<Vector3> PositionsRef { get; }

    bool RequiresUpdate { get; }
}
