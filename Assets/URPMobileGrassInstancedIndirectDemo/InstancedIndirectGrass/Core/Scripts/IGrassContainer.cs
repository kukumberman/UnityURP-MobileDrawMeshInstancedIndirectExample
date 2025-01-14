using System.Collections.Generic;
using UnityEngine;

public interface IGrassContainer
{
    IReadOnlyList<Vector3> PositionsRef { get; }

    bool RequiresUpdate { get; }
}
