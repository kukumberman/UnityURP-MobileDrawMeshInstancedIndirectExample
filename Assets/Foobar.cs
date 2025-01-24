using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

public class Foobar : MonoBehaviour, IGrassContainer
{
    [SerializeField]
    private GrassScriptableObject _grass;

    [SerializeField]
    private MeshFilter _meshFilter;

    [SerializeField]
    private int _sampleCount;

    [SerializeField]
    private int _seed;

    [Header("Normal")]
    [SerializeField]
    private bool _useFilterByNormal;

    [SerializeField]
    private float _normalDotThreshold;

    [SerializeField]
    private Vector3 _targetNormal;

    [Header("Mask")]
    [SerializeField]
    private bool _useFilterByTextureMask;

    [SerializeField]
    private Texture2D _maskTexture;

    [Range(0, 7)]
    [SerializeField]
    private int _maskUvChannelIndex = 0;

    [Range(0f, 1f)]
    [SerializeField]
    private float _maskThreshold01 = 0.5f;

    [Header("Gizmo")]
    [SerializeField]
    private bool _drawGizmo;

    [SerializeField]
    private float _gizmoRadius;

    private int _internalSeed;

    private Vector2Int _textureSize;

    private MeshData.Vertex[] _vertices;

    private List<Vector3> _positions;

    string IGrassContainer.Id => _grass.Id;

    IReadOnlyList<Vector3> IGrassContainer.PositionsRef => _positions;

    bool IGrassContainer.RequiresUpdate => false;

    private void Awake()
    {
        _positions = new List<Vector3>(_sampleCount);
    }

    private void OnEnable()
    {
        Setup();

        GrassManager.Add(this);
    }

    private void OnDisable()
    {
        GrassManager.Remove(this);
    }

    [ContextMenu(nameof(Setup))]
    private void Setup()
    {
        _internalSeed = _seed >= 0 ? _seed : Random.Range(0, int.MaxValue);

        var mesh = _meshFilter.sharedMesh;

        var meshData = VFXMeshSamplingHelper.ComputeDataCache(mesh);
        var rand = new System.Random(_internalSeed);
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

        IEnumerable<MeshData.Vertex> vertices = _vertices;

        if (_useFilterByNormal)
        {
            vertices = vertices.Where(IsValidVertexWithNormal);
        }

        if (_useFilterByTextureMask && _maskTexture != null)
        {
            _textureSize = new Vector2Int(_maskTexture.width, _maskTexture.height);
            vertices = vertices.Where(IsValidVertexOnTextureMask);
        }

        _vertices = vertices.ToArray();

        var result = _vertices.Select(x => x.position);

        _positions.Clear();
        _positions.AddRange(result);
    }

    private bool IsValidVertexWithNormal(MeshData.Vertex vertex)
    {
        var dot = Vector3.Dot(vertex.normal, _targetNormal);

        return dot > _normalDotThreshold;
    }

    private bool IsValidVertexOnTextureMask(MeshData.Vertex vertex)
    {
        if (_maskUvChannelIndex >= vertex.uvs.Length)
        {
            return true;
        }

        var uv = vertex.uvs[_maskUvChannelIndex];

        var point = new Vector2Int(
            Mathf.FloorToInt(uv.x * _textureSize.x),
            Mathf.FloorToInt(uv.y * _textureSize.y)
        );

        var pixel = _maskTexture.GetPixel(point.x, point.y);

        return pixel.r > _maskThreshold01;
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
