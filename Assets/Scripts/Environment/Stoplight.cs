using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stoplight : MonoBehaviour
{

    private enum Lights  { Green, Yellow, Red };
    private Lights lightActive = Lights.Green;

    [SerializeField] private float interval;
    [SerializeField] private GameObject green;
    [SerializeField] private GameObject yellow;
    [SerializeField] private GameObject red;
    private float _timer;
    
    private void Update()
    {
        _timer += Time.deltaTime;
        if(_timer >= interval)
        {
            _timer = 0f;

            if(lightActive == Lights.Green)
            {
                green.SetActive(false);
                red.SetActive(false);
                yellow.SetActive(true);

                lightActive = Lights.Yellow;
            }
            else if(lightActive == Lights.Yellow)
            {
                green.SetActive(false);
                yellow.SetActive(false);  
                red.SetActive(true);
            }
            else if(lightActive == Lights.Red)
            {
                green.SetActive(true);
                yellow.SetActive(false);
                red.SetActive(false);
                lightActive = Lights.Green;
            }
        }
    }
}
