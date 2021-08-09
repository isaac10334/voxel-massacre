using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LeaderboardController : MonoBehaviour
{
    public uint playerNetId;
    [SerializeField] private TMP_Text username;
    [SerializeField] private TMP_Text totalKills;
    [SerializeField] private TMP_Text totalDeaths;

    public void Initialize(uint playerNetId, string username)
    {
        this.playerNetId = playerNetId;
        this.username.text = username;
    }

    public void UpdateController(int kills, int deaths)
    {
        totalKills.text = kills.ToString();
        totalDeaths.text = deaths.ToString();
    }

    public void Remove()
    {
        Destroy(gameObject);
    }

}
