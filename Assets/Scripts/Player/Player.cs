using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;
public class Player : NetworkBehaviour
{
    [SyncVar]
    public Team team;
    [SyncVar]
    public string username;
    public bool isDriving;
    public bool addItemsOnStart = true;
    public ItemsDatabase itemsDatabase;
    public static Player Instance;
    private int health;
    public Transform hand;
    public int maxInventorySize;
    public readonly SyncList<InventoryItem> inventory = new SyncList<InventoryItem>();

    public int raycastRange;
    public GameObject enemyStuff;
    public TMP_Text playerText;
    [SerializeField] private float onDamageShakeAmount = 0.5f;
    [SerializeField] private float onDamageShakeDuration = 0.25f;
    [SerializeField] private ParticleSystem blood;
    [SerializeField] private AudioSource audioSource;

    #region Sounds
    [SerializeField] private AudioClip takeDamageSound;
    #endregion

    private Ray _ray;
    private RaycastHit _hit;
    private PlayerMovement _playerMovement;
    private int _currentlyEquippedSlot;

    private void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
    }

    public override void OnStartLocalPlayer()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else if(Instance != this)
        {
            Destroy(gameObject);
        }
        
        PlayerInput.InputEnabled = true;
        Player.Instance.UnlockPlayer();

        UIThings.Instance.CloseMenu();

        // just spawned so health should be full
        UIThings.Instance.SetHealthText(100);
        
        Player[] players = FindObjectsOfType<Player>();
        foreach(Player player in players)
        {
            bool playerIsEnemy = player.team != this.team;

            if(playerIsEnemy)
            {
                MarkPlayerAsEnemy(player);
            }
            else
            {
                MarkPlayerAsFriendly(player);
            }
        }

        CmdNotifyOtherPlayers();
    }
    
    [Command]
    private void CmdNotifyOtherPlayers(NetworkConnectionToClient sender = null)
    {
        ClientNotifyPlayers(sender.identity);
    }

    [ClientRpc(includeOwner = false)]
    private void ClientNotifyPlayers(NetworkIdentity playerIdentity)
    {
        bool isEnemy = NetworkClient.localPlayer.GetComponent<Player>().team != playerIdentity.GetComponent<Player>().team;

        if(isEnemy)
        {
            MarkPlayerAsEnemy(this);
        }
        else
        {
            MarkPlayerAsFriendly(this);
        }
    }

    private void MarkPlayerAsEnemy(Player player)
    {
        player.enemyStuff.SetActive(true);
        player.playerText.text = player.username;
        player.playerText.color = Color.red;
    }
    private void MarkPlayerAsFriendly(Player player)
    {
        player.enemyStuff.SetActive(false);
        player.playerText.text = player.username;
        player.playerText.color = Color.green;
    }


    public override void OnStartServer()
    {
        health = 100;
    }

    public void Start()
    {
        if(addItemsOnStart)
        {
            AddItem("Machine Gun");
            AddItem("Revolver");
        }
    }

    void Update()
    {
        if(!isLocalPlayer) return;

        if(Input.GetKeyDown(KeyCode.L))
        {
            CmdTakeDamage(10);
        }

        // if(isDriving) return;

        if(!PlayerInput.InputEnabled) return;
        
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            Equip(0);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            Equip(1);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha3))
        {
            Equip(2);
        }

        HandleScrolling();

        // HandleRaycastThings();

        // if(Input.GetKeyDown(KeyCode.E))
        // {
        //     RaycastOnce();
        // }
    }

    private void HandleScrolling()
    {
        float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
        if(scrollWheel < 0)
        {
            if(_currentlyEquippedSlot < 2)
            {
                Equip(_currentlyEquippedSlot + 1);
            }
        }
        else if(scrollWheel > 0)
        {
            if(_currentlyEquippedSlot > 0)
            {
                Equip(_currentlyEquippedSlot - 1);
            }
        }
    }

    private void HandleRaycastThings()
    {
        _ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        
        if(Physics.Raycast(_ray, out _hit, raycastRange))
        {
            if(_hit.transform.CompareTag("Interactable"))
            {
                Interactable i = _hit.transform.GetComponentInChildren<Interactable>();

                if(!i)
                {
                    i = _hit.transform.GetComponentInParent<Interactable>();
                }

                if(i && UIThings.Instance)
                    UIThings.Instance.ShowInteractionText(i.textToDisplayOnLook);
            }
        }
        else
        {
            if(UIThings.Instance)
                UIThings.Instance.ShowInteractionText("");
        }
    }

    private void RaycastOnce()
    {
        if(Physics.Raycast(_ray, out _hit, raycastRange))
        {
            if(_hit.transform.CompareTag("Interactable"))
            {
                Interactable i = _hit.transform.GetComponentInChildren<Interactable>();

                if(!i)
                {
                    i = _hit.transform.GetComponentInParent<Interactable>();
                }

                if(!i) return;

                i.Interact();
            }
        }
    }

    [Command]
    public void CmdTakeDamage(int amount, NetworkConnectionToClient sender = null)
    {
        TakeDamage(sender.identity, amount);
    }

    [Server]
    public void TakeDamage(NetworkIdentity playerResponsible, int amount)
    {
        if(amount < 0) return;

        health -= amount;

        if(health <= 0)
        {
            Die(playerResponsible);
            return;
        }
        
        TargetTakeDamage(health);
        ClientTakeDamage();
    }

    // consider experimenting with this, could be fine for the owner to see the blood.
    [ClientRpc(includeOwner = false)]
    private void ClientTakeDamage( )
    {
        blood.Play();
    }


    // On take damage for just this player locally
    [TargetRpc]
    private void TargetTakeDamage(int amount)
    {
        UIThings.Instance.SetHealthText(amount);

        audioSource.PlayOneShot(takeDamageSound);

        _playerMovement.ShakeCamera(onDamageShakeAmount, onDamageShakeDuration);
    }
    
    public void Heal(int amount)
    {
        if(!isServer) return;
        
        if((health + amount) > 100)
        {
            health = 100;
        }
        else
        {
            health = health + amount;
        }
    }

    private void Die(NetworkIdentity playerResponsible)
    {
        if(!isServer) return;

        GameManager.Instance.PlayerDied(this);
            
        TargetDie();

        ClientUpdateDeaths(playerResponsible, netIdentity);

        // consider just disabling the gameobject?
        // eh.... nothing wrong with destroying it, right? Hmm..
        // perhaps the TargetRPC doesn't have time to go through, or something? 
    }

    [ClientRpc]
    private void ClientUpdateDeaths(NetworkIdentity playerResponsible, NetworkIdentity playerWhoDied)
    {
        UIThings.Instance.UpdateDeaths(playerResponsible.GetComponent<Player>(), playerWhoDied.GetComponent<Player>());
    }

    [TargetRpc]
    private void TargetDie()
    {
        Debug.Log("target die i am dieing now i am the target");
        UIThings.Instance.SetHealthText(0);
        UIThings.Instance.OnPlayerDeath();

        CmdDestroyPlayer();
    }

    [Command]
    private void CmdDestroyPlayer()
    {
        NetworkServer.Destroy(gameObject);
    }

    public void LockPlayer()
    {
        PlayerInput.InputEnabled = false;
        _playerMovement.movementStopped = true;
    }
    public void UnlockPlayer()
    {
        PlayerInput.InputEnabled = true;
        _playerMovement.movementStopped = false;
    }

    #region Inventory

    private void Equip(int itemSlot)
    {
        // client stuff
        UIThings.Instance.SelectSlot(itemSlot);

        CmdEquip(itemSlot);
    }

    [Command]
    public void CmdEquip(int itemSlot)
    {
        foreach(Transform child in hand.transform)
        {
            NetworkServer.Destroy(child.gameObject);
        }

        foreach(InventoryItem item in inventory)
        {
            if(item.slot == itemSlot)
            {
                _currentlyEquippedSlot = itemSlot;

                GameObject itemPrefab = itemsDatabase.GetItemByName(item.name).prefab;

                GameObject newItem = Instantiate(itemPrefab);

                newItem.transform.parent = hand;
                newItem.transform.position = hand.position;
                newItem.transform.rotation = hand.rotation;
                
                if(itemsDatabase.GetItemByName(item.name).itemInfo)
                {
                    newItem.GetComponent<ItemInfo>().itemName = item.name;
                    newItem.GetComponent<ItemInfo>().slot = itemSlot;
                }

                NetworkServer.Spawn(newItem, gameObject);

                // newItem.GetComponent<NetworkIdentity>().AssignClientAuthority
                ClientEquip(newItem);

                return;
            }
        }
    }
    
    [ClientRpc]
    private void ClientEquip(GameObject newItem)
    {
        if(newItem == null) return;

        newItem.transform.parent = hand;
        newItem.transform.position = hand.position;
        newItem.transform.rotation = hand.rotation;
    }
    
    public void AddItem(string itemName)
    {
        if(!isServer) return;

        if(inventory.Count > maxInventorySize) return;

        InventoryItem newItem = new InventoryItem{ name = itemName, slot = inventory.Count, };

        inventory.Add(newItem);

        TargetAddItem(newItem);
    }

    [TargetRpc]
    private void TargetAddItem(InventoryItem item)
    {
        UIThings.Instance.AddInventoryItemToUI(item);
    }
    
    [Server]
    public bool DestroyItem(string itemName)
    {
        foreach(InventoryItem item in inventory)
        {
            if(item.name == itemName)
            {
                if(_currentlyEquippedSlot == item.slot)
                {
                    foreach(Transform child in hand.transform)
                    {
                        NetworkServer.Destroy(child.gameObject);
                    }
                }

                UIThings.Instance.RemoveItemFromUI(item.slot);
                inventory.Remove(item);

                return true;
            }
        }

        return false;
    }

    public bool ContainsItem(string itemName, int amount)
    {
        foreach(InventoryItem item in inventory)
        {
            if(item.name == itemName) return true;
        }

        return false;
    }

    #endregion

    #region Driving
    public void StartDrivingMode(Transform holder)
    {
        Equip(-1);
        isDriving = true;

        if(UIThings.Instance)
            UIThings.Instance.ShowToolbar(false);

        Destroy(gameObject.GetComponent<Rigidbody>());

        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<CharacterController>().enabled = false;
        transform.position = holder.position;
        transform.rotation = holder.rotation;
        transform.parent = holder;
        gameObject.GetComponent<Collider>().enabled = false;
    }

    public void StopDrivingMode()
    {
        isDriving = false;
        UIThings.Instance.ShowToolbar(true);

        transform.parent = null;
        GetComponent<PlayerMovement>().enabled = true;
        GetComponent<CharacterController>().enabled = true;
        Destroy(GetComponent<FixedJoint>());
        gameObject.GetComponent<Collider>().enabled = true;
    }
#endregion
}
