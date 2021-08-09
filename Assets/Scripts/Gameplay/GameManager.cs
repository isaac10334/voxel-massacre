using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public int redTeamDeaths;
    public int blueTeamDeaths;
    public int deathsToWin = 30;
    public float maximumMatchTimeInSeconds = 300;
    public int newRoundCountdownInSeconds = 10;
    public Transform spawnPosOne;
    public Transform spawnPosTwo;
    private float _timer;
    private bool _gameActive;
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else if(Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public override void OnStartServer()
    {
        StartGame();
    }

    public void StartGame()
    {
        _timer = maximumMatchTimeInSeconds;
        _gameActive = true;

        ClientStartGame();
    }

    [ClientRpc]
    private void ClientStartGame()
    {
        Debug.Log("Game starting.");
        UIThings.Instance.OnGameStart();
    }
    
    private void Update()
    {
        if(!isServer) return;
        
        if(!_gameActive) return;

        _timer -= Time.deltaTime;

        // only do this every 15 frames to save network traffic
        if((Time.frameCount % 15) == 0)
            ClientUpdateTimer(_timer);

        if(_timer <= 0) GameOver();
    }
    
    [ClientRpc]
    private void ClientUpdateTimer(float time)
    {
        UIThings.Instance.UpdateTimer(time);
    }

    public void PlayerDied(Player player)
    {
        if(!isServer) return;

        if(player.team == Team.Blue)
        {
            blueTeamDeaths++;
        }
        else if(player.team == Team.Red)
        {
            redTeamDeaths++;
        }

        ClientUpdateScore(redTeamDeaths, blueTeamDeaths);

        if(blueTeamDeaths == deathsToWin || redTeamDeaths == deathsToWin)
        {
            GameOver();
        }
    }
    
    [ClientRpc]
    private void ClientUpdateScore(int redDeaths, int blueDeaths)
    {
        UIThings.Instance.UpdateScore(redDeaths, blueDeaths);
    }

    private void GameOver()
    {
        if(!isServer) return;

        _gameActive = false;

        if(redTeamDeaths == blueTeamDeaths)
        {
            ClientTie();
        }
        else if (redTeamDeaths > blueTeamDeaths)
        {
            ClientTeamWon(Team.Blue);

        }
        else if(blueTeamDeaths > redTeamDeaths)
        {
            ClientTeamWon(Team.Red);
        }

        StartCoroutine(NewRoundCountdown());
    }

    private IEnumerator NewRoundCountdown()
    {
        if(!isServer) yield break;

        for(int i = newRoundCountdownInSeconds; i >= 0; i--)
        {
            ClientUpdateRoundCountdown(i);
            yield return new WaitForSeconds(1);
        }

        RestartGame();
    }

    [Server]
    private void RestartGame()
    {
        ((MyNetworkManager)(NetworkManager.singleton)).ServerChangeScene("Main");
    }

    [ClientRpc]
    private void ClientUpdateRoundCountdown(int secondsLeft)
    {
        UIThings.Instance.gameOverScreen.UpdateNewRoundCountdown(secondsLeft);
    }

    [ClientRpc]
    private void ClientTeamWon(Team team)
    {
        UIThings.Instance.GameOver(team);
    }
    [ClientRpc]
    private void ClientTie()
    {
        UIThings.Instance.Tie();
    }

}
