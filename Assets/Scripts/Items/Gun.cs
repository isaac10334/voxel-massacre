using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Mirror;

[RequireComponent(typeof(AudioSource))]
public class Gun : NetworkBehaviour
{
    public Transform rightHandGrip;
    public Transform leftHandGrip;
    public Transform pivot;
    [SerializeField] private float projectileForce = 40f;
    [SerializeField] private new ParticleSystem particleSystem;
    [SerializeField] private Transform barrel;
    [SerializeField] private Vector3 aimDownSightPos;
    [SerializeField] private Vector3 aimDownSightRot;
    [SerializeField] private float shootRate;
    [SerializeField] private float tweenDuration;
    [SerializeField] private bool fullyAutomatic;
    [SerializeField] private GameObject projectile;
    [SerializeField] private AudioClip gunshotSound;
    [SerializeField] private AudioClip outOfAmmoSound;
    [SerializeField] private Transform gunHolder;
    [SerializeField] private Vector3 recoilRotation;
    [SerializeField] private float recoilRotationDuration;
    private float timer = 0f;
    private AudioSource audioSource;
    private Vector3 _originalPosition;
    private Vector3 _originalRotation;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        _originalPosition = gunHolder.localPosition;
        _originalRotation = gunHolder.localEulerAngles;
    }

    private void Update()
    {
        if(!hasAuthority) return;

        if(!PlayerInput.InputEnabled) return;

        Camera cam = NetworkClient.localPlayer.GetComponent<PlayerMovement>().cam;

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hitInfo))
        {
            Vector3 direction = hitInfo.point - barrel.transform.position;
            barrel.transform.rotation = Quaternion.Euler(0, 90, 0) * Quaternion.LookRotation(direction);
        }

        if(Input.GetKeyDown(KeyCode.R)) TryReload();

        timer += Time.deltaTime;

        if(Input.GetMouseButton(1))
        {
            gunHolder.DOLocalMove(aimDownSightPos, tweenDuration);
            gunHolder.DOLocalRotate(aimDownSightRot, tweenDuration);
        }
        if(Input.GetMouseButtonUp(1))
        {
            gunHolder.DOLocalMove(_originalPosition, tweenDuration);
            gunHolder.DOLocalRotate(_originalRotation, tweenDuration);
        }

        if(fullyAutomatic)
        {
            HandFullyAutomatic();
        }
        else
        {
            NotFullyAutomatic();
        }
    }

    private void HandFullyAutomatic()
    {
        if(timer > shootRate)
        {
            if(Input.GetMouseButton(0))
            {
                Fire();
                timer = 0f;
            }
        }
    }
    private void NotFullyAutomatic()
    {
        if(timer > shootRate)
        {
            if(Input.GetMouseButtonDown(0))
            {
                Fire();
                timer = 0f;
            }
        }
    }

    private void Fire()
    {
        if(Player.Instance.EnoughAmmo())
        {
            audioSource.PlayOneShot(gunshotSound);
            particleSystem.Play();
            pivot.DOPunchRotation(recoilRotation, recoilRotationDuration, 1, 1);
            
            CmdFire();
        }
        else
        {
            OnOutOfAmmo();
        }
    }

    private void OnOutOfAmmo()
    {
        audioSource.PlayOneShot(outOfAmmoSound);
    }

    private void TryReload()
    {
        Player.Instance.CmdReload();
    }
    
    [Command]
    private void CmdFire(NetworkConnectionToClient sender = null)
    {
        Player player = sender.identity.GetComponent<Player>();

        // the rest of this method requires ammo, so if we can't get it from the player return
        if(!player.EnoughAmmo()) return;
        player.CmdUseAmmo();

        ClientFire();

        GameObject newObj = Instantiate(projectile);
        newObj.transform.position = barrel.transform.position;
        // transform.position + transform.forward * spawnInFrontDistance;
        newObj.transform.rotation = transform.rotation;

        Rigidbody rb = newObj.GetComponent<Rigidbody>();
        rb.AddForce(transform.forward * projectileForce, ForceMode.Impulse);
        rb.AddForce(Physics.gravity * 4.5f, ForceMode.Acceleration);

        // set up projectile identity
        newObj.GetComponent<Projectile>().owner = sender.identity;

        // bullets should not hit the player that shot them.
        foreach(Collider coll in sender.identity.GetComponentsInChildren<Collider>())
        {
            Physics.IgnoreCollision(newObj.GetComponent<Collider>(), coll);
        }

        NetworkServer.Spawn(newObj);

        ClientFixCollisionIssue(sender.identity, newObj);
    }
    
    [ClientRpc]
    private void ClientFixCollisionIssue(NetworkIdentity id, GameObject newObj)
    {
        foreach(Collider coll in id.GetComponentsInChildren<Collider>())
        {
            Physics.IgnoreCollision(newObj.GetComponent<Collider>(), coll);
        }
    }

    [ClientRpc(includeOwner = false)]
    private void ClientFire()
    {
        audioSource.PlayOneShot(gunshotSound);
        particleSystem.Play();
    }
}
