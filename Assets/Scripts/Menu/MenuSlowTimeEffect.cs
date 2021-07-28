using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuSlowTimeEffect : MonoBehaviour
{
    private async void Start()
    {
        await new WaitForSeconds(0.7f);
        Time.timeScale = 0.1f;
    }
}
