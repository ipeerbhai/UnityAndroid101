using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Instantiation : MonoBehaviour
{

    public Transform Prefab;

    void Start()
    {
        Instantiate(Prefab, new Vector3(0, 3, 10), Quaternion.Euler(0, 0, 0));
    }
}