using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    public AudioClip breakdown;
    public AudioSource audioSource;

    private void Awake()
    {
		if (instance == null)
		{
			DontDestroyOnLoad(gameObject);
			instance = this;
		}
    }

    public void PlayBreakdown()
    {
        audioSource.PlayOneShot(breakdown);
    }

    public void StopPlayingBreakdown()
    {
      audioSource.Stop();
    }

}
