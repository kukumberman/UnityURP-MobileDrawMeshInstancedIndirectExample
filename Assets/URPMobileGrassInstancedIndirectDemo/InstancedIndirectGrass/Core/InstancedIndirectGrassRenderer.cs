//see this for ref: https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstancedIndirect.html

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public interface IGrassContainer
{
    IReadOnlyList<Vector3> PositionsRef { get; }

    bool RequiresUpdate { get; }
}

[DefaultExecutionOrder(-100)]
public class InstancedIndirectGrassRenderer : MonoBehaviour
{
    [Header("Settings")]
    public float drawDistance = 125; //this setting will affect performance a lot!
    public Material instanceMaterial;

    [SerializeField]
    private bool _useCullingPerGrass;

    //smaller the number, CPU needs more time, but GPU is faster
    [SerializeField]
    private Vector3 cellSize = Vector3.one * 10; //unity unit (m)

    //y test allow 50% more threshold (hardcode for grass)
    //x test allow 10% more threshold (hardcode for grass)
    [SerializeField]
    private Vector2 _threshold = new Vector2(1.1f, 1.5f);

    [SerializeField]
    private bool _useCustomMesh = false;

    [SerializeField]
    private Mesh _customMesh = null;

    [Header("Internal")]
    public ComputeShader cullingComputeShader;

    [SerializeField]
    private bool shouldBatchDispatch = true;

    [Header("Debug")]
    [SerializeField]
    private bool _drawCellsGizmo;

    [SerializeField]
    private bool _drawVisibleCells;

    [SerializeField]
    private bool _drawInvisibleCells;

    [SerializeField]
    private bool _drawCellLabel;

    private List<IGrassContainer> _grassContainers = new();

    private List<Vector3> allGrassPos = new List<Vector3>(); //user should update this list using C#

    //=====================================================
    [HideInInspector]
    public static InstancedIndirectGrassRenderer instance; // global ref to this script

    private bool _requireUpdate = false;

    private int cellCountX = -1;
    private int cellCountY = -1;
    private int cellCountZ = -1;
    private int dispatchCount = -1;

    private Bounds _bounds;
    private Grid3D _grid = new Grid3D();

    private int instanceCountCache = -1;
    private Mesh cachedGrassMesh;

    private ComputeBuffer allInstancesPosWSBuffer;
    private ComputeBuffer visibleInstancesOnlyPosWSIDBuffer;
    private ComputeBuffer argsBuffer;

    private List<Vector3>[] cellPosWSsList; //for binning: binning will put each posWS into correct cell
    private Vector3 min;
    private Vector3 max;

    private List<int> visibleCellIDList = new List<int>();
    private Plane[] cameraFrustumPlanes = new Plane[6];

    private Mesh _mesh;

    private List<MyCell> _myCells = new();
    private int _kernelIndex = -1;
    private uint _kernelThreadGroupSizeX;

    //=====================================================

    private void Awake()
    {
        _mesh = _useCustomMesh ? _customMesh : GetGrassMeshCache();
        Debug.Assert(_mesh != null);

        _kernelIndex = cullingComputeShader.FindKernel("CSMain");

        cullingComputeShader.GetKernelThreadGroupSizes(
            _kernelIndex,
            out _kernelThreadGroupSizeX,
            out _,
            out _
        );
    }

    private void OnEnable()
    {
        instance = this; // assign global ref using this script
    }

    void Update()
    {
        const string keyword = "CULLING_PER_CHUNK";
        if (!_useCullingPerGrass)
        {
            cullingComputeShader.EnableKeyword(keyword);
        }
        else
        {
            cullingComputeShader.DisableKeyword(keyword);
        }

        foreach (var container in _grassContainers)
        {
            if (container.RequiresUpdate)
            {
                _requireUpdate = true;
            }
        }

        if (_requireUpdate)
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

            SetGrassPositions(list);

            _requireUpdate = false;
        }

        // recreate all buffers if needed
        UpdateAllInstanceTransformBufferIfNeeded();

        //=====================================================================================================
        // rough quick big cell frustum culling in CPU first
        //=====================================================================================================
        visibleCellIDList.Clear(); //fill in this cell ID list using CPU frustum culling first
        Camera cam = Camera.main;

        //Do frustum culling using per cell bound
        //https://docs.unity3d.com/ScriptReference/GeometryUtility.CalculateFrustumPlanes.html
        //https://docs.unity3d.com/ScriptReference/GeometryUtility.TestPlanesAABB.html
        float cameraOriginalFarPlane = cam.farClipPlane;
        cam.farClipPlane = Mathf.Min(cam.farClipPlane, drawDistance); //allow drawDistance control
        GeometryUtility.CalculateFrustumPlanes(cam, cameraFrustumPlanes); //Ordering: [0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far
        cam.farClipPlane = cameraOriginalFarPlane; //revert far plane edit

        //slow loop
        //TODO: (A)replace this forloop by a quadtree test?
        //TODO: (B)convert this forloop to job+burst? (UnityException: TestPlanesAABB can only be called from the main thread.)
        Profiler.BeginSample("CPU cell frustum culling (heavy)");

        for (int i = 0; i < _myCells.Count; i++)
        {
            var cell = _myCells[i];
            cell.Visible = GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, cell.Bounds);

            if (cell.Visible)
            {
                visibleCellIDList.Add(i);
            }
        }
        Profiler.EndSample();

        //=====================================================================================================
        // then loop though only visible cells, each visible cell dispatch GPU culling job once
        // at the end compute shader will fill all visible instance into visibleInstancesOnlyPosWSIDBuffer
        //=====================================================================================================
        Matrix4x4 v = cam.worldToCameraMatrix;
        Matrix4x4 p = cam.projectionMatrix;
        Matrix4x4 vp = p * v;

        visibleInstancesOnlyPosWSIDBuffer.SetCounterValue(0);

        //set once only
        cullingComputeShader.SetMatrix("_VPMatrix", vp);
        cullingComputeShader.SetFloat("_MaxDrawDistance", drawDistance);
        cullingComputeShader.SetVector("_Threshold", _threshold);

        //dispatch per visible cell
        dispatchCount = 0;
        for (int i = 0; i < visibleCellIDList.Count; i++)
        {
            int targetCellFlattenID = visibleCellIDList[i];
            int memoryOffset = 0;
            for (int j = 0; j < targetCellFlattenID; j++)
            {
                memoryOffset += cellPosWSsList[j].Count;
            }
            cullingComputeShader.SetInt("_StartOffset", memoryOffset); //culling read data started at offseted pos, will start from cell's total offset in memory
            int jobLength = cellPosWSsList[targetCellFlattenID].Count;

            //============================================================================================
            //batch n dispatchs into 1 dispatch, if memory is continuous in allInstancesPosWSBuffer
            if (shouldBatchDispatch)
            {
                while (
                    (i < visibleCellIDList.Count - 1)
                    && //test this first to avoid out of bound access to visibleCellIDList
                    (visibleCellIDList[i + 1] == visibleCellIDList[i] + 1)
                )
                {
                    //if memory is continuous, append them together into the same dispatch call
                    jobLength += cellPosWSsList[visibleCellIDList[i + 1]].Count;
                    i++;
                }
            }
            //============================================================================================

            if (jobLength == 0)
            {
                continue;
            }
            var threadGroupsX = Mathf.CeilToInt(jobLength / (float)_kernelThreadGroupSizeX);
            cullingComputeShader.Dispatch(_kernelIndex, threadGroupsX, 1, 1); //disaptch.X division number must match numthreads.x in compute shader (e.g. 64)
            dispatchCount++;
        }

        //====================================================================================
        // Final 1 big DrawMeshInstancedIndirect draw call
        //====================================================================================
        // GPU per instance culling finished, copy visible count to argsBuffer, to setup DrawMeshInstancedIndirect's draw amount
        ComputeBuffer.CopyCount(visibleInstancesOnlyPosWSIDBuffer, argsBuffer, 4);

        // Render 1 big drawcall using DrawMeshInstancedIndirect
        //if camera frustum is not overlapping this bound, DrawMeshInstancedIndirect will not even render
        Graphics.DrawMeshInstancedIndirect(_mesh, 0, instanceMaterial, _bounds, argsBuffer);
    }

    //private void OnGUI()
    //{
    //    GUI.contentColor = Color.black;
    //    GUI.Label(
    //        new Rect(200, 0, 400, 60),
    //        $"After CPU cell frustum culling,\n"
    //            + $"-Visible cell count = {visibleCellIDList.Count}/{cellCountX * cellCountZ}\n"
    //            + $"-Real compute dispatch count = {dispatchCount} (saved by batching = {visibleCellIDList.Count - dispatchCount})"
    //    );

    //    shouldBatchDispatch = GUI.Toggle(
    //        new Rect(400, 400, 200, 100),
    //        shouldBatchDispatch,
    //        "shouldBatchDispatch"
    //    );
    //}

    void OnDisable()
    {
        //release all compute buffers
        if (allInstancesPosWSBuffer != null)
            allInstancesPosWSBuffer.Release();
        allInstancesPosWSBuffer = null;

        if (visibleInstancesOnlyPosWSIDBuffer != null)
            visibleInstancesOnlyPosWSIDBuffer.Release();
        visibleInstancesOnlyPosWSIDBuffer = null;

        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;

        instance = null;
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

    private void SetGrassPositions(IList<Vector3> positions)
    {
        int divider = (int)_kernelThreadGroupSizeX;
        int remainder = positions.Count % divider;
        int extraSize = remainder == 0 ? 0 : divider - remainder;

        allGrassPos = new List<Vector3>(positions.Count + extraSize);
        allGrassPos.AddRange(positions);

        CalculateBounds();

        for (int i = 0; i < extraSize; i++)
        {
            allGrassPos.Add(min);
        }

        instanceCountCache = 0;
    }

    Mesh GetGrassMeshCache()
    {
        if (!cachedGrassMesh)
        {
            //if not exist, create a 3 vertices hardcode triangle grass mesh
            cachedGrassMesh = new Mesh();

            //single grass (vertices)
            Vector3[] verts = new Vector3[3];
            verts[0] = new Vector3(-0.25f, 0);
            verts[1] = new Vector3(+0.25f, 0);
            verts[2] = new Vector3(-0.0f, 1);
            //single grass (Triangle index)
            int[] trinagles = new int[3] { 2, 1, 0, }; //order to fit Cull Back in grass shader

            cachedGrassMesh.SetVertices(verts);
            cachedGrassMesh.SetTriangles(trinagles, 0);
        }

        return cachedGrassMesh;
    }

    void UpdateAllInstanceTransformBufferIfNeeded()
    {
        //always update
        instanceMaterial.SetVector("_PivotPosWS", _bounds.center);
        instanceMaterial.SetVector("_BoundSize", _bounds.size);

        if (allGrassPos.Count == 0)
        {
            return;
        }

        //early exit if no need to update buffer
        if (
            instanceCountCache == allGrassPos.Count
            && argsBuffer != null
            && allInstancesPosWSBuffer != null
            && visibleInstancesOnlyPosWSIDBuffer != null
        )
        {
            return;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        Debug.Log("UpdateAllInstanceTransformBuffer (Slow)");

        ///////////////////////////
        // allInstancesPosWSBuffer buffer
        ///////////////////////////
        if (allInstancesPosWSBuffer != null)
            allInstancesPosWSBuffer.Release();
        allInstancesPosWSBuffer = new ComputeBuffer(allGrassPos.Count, sizeof(float) * 3); //float3 posWS only, per grass

        if (visibleInstancesOnlyPosWSIDBuffer != null)
            visibleInstancesOnlyPosWSIDBuffer.Release();
        visibleInstancesOnlyPosWSIDBuffer = new ComputeBuffer(
            allGrassPos.Count,
            sizeof(uint),
            ComputeBufferType.Append
        ); //uint only, per visible grass

        CalculateBounds();

        //init per cell posWS list memory
        cellPosWSsList = new List<Vector3>[cellCountX * cellCountY * cellCountZ]; //flatten 2D array
        for (int i = 0; i < cellPosWSsList.Length; i++)
        {
            cellPosWSsList[i] = new List<Vector3>();
        }

        _myCells.Clear();

        _grid.GridSize = new Vector3Int(cellCountX, cellCountY, cellCountZ);

        Vector3 sizeWS = new Vector3(
            Mathf.Abs(max.x - min.x) / cellCountX,
            Mathf.Abs(max.y - min.y) / cellCountY,
            Mathf.Abs(max.z - min.z) / cellCountZ
        );

        for (int i = 0; i < cellPosWSsList.Length; i++)
        {
            var gridPos = _grid.IndexToGrid(i);
            Vector3 centerPosWS = GridToWorld(gridPos);

            Bounds cellBound = new Bounds(centerPosWS, sizeWS);

            var cell = new MyCell
            {
                Index = i,
                GridPosition = gridPos,
                IndexReversedCalc = _grid.GridToIndex(gridPos),
                Bounds = cellBound,
                Visible = false
            };
            _myCells.Add(cell);
        }

        //binning, put each posWS into the correct cell
        for (int i = 0; i < allGrassPos.Count; i++)
        {
            Vector3 pos = allGrassPos[i];
            Vector3Int gridPos = WorldToGrid(pos);
            var index = _grid.GridToIndex(gridPos);
            cellPosWSsList[index].Add(pos);
        }

        //combine to a flatten array for compute buffer
        int offset = 0;
        Vector3[] allGrassPosWSSortedByCell = new Vector3[allGrassPos.Count];
        for (int i = 0; i < cellPosWSsList.Length; i++)
        {
            for (int j = 0; j < cellPosWSsList[i].Count; j++)
            {
                allGrassPosWSSortedByCell[offset] = cellPosWSsList[i][j];
                offset++;
            }
        }

        allInstancesPosWSBuffer.SetData(allGrassPosWSSortedByCell);
        instanceMaterial.SetBuffer("_AllInstancesTransformBuffer", allInstancesPosWSBuffer);
        instanceMaterial.SetBuffer(
            "_VisibleInstanceOnlyTransformIDBuffer",
            visibleInstancesOnlyPosWSIDBuffer
        );

        ///////////////////////////
        // Indirect args buffer
        ///////////////////////////
        if (argsBuffer != null)
            argsBuffer.Release();
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        argsBuffer = new ComputeBuffer(
            1,
            args.Length * sizeof(uint),
            ComputeBufferType.IndirectArguments
        );

        args[0] = (uint)_mesh.GetIndexCount(0);
        args[1] = (uint)allGrassPos.Count;
        args[2] = (uint)_mesh.GetIndexStart(0);
        args[3] = (uint)_mesh.GetBaseVertex(0);
        args[4] = 0;

        argsBuffer.SetData(args);

        ///////////////////////////
        // Update Cache
        ///////////////////////////
        //update cache to prevent future no-op buffer update, which waste performance
        instanceCountCache = allGrassPos.Count;

        //set buffer
        cullingComputeShader.SetBuffer(
            _kernelIndex,
            "_AllInstancesPosWSBuffer",
            allInstancesPosWSBuffer
        );
        cullingComputeShader.SetBuffer(
            _kernelIndex,
            "_VisibleInstancesOnlyPosWSIDBuffer",
            visibleInstancesOnlyPosWSIDBuffer
        );
    }

    private Vector3 GridToWorld(Vector3Int gridPosition)
    {
        Vector3 centerPosWS = new Vector3(
            gridPosition.x + 0.5f,
            gridPosition.y + 0.5f,
            gridPosition.z + 0.5f
        );
        centerPosWS.x = Mathf.Lerp(min.x, max.x, centerPosWS.x / cellCountX);
        centerPosWS.y = Mathf.Lerp(min.y, max.y, centerPosWS.y / cellCountY);
        centerPosWS.z = Mathf.Lerp(min.z, max.z, centerPosWS.z / cellCountZ);

        return centerPosWS;
    }

    private Vector3Int WorldToGrid(Vector3 pos)
    {
        //find cellID
        int xID = Mathf.Min(
            cellCountX - 1,
            Mathf.FloorToInt(Mathf.InverseLerp(min.x, max.x, pos.x) * cellCountX)
        ); //use min to force within 0~[cellCountX-1]
        int yID = Mathf.Min(
            cellCountY - 1,
            Mathf.FloorToInt(Mathf.InverseLerp(min.y, max.y, pos.y) * cellCountY)
        );
        int zID = Mathf.Min(
            cellCountZ - 1,
            Mathf.FloorToInt(Mathf.InverseLerp(min.z, max.z, pos.z) * cellCountZ)
        ); //use min to force within 0~[cellCountZ-1]

        return new Vector3Int(xID, yID, zID);
    }

    private void CalculateBounds()
    {
        //find all instances's posWS XZ bound min max
        min.x = float.MaxValue;
        min.y = float.MaxValue;
        min.z = float.MaxValue;
        max.x = float.MinValue;
        max.y = float.MinValue;
        max.z = float.MinValue;
        for (int i = 0; i < allGrassPos.Count; i++)
        {
            Vector3 target = allGrassPos[i];
            min.x = Mathf.Min(target.x, min.x);
            min.y = Mathf.Min(target.y, min.y);
            min.z = Mathf.Min(target.z, min.z);
            max.x = Mathf.Max(target.x, max.x);
            max.y = Mathf.Max(target.y, max.y);
            max.z = Mathf.Max(target.z, max.z);
        }

        //decide cellCountX,Z here using min max
        //each cell is cellSizeX x cellSizeZ
        cellCountX = Mathf.CeilToInt((max.x - min.x) / cellSize.x);
        cellCountY = Mathf.CeilToInt((max.y - min.y) / cellSize.y);
        cellCountZ = Mathf.CeilToInt((max.z - min.z) / cellSize.z);

        cellCountY = Mathf.Max(1, cellCountY);

        _bounds = new Bounds();
        _bounds.SetMinMax(min, max);
    }

    private void OnDrawGizmos()
    {
        if (_drawCellsGizmo)
        {
            DrawCells();
        }
    }

    private void DrawCells()
    {
        foreach (var cell in _myCells)
        {
            var shouldRender =
                (_drawVisibleCells && cell.Visible) || (_drawInvisibleCells && !cell.Visible);

            if (!shouldRender)
            {
                continue;
            }

            Gizmos.color = cell.Visible ? Color.green : Color.red;
            Gizmos.DrawWireCube(cell.Bounds.center, cell.Bounds.size * 0.99f);

            if (!_drawCellLabel)
            {
                continue;
            }

#if UNITY_EDITOR
            GUI.color = cell.Index == cell.IndexReversedCalc ? Color.white : Color.red;
            UnityEditor.Handles.Label(
                cell.Bounds.center,
                $"{cell.Index} ({cell.IndexReversedCalc}): {cell.GridPosition};",
                GUI.skin.box
            );
#endif
        }
    }
}

public sealed class MyCell
{
    public int Index;
    public Vector3Int GridPosition;
    public int IndexReversedCalc;
    public Bounds Bounds;
    public bool Visible;
}

public class Grid3D
{
    public Vector3Int GridSize;

    public Vector3Int IndexToGrid(int index)
    {
        return new Vector3Int(
            index % GridSize.x,
            (index / GridSize.x) % GridSize.y,
            (index / (GridSize.x * GridSize.y))
        );
    }

    public int GridToIndex(Vector3Int grid)
    {
        return GridToIndex(grid.x, grid.y, grid.z);
    }

    public int GridToIndex(int x, int y, int z)
    {
        return z * (GridSize.x * GridSize.y) + y * GridSize.x + x;
    }
}
