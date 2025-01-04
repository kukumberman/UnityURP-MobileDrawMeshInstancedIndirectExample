using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

public class Foobar : MonoBehaviour, IGrassContainer
{
    [SerializeField]
    private MeshFilter _meshFilter;

    [SerializeField]
    private int _sampleCount;

    [SerializeField]
    private bool _useFilterByNormal;

    [SerializeField]
    private float _normalDotThreshold;

    [SerializeField]
    private Vector3 _targetNormal;

    [Header("Gizmo")]
    [SerializeField]
    private bool _drawGizmo;

    [SerializeField]
    private float _gizmoRadius;

    private MeshData.Vertex[] _vertices;

    private List<Vector3> _positions;

    IReadOnlyList<Vector3> IGrassContainer.PositionsRef => _positions;

    bool IGrassContainer.RequiresUpdate => false;

    private void OnEnable()
    {
        Setup();

        InstancedIndirectGrassRenderer.Add(this);
    }

    private void OnDisable()
    {
        InstancedIndirectGrassRenderer.Remove(this);
    }

    [ContextMenu(nameof(Setup))]
    private void Setup()
    {
        var mesh = _meshFilter.sharedMesh;

        var meshData = VFXMeshSamplingHelper.ComputeDataCache(mesh);
        var rand = new System.Random(1234);
        var bakedSampling = new TriangleSampling[_sampleCount];

        for (int i = 0; i < _sampleCount; ++i)
        {
            bakedSampling[i] = VFXMeshSamplingHelper.GetNextSampling(meshData, rand);
        }

        _vertices = new MeshData.Vertex[_sampleCount];

        for (int i = 0; i < _vertices.Length; i++)
        {
            _vertices[i] = VFXMeshSamplingHelper.GetInterpolatedVertex(meshData, bakedSampling[i]);
        }

        for (int i = 0; i < _vertices.Length; i++)
        {
            _vertices[i].normal = transform.TransformDirection(_vertices[i].normal);
            _vertices[i].position = transform.TransformPoint(_vertices[i].position);
        }

        if (_useFilterByNormal)
        {
            _vertices = _vertices.Where(IsValidVertexWithNormal).ToArray();
        }

        _positions = _vertices.Select(x => x.position).ToList();
    }

    private bool IsValidVertexWithNormal(MeshData.Vertex vertex)
    {
        var dot = Vector3.Dot(vertex.normal, _targetNormal);

        return dot > _normalDotThreshold;
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmo)
        {
            return;
        }

        if (_vertices == null)
        {
            return;
        }

        Gizmos.color = Color.black;

        for (int i = 0; i < _vertices.Length; i++)
        {
            var origin = _vertices[i].position;
            var next = origin + _vertices[i].normal * _gizmoRadius;

            Gizmos.DrawLine(origin, next);
        }
    }
}
