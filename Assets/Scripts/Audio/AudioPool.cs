using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPool : MonoBehaviour
{

    public static AudioPool instance;

    #pragma warning disable 0649
    [SerializeField] private int poolTarget;
    [SerializeField] private int numberOfGameobjectsToPreinstantiate;
    [SerializeField] private int maxAmount;
    [SerializeField] private int audioSourcesPerObject = 1;
    #pragma warning restore 0649

    private List<GameObject> pooledAudioSources = new List<GameObject>();

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else if(instance != this)
        {
            Destroy(gameObject);
        }

        for(int i = 0; i < numberOfGameobjectsToPreinstantiate; i++)
        {
            GameObject newObj = new GameObject("AudioSource");
            newObj.transform.parent = transform;

            for(int j = 0; j < audioSourcesPerObject; j++)
            {
                AudioSource audioSource = newObj.AddComponent<AudioSource>();

                audioSource.playOnAwake = false;
                audioSource.loop = false;
            }

            newObj.SetActive(false);

            pooledAudioSources.Add(newObj);
        }
    }

    public async void PlaySoundHere(Vector3 position, AudioClip sound, float volume = 1f)
    {
        GameObject obj = GetPooledAudioSource();
        AudioSource a = obj.GetComponent<AudioSource>();
        a.spatialBlend = 1f;

        obj.transform.position = position;
        a.PlayOneShot(sound, volume);

        await new WaitForSeconds(sound.length);

        ReturnToPool(obj);
    }

    private GameObject GetPooledAudioSource()
    {
        foreach(GameObject obj in pooledAudioSources)
        {
            if(!obj.activeInHierarchy)
            {
                obj.SetActive(true);
                return obj;
            } 
        }

        // EXPAND
        if(pooledAudioSources.Count < maxAmount)
        {
            GameObject newObj = new GameObject("AudioSource");
            AudioSource a = newObj.AddComponent<AudioSource>();
            a.loop = false;
            a.playOnAwake = false;
            return newObj;
        }

        throw new System.InvalidOperationException("Could not get AudioSource.");
    }
    private void ReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
    }
}
