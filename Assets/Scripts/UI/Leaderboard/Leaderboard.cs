using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Leaderboard : MonoBehaviour
{
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private GameObject controller;
    [SerializeField] private GameObject activationContainer;

    private void Awake()
    {
        activationContainer.SetActive(false);
    }

    public void OpenLeaderboard()
    {
        activationContainer.SetActive(true);
    }
    public void CloseLeaderboard()
    {
        activationContainer.SetActive(false);
    }

    public void AddPlayer(NetworkIdentity identity)
    {
        uint netId = identity.netId;
        string playerUsername = identity.GetComponent<Player>().username;

        GameObject newController = Instantiate(controller, itemsContainer);
        LeaderboardController leaderboardController = newController.GetComponent<LeaderboardController>();

        leaderboardController.Initialize(netId, playerUsername);
    }

    public void RemovePlayer(NetworkIdentity identity)
    {

    }
}
