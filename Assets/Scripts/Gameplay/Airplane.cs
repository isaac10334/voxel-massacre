using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Airplane : Interactable
{
    #pragma warning disable 0649   	
    public bool playerDriving = false;
    [SerializeField] private bool useRotateableCamera;
    [SerializeField] private float lookSpeed = 3;
    [SerializeField] private Transform camHolder;
    private Vector2 _rotation = Vector2.zero;
    private bool _canExit = false;
    #pragma warning restore 0649   	

    public override void Interact()
    {
        EnterCar();
    }

    private void Update()
    {
        if(playerDriving && _canExit)
        {
            if(Input.GetKeyDown(KeyCode.E))
            {
                ExitCar();
            }
        }

        if(playerDriving)
        {
            ControlCamera();
        }
    }

    private void ControlCamera()
    {
        if(!useRotateableCamera)
            return;
        
        _rotation.y += Input.GetAxis("Mouse X");
        _rotation.x += -Input.GetAxis("Mouse Y");
        _rotation.x = Mathf.Clamp(_rotation.x, -15f, 15f);

        camHolder.transform.localRotation = Quaternion.Euler(_rotation.x * lookSpeed, 0, 0);
    }
    private async void EnterCar()
    {
        _canExit = false;

        if(playerDriving)
            return;

        Player.Instance.StartDrivingMode(camHolder);
        playerDriving = true;

        if(MusicManager.instance)
            MusicManager.instance.PlayBreakdown();

        await new WaitForSeconds(1);
        _canExit = true;

        if(UIThings.Instance)
            UIThings.Instance.ShowNotificationText("Use the mouse and WASD to steer. Click to drop the bomb.", 4f);

    }
    private void ExitCar()
    {
        MusicManager.instance.StopPlayingBreakdown();
        playerDriving = false;
        Player.Instance.StopDrivingMode();
    }
}
