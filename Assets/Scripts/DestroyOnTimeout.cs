using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnTimeout : MonoBehaviour
{
    [SerializeField] public float destoryTimeoutInSecs = 30f;

    void Awake()
    {
        Destroy(gameObject, destoryTimeoutInSecs);
    }
}
