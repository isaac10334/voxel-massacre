using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Projectile : NetworkBehaviour
{
    public NetworkIdentity owner;
    [SerializeField] private int damage = 2;
    [SerializeField] private int headshotDamage = 20;
    [SerializeField] private ParticleSystem smoke;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip headshotDing;
    [SerializeField] private AudioClip hitPlayerSound;
    private bool _playedSmoke;
    
    private void Start()
    {
        Invoke(nameof(Disappear), 3f);
    }

    void OnCollisionEnter(Collision collision)
    {
        if(!isServer) return;

        bool isHeadshot = collision.gameObject.CompareTag("Head");

        if(!isHeadshot && !collision.gameObject.CompareTag("Body")) return;

        Player player = collision.gameObject.GetComponentInParent<Player>();

        bool hitPlayer = player != null;

        if(!_playedSmoke && !hitPlayer)
        {
            ClientPlaySmoke();
            _playedSmoke = true;
        }

        if(!hitPlayer) return;

        player.TakeDamage(owner, isHeadshot ? headshotDamage : damage);

        TargetShotPlayer(owner.connectionToClient, isHeadshot);
    }

    [TargetRpc]
    private void TargetShotPlayer(NetworkConnection target, bool isHeadshot)
    {
        audioSource.PlayOneShot(hitPlayerSound);

        if(isHeadshot)
        {
            audioSource.PlayOneShot(headshotDing);
        }
    }

    [ClientRpc]
    private void ClientPlaySmoke()
    {
        smoke.Play();
    }
    
    void Disappear()
    {
        NetworkServer.Destroy(gameObject);
    }
}
