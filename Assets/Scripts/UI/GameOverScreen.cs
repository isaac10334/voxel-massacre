using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text newRoundCountdownText;
    public void PlayerWon(Team playerTeam)
    {
        OpenGameOverScreen();
        resultText.text = "SUCCESS";
    }
    public void PlayerLost(Team playerTeam)
    {
        OpenGameOverScreen();
        resultText.text = "FAILURE";
    }
    public void Tie()
    {
        OpenGameOverScreen();
        resultText.text = "TIE";
    }

    private void OpenGameOverScreen()
    {
        gameOverScreen.SetActive(true);
    }
    public void CloseGameOverScreen()
    {
        gameOverScreen.SetActive(false);
    }

    public void UpdateNewRoundCountdown(int secondsLeft)
    {
        newRoundCountdownText.text = secondsLeft + "...";
    }
}
