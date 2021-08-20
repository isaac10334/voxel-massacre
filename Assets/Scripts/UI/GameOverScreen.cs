using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text newRoundCountdownText;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip winningSound;
    [SerializeField] private AudioClip losingSound;
    [SerializeField] private AudioClip tieSound;
    public void PlayerWon(Team playerTeam)
    {
        OpenGameOverScreen();
        // audioSource.PlayOneShot(winningSound);
        resultText.text = "SUCCESS";
    }
    public void PlayerLost(Team playerTeam)
    {
        OpenGameOverScreen();
        // audioSource.PlayOneShot(losingSound);
        resultText.text = "FAILURE";
    }
    public void Tie()
    {
        // audioSource.PlayOneShot(tieSound);
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
