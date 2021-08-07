using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeSetter : MonoBehaviour
{
    [SerializeField] private float timeScale;

    private void Awake()
    {
        Time.timeScale = timeScale;
    }
}
