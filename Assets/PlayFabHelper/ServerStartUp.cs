using System.Collections;
using UnityEngine;
using PlayFab;
using System;
using PlayFab.Networking;
using System.Collections.Generic;
using PlayFab.MultiplayerAgent.Model;
using Mirror;

public class ServerStartUp : MonoBehaviour
{

	public Configuration configuration;

	private List<ConnectedPlayer> _connectedPlayers;
	public MyNetworkManager networkManager;

	void Start()
	{
		if (configuration.buildType == BuildType.REMOTE_SERVER)
		{
			StartRemoteServer();
		}
	}

	public void OnStartLocalServerButtonClick()
	{
		if (configuration.buildType == BuildType.LOCAL_SERVER)
		{
			networkManager.StartServer();
		}
	}

	private void StartRemoteServer()
	{
		Debug.Log("[ServerStartUp].StartRemoteServer");
		_connectedPlayers = new List<ConnectedPlayer>();
		PlayFabMultiplayerAgentAPI.Start();
		PlayFabMultiplayerAgentAPI.IsDebugging = configuration.playFabDebugging;
		PlayFabMultiplayerAgentAPI.OnMaintenanceCallback += OnMaintenance;
		PlayFabMultiplayerAgentAPI.OnShutDownCallback += OnShutdown;
		PlayFabMultiplayerAgentAPI.OnServerActiveCallback += OnServerActive;
		PlayFabMultiplayerAgentAPI.OnAgentErrorCallback += OnAgentError;

		StartCoroutine(ReadyForPlayers());
		StartCoroutine(ShutdownServerInXTime());
	}

	IEnumerator ShutdownServerInXTime()
	{
		yield return new WaitForSeconds(300f);
		OnShutdown();
	}

	IEnumerator ReadyForPlayers()
	{
		yield return new WaitForSeconds(.5f);
		PlayFabMultiplayerAgentAPI.ReadyForPlayers();
	}

	private void OnServerActive()
	{
		networkManager.StartServer();
	}
	
	public void OnPlayerRemoved(string playfabId)
	{
		ConnectedPlayer player = _connectedPlayers.Find(x => x.PlayerId.Equals(playfabId, StringComparison.OrdinalIgnoreCase));
		_connectedPlayers.Remove(player);
		PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(_connectedPlayers);
		CheckPlayerCountToShutdown();
	}

	private void CheckPlayerCountToShutdown()
	{
		if (_connectedPlayers.Count <= 0)
		{
			OnShutdown();
		}
	}
	
	public void OnPlayerAdded(string playfabId)
	{
		_connectedPlayers.Add(new ConnectedPlayer(playfabId));
		PlayFabMultiplayerAgentAPI.UpdateConnectedPlayers(_connectedPlayers);
	}

	private void OnAgentError(string error)
	{
		Debug.Log(error);
	}

	private void OnShutdown()
	{
		networkManager.ShutDown();
	}

	private void OnMaintenance(DateTime? NextScheduledMaintenanceUtc)
	{
		Debug.Log("Maintenance now");
	}
}
