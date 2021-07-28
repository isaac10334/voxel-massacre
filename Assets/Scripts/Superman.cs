using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Superman : MonoBehaviour
{
    private Player player;
    private Transform positionToFollow;
    [SerializeField]
    private float speed;

    private NavMeshAgent agent;

    //if health gets to zero, he's inactive for a while
    public int health = 150;

    private bool playerDriving;

    [SerializeField]
    private float shootDistance;

    [SerializeField]
    public Transform[] paceBetween;

    private bool seePlayer;

    [SerializeField]
    private AudioClip gotchaSound;
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private float seeTime;

    [SerializeField]
    private GameObject exclamationMark;

    private bool gotchaIsOk = true;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        InvokeRepeating("FindPlayer", 1.0f, 0.3f); 
    }

    private void Update()
    {   
        if(seePlayer)
        {
            seeTime += Time.deltaTime;
        }

        if(seePlayer && seeTime >= 1f && gotchaIsOk)
        {
            Gotcha();
        }

        if(playerDriving)
        {
            FlyTowardsPlayer();
        }
        else
        {
            if(positionToFollow != null)
            {
                if(player != null)
                {
                    if ((positionToFollow.position - this.transform.position).sqrMagnitude<shootDistance*shootDistance) 
                    {
                      RotateAround();  
                    }
                }
            }
        }
    }

    private void FlyTowardsPlayer()
    {

    }

    private void RotateAround()
    {
        var rotation = Quaternion.LookRotation(positionToFollow.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 9f);
    }

    private void Gotcha()
    {
        gotchaIsOk = false;
        Debug.Log("playing gotcha sound");
        audioSource.PlayOneShot(gotchaSound, 0.8f);
        SetExclamationMark();
    }

    private async void SetExclamationMark()
    {
        exclamationMark.SetActive(true);
        await new WaitForSeconds(1);
        exclamationMark.SetActive(false);       
    }

    private void FindPlayer()
    {
        player = GameObject.FindObjectOfType<Player>();

        bool foundPlayer = false;

        var capsule = GetComponent<CapsuleCollider>();
        Vector3 center = capsule.center;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position + center, 30f);

        foreach (Collider collider in hitColliders)
        {   
            if(collider.tag == "Player")
            {
                foundPlayer = true;
                seePlayer = true;
            }
            else if(foundPlayer)
            {
                seePlayer = true;
            }
            else
            {
                seePlayer = false;
            }
        }
        if(player == null)
        {
            CheckPlayerDriving();
        }
        else
        {
            positionToFollow = player.transform;
        }
    }

    private void CheckPlayerDriving()
    {
        Vehicle[] vehicles = FindObjectsOfType<Vehicle>();
        foreach(Vehicle car in vehicles)
        {
            if(car.isPlayerDriving)
            {
                playerDriving = true;
            }
        }
    }


}
