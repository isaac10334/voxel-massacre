using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{

    public GameObject policemanLevelOne;
    public GameObject sniperMan;
    // public GameObject superman;
    private int _currentAI;
    private int _level;

    void Start()
    {
        SpawnSegwayMen(15, policemanLevelOne);
    }

    private void Update()
    {
        if(_currentAI == 0)
        {
            _level += 1;
            SpawnSegwayMen(15, policemanLevelOne);

            if(_level >= 1)
            {
                SpawnSnipers(_level);
            }
        }
    }
    private void SpawnSegwayMen(int amount, GameObject obj)
    {
        Transform player = Player.Instance.transform;

        for(int i = 0; i < amount; i++)
        {
            GameObject segwayMan = Instantiate(obj, transform);
            var randomPos = Random.insideUnitSphere * 45;
            randomPos.y = 0f;
            _currentAI++;
            segwayMan.transform.position = player.transform.position + (player.transform.forward * 10) + randomPos;
            segwayMan.GetComponent<PoliceMan>().aiManager = this;
        }
    }
    
    private void SpawnSnipers(int amount)
    {
        PoliceMan[] policeMen = FindObjectsOfType<PoliceMan>(true);

        int i = 0;

        foreach(PoliceMan p in policeMen)
        {
            if(i == amount) return;
            if(p.gameObject.activeInHierarchy == false && p.enemyType == PoliceMan.EnemyType.shooter)
            {
                p.gameObject.SetActive(true);
                i += 1;
            }
        }
        // // Building[] buildings = FindObjectsOfType<Building>();
        // // Building b = buildings[Random.Range(0, buildings.Length)];

        // Transform player = Player.Instance.transform;

        // for(int i = 0; i < amount; i++)
        // {
        //     GameObject sniper = Instantiate(sniperMan, transform);
        //     var randomPos = Random.insideUnitSphere * 45;
        //     _currentAI++;
        //     sniper.transform.position = player.transform.position + (player.transform.forward * 10) + randomPos;
        //     sniper.GetComponent<PoliceMan>().aiManager = this;
        // }
    }

    public void AIDied()
    {
        _currentAI--;
    }
}
