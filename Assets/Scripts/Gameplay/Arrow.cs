using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{

    public GameObject arrow;

    void Start()
    {
        ShowArrow();
    }

    public async void ShowArrow()
    {
        arrow.SetActive(true);
        await new WaitForSeconds(5);
        arrow.SetActive(false);
    }
}
