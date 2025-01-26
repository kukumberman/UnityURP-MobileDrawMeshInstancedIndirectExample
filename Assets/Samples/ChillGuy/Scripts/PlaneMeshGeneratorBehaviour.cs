using UnityEngine;
using static UnityEditor.Progress;

public sealed class PlaneMeshGeneratorBehaviour : MonoBehaviour
{
    [SerializeField]
    private bool _regenerateInUpdate;

    [SerializeField]
    private MeshFilter _meshFilter;

    [SerializeField]
    private PlaneMeshGenerator _meshGenerator;

    [SerializeField]
    private InstancedIndirectGrassPosDefine[] _grassContainers;

    [SerializeField]
    private Transform _point;

    [Header("Noise")]
    [SerializeField]
    private Vector3 _offset;

    [SerializeField]
    private Vector2 _frequency;

    [SerializeField]
    private float _amplitude;

    private bool _needsUpdate;

    private Vector2 _frequencyInternal;

    private void Start()
    {
        _meshGenerator.VertexPositionTransformer = VertexPositionTransformer;

        foreach (var container in _grassContainers)
        {
            container.VertexPositionTransformer = VertexPositionTransformer;
        }

        GenerateAndApply();
    }

    private void OnValidate()
    {
        _needsUpdate = true;
    }

    private void Update()
    {
        if (_regenerateInUpdate)
        {
            _needsUpdate = true;
        }

        if (_needsUpdate)
        {
            _needsUpdate = false;

            GenerateAndApply();
        }

        if (_point != null)
        {
            _point.position = VertexPositionTransformer(_point.position);
        }
    }

    private void GenerateAndApply()
    {
        UpdateInternalValues();

        _meshFilter.mesh = _meshGenerator.Generate();

        var bounds = _meshFilter.sharedMesh.bounds;
        var scale = bounds.size;
        var position = bounds.center;
        position.y = 0;

        foreach (var container in _grassContainers)
        {
            container.transform.localScale = new Vector3(scale.x, 1, scale.z);
            container.transform.position = position;

            container.gameObject.SetActive(true);
            container.enabled = true;
        }
    }

    private void UpdateInternalValues()
    {
        _frequencyInternal = _frequency * 0.1f;
    }

    private Vector3 VertexPositionTransformer(Vector3 position)
    {
        var pos = position;

        var noise01 = Mathf.PerlinNoise(
            _offset.x + pos.x * _frequencyInternal.x,
            _offset.z + pos.z * _frequencyInternal.y
        );

        var height = Mathf.Lerp(-1f * _amplitude, _amplitude, noise01);

        pos.y = height;

        pos.y += _offset.y;

        return pos;
    }
}
