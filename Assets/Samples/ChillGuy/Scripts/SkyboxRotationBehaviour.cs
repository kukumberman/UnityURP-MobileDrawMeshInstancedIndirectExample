using UnityEngine;

public class SkyboxRotationBehaviour : MonoBehaviour
{
    [SerializeField]
    private float _speed;

    private Material _material;

    private float _elapsedTime = 0;

    private void Start()
    {
        _material = RenderSettings.skybox;
    }

    private void Update()
    {
        _elapsedTime += _speed * Time.deltaTime;

        _material.SetFloat("_Rotation", _elapsedTime % 360f);
    }
}
