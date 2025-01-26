using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChillGuyBehaviour : MonoBehaviour
{
    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private string _animEarsTriggerName;

    [SerializeField]
    private float _waitTimeSeconds = 5;

    private void Start()
    {
        StartCoroutine(MyCoroutine());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TriggerEars();
        }
    }

    private void TriggerEars()
    {
        _animator.SetTrigger(_animEarsTriggerName);
    }

    private IEnumerator MyCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_waitTimeSeconds);

            TriggerEars();
        }
    }
}
