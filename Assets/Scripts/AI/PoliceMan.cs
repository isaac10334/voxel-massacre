using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

public class PoliceMan : MonoBehaviour
{
    public AIManager aiManager;
    private Player player;
    [SerializeField]
    private Transform positionToFollow;
    [SerializeField]
    public enum EnemyType {segwayMan, superMan, shooter};
    public EnemyType enemyType;

    private NavMeshAgent agent;

    public int health = 100;

    public AudioClip deathSound;
    public AudioClip attackSound;
    public AudioSource audioPlayer;
    private bool seePlayer;

    [SerializeField]
    private float tickRate = 0.2f;
    [SerializeField]
    private float accuracyPercent = 50f;

    private float timer = 0;
    private bool _isDying;
    private MeshRenderer[] _mr;

    private void Awake()
    {
        _mr = GetComponentsInChildren<MeshRenderer>();
        agent = GetComponent<NavMeshAgent>();
        InvokeRepeating("FindPlayer", 1.0f, 1f);
    }

    private void Start()
    {
        positionToFollow = Player.Instance.transform;
    }

    private void Update()
    {
        if(_isDying)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(90, 0, 0), Time.deltaTime * 40f);
            return;
        }
        
        if(enemyType == EnemyType.segwayMan && positionToFollow != null)
        {
            agent.destination = positionToFollow.position;      
        }

        timer += Time.deltaTime;

        if(timer > tickRate)
        {
            if(enemyType == EnemyType.shooter)
            {
                LookForPlayer();
                ShootPlayer();
            }
            
            timer = timer - tickRate;
        }

    }
    
    private void LookForPlayer()
    {
        if(positionToFollow != null)
        {
            var rotation = Quaternion.LookRotation(positionToFollow.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 9f);
        }
    }
    private void ShootPlayer()
    {
        throw new System.NotImplementedException();
        
        if(_isDying) return;
        
        RaycastHit hit;
        Vector3 direction = Vector3.zero;
        if(positionToFollow != null)
        {
            direction = positionToFollow.position - transform.position;
        }
        if(Physics.Raycast(transform.position, direction, out hit))
        {
            if(hit.collider.tag == "Player")
            {      
                audioPlayer.PlayOneShot(attackSound, 0.3f);
                float randomRange = Random.Range(0, 100f);
                if(randomRange >= accuracyPercent)
                {
                    if(player)
                    {
                        // player.TakeDamage(5);
                    }
                }
            }
        }
    }

    private Transform CheckPlayerDriving()
    {
        Vehicle[] vehicles = FindObjectsOfType<Vehicle>();
        foreach(Vehicle v in vehicles)
        {
            if(v.isPlayerDriving)
            {
                return v.transform;
            }
        }
        return null;
    }

    public void TakeDamage(float damage)
    {
        if(_isDying) return;
        health -= Mathf.RoundToInt(damage);

        foreach(MeshRenderer mr in _mr)
        {
            if(mr.sharedMaterial)
                mr.sharedMaterial.DOColor(Color.red, 1, 0.5f);
        }

        transform.DOPunchPosition(-transform.forward * 1.5f, 0.5f);

        if(health <= 0)
        {
            _isDying = true;
            StartCoroutine(Die());
        }
    }

    private IEnumerator Die()
    {   
        audioPlayer.clip = deathSound;
        audioPlayer.PlayOneShot(deathSound, 1f);
        yield return new WaitForSeconds(deathSound.length);
        
        aiManager.AIDied();

        if(gameObject != null)
            Destroy(gameObject);
    }
}
