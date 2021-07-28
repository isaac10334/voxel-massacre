using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandPlaneDeath : MonoBehaviour
{
    public float deathForce;
    private LevelManager levelManager;
    private bool planeDied = false;

    [SerializeField]
    private AudioSource audioSource;
    [SerializeField]
    private AudioClip explosionSound;
    
    private ParticleSystem particleSystem;
    float timer;
    private bool canCollide = false;

    private void Start()
    {
        levelManager = GameObject.FindObjectOfType<LevelManager>();
        particleSystem = GetComponent<ParticleSystem>();

    }
    private void Update()
    {
        timer += Time.deltaTime;

        if(timer >= 8f)
        {
            canCollide = true;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("plane collision");
        if ((collision.relativeVelocity.magnitude > deathForce) && canCollide)
        {
            if(!planeDied)
            {
                PlaneDeath();
            }
        }

    }

    private void PlaneDeath()
    {
        audioSource.PlayOneShot(explosionSound, 0.9f);
        particleSystem.Play();
        Destroy(gameObject, particleSystem.main.duration);
    }
}
