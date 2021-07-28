using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuAirplaineMove : MonoBehaviour
{
    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        rb.AddForce(0, 100, 0);   
    }
}
