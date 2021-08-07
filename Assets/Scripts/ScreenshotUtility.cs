using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ScreenshotUtility : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        if(!Application.isEditor)
        {
            gameObject.SetActive(false);
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.T))
        {
            byte[] bytes = I360Render.Capture(1024, true, NetworkClient.localPlayer.GetComponent<PlayerMovement>().cam, true);
            System.IO.File.WriteAllBytes(Application.dataPath + "/screenshot.jpg", bytes);
        }
    }
}
