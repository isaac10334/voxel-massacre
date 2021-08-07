using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using PlayFab;

public struct CreatePlayerMessage : NetworkMessage
{
    public string name;
    public string playFabId;
    public Avatar avatar;
}

public enum Avatar
{
    MrJoyride,
    Monkey
}

public struct Loadout
{
    public string primaryWeapon;
    public string secondaryWeapon;
    public string meleeWeapon;

    public Loadout(string primaryWeapon, string secondaryWeapon, string meleeWeapon)
    {
        this.primaryWeapon = primaryWeapon;
        this.secondaryWeapon = secondaryWeapon;
        this.meleeWeapon = meleeWeapon;
    }
}

public enum Team
{
    Red,
    Blue
}

[Serializable]
public class UnityNetworkConnection
{
    public bool IsAuthenticated;
    public string PlayFabId;
    public string LobbyId;
    public int ConnectionId;
    public NetworkConnection Connection;
}

public class MyNetworkManager : NetworkManager
{
    // [SerializeField] private ServerStartUp serverStartUp;
    // [SerializeField] private Transform spawnPosOne;
    [SerializeField] private Configuration configuration;

    public override void Awake()
    {
        base.Awake();

        if(Debug.isDebugBuild || Application.isEditor)
        {
            gameObject.AddComponent<NetworkManagerHUD>();
            return;
        }
    }

    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        base.OnClientSceneChanged(conn);

        Debug.Log("Hiding loading screen now.");
        UIThings.Instance.HideLoadingScreen();
        OpenMenu();
    }
    
    public override void OnStartServer()
    {
        base.OnStartServer();

        NetworkServer.RegisterHandler<CreatePlayerMessage>(OnCreateCharacter);
    }

    public override void OnStartClient()
    {
        OpenMenu();
        UIThings.Instance.HideLoadingScreen();
    }
    
    public override void OnStopClient()
    {
        Debug.Log("Client is disconnected and should be reconnected.");

        OpenMenu();
    }

    public void Spawn(Avatar avatarSelection, Loadout loadout, string username)
    {
        if(NetworkClient.connection == null) return;

        // choose team here. But note this is doint it on connect and I should have a sort of SPAWN function thing for death and respawning.
        CreatePlayerMessage characterMessage = new CreatePlayerMessage
        {
            name = String.IsNullOrEmpty(username) ? "Player" : username,
        };

        NetworkClient.connection.Send(characterMessage);
    }

    private void OnCreateCharacter(NetworkConnection conn, CreatePlayerMessage message)
    {
        if(conn.identity != null) return;

        GameObject playerGameObject = Instantiate(playerPrefab);

        Player player = playerGameObject.GetComponent<Player>();
        player.name = message.name;

        player.team = FindTeam();
        
        player.transform.position = player.team == Team.Red ? GameManager.Instance.spawnPosOne.position : GameManager.Instance.spawnPosTwo.position;

        // call this to use this gameobject as the primary controller
        if(!NetworkServer.AddPlayerForConnection(conn, playerGameObject))
        {
            Destroy(playerGameObject);
            return;
        }
    }
    private Team FindTeam()
    {
        int reds = 0;
        int blues = 0;

        if(NetworkServer.connections != null && NetworkServer.connections.Count > 0)
        {
            foreach(var connection in NetworkServer.connections.Values)
            {
                if(!connection.identity) continue;
                Player player = connection.identity.GetComponent<Player>();
                
                if(!player) continue;

                if(player.team == Team.Red)
                {
                    reds++;
                }
                else if(player.team == Team.Blue)
                {
                    blues++;
                }
            }
        }

        if(reds > blues)
        {
            return Team.Blue;
        }
        else if(blues > reds)
        {
            return Team.Red;
        }
        else
        {
            return (UnityEngine.Random.Range(0, 100) > 50) ? Team.Red : Team.Blue;
        }
    }

    private void OpenMenu()
    {
        UIThings.Instance.OpenMenuAsMainMenu();
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);

        if(NetworkServer.connections.Count == 0)
        {
            ShutDown();
        }

    }

    public void ShutDown()
    {
        NetworkServer.Shutdown();

		StartCoroutine(ShutdownServer());
    }

    IEnumerator ShutdownServer()
	{
		yield return new WaitForSeconds(5f);
		Application.Quit();
	}
}
