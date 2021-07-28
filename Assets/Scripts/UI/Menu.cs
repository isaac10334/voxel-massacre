using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [SerializeField]
    private GameObject loadingScreen;
    public void Play()
    {
        loadingScreen.SetActive(loadingScreen);
        SceneManager.LoadSceneAsync("Main");
    }
    public void Quit()
    {
        Application.Quit();
    }
}
