using UnityEngine;
using System.Collections;
using System.Collections.Generic;
    
[System.Serializable]
public class AxleInfo
{
    public WheelCollider leftWheel;
    public WheelCollider rightWheel;
    public bool motor;
    public bool steering;
}

//This script brakes using the same keys you drive with - W and S, just like in many popular games.
public class Vehicle : Interactable
{
    public bool isPlayerDriving;
    public List<AxleInfo> axleInfos;
    public float maxMotorTorque;
    public float maxSteeringAngle;
    private Rigidbody rb;
    public float brake;
    public float brakeForce;
    public float defaultBrake;

    public float forwardSpeed;
    public float backwardsSpeed;

    public float motor;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3 (0, -0.9f, 0); 
    }

    public override void Interact()
    {
        Debug.Log("Player tried to enter car.");
    }
        
    private void FixedUpdate()
    {

        float steering = maxSteeringAngle * Input.GetAxis("Horizontal");
        brake = defaultBrake;

        motor = 0.0f;

        Vector3 forwardVelocity = transform.InverseTransformDirection(rb.velocity);
        
        if(forwardVelocity.z < -0.1f && Input.GetKey(KeyCode.W))
        {
            brake = rb.mass * brakeForce;
            motor = 0.0f;
        }
        else if(Input.GetKey(KeyCode.W))
        {
            motor = maxMotorTorque * forwardSpeed;
            brake = 0f;
        }

        if(forwardVelocity.z > 0.1f  && Input.GetKey(KeyCode.S))
        {
            brake = rb.mass * brakeForce;
            motor = 0.0f;
        }
        else if(Input.GetKey(KeyCode.S))
        {
            motor = maxMotorTorque * -(backwardsSpeed);
            brake = 0f;
        }

        foreach (AxleInfo axleInfo in axleInfos) 
        {

            axleInfo.leftWheel.brakeTorque = brake;
            axleInfo.rightWheel.brakeTorque = brake;

            if (axleInfo.steering) 
            {
                axleInfo.leftWheel.steerAngle = steering;
                axleInfo.rightWheel.steerAngle = steering;
            }

            if (axleInfo.motor)
            {
                axleInfo.leftWheel.motorTorque = motor;
                axleInfo.rightWheel.motorTorque = motor;
            }
            ApplyLocalPositionToVisuals(axleInfo.leftWheel);
            ApplyLocalPositionToVisuals(axleInfo.rightWheel);
        }

    }

    public void ApplyLocalPositionToVisuals(WheelCollider collider)
    {
        if (collider.transform.childCount == 0) {
            return;
        }
     
        Transform visualWheel = collider.transform.GetChild(0);
     
        Vector3 position;
        Quaternion rotation;
        collider.GetWorldPose(out position, out rotation);
     
        visualWheel.transform.position = position;
        visualWheel.transform.rotation = rotation;
    }
}
    
