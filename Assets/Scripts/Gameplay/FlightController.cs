using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

/*
    This is a 3D First Person Controller script that can be used to simulate anything from 
    high speed aerial combat, to submarine warfare. I have tried to make my variable names
    as descriptive as possible so you can immediately see what they affect. The variables are
    sorted by type; speed with speed, rotation with rotation ect... I have also included generic
    ranges on variables that need them; relative upper and lower variable limits. This is because 
    some of the variables have a greater effect as they approach 1, while others have greater
    impact as they approach infinity. If for some reason the ship is not doing what it is supposed to,
    check the ranges, as some variables create problems when they are set to 0 or very large values.
    
    Also note the separate script titled Space Flight Script. This script has been
    optimized to better suit space combat. The effects of gravity, drag and lift are removed
    to better simulate flight in zero-gravity space.
    
    This script uses controls based off 4 axis. I found these parameters worked well...
        Name : (Roll, Pitch, Yaw or Throttle)
        Gravity : 20
        Dead : 0.001
        Sensitivity : 1
    
    Axis for each control (Axis based off a standard flight joystick).
        Pitch: Y- Axis
        Roll: X - Axis
        Yaw: 3'rd Axis
        Throttle: 4'th - Axis
    
    How to use this script: 
    
        Drag and Drop the Transform and its Rigidbody onto the variables Flyer and
        FlyerRigidbody in the inspector panel. Remember to change the rigidbody's Drag
        value. If you dont change this, gravity will be unrealistic... (I set drag to 500)
    
        Change the variables to simulate the flight style you desire.
    
        Create a prefab of your GameObject, and back it up to a secure location.
    
        *Note: This is important because none of the variables are stored in the 
        script. If for some reason Unity crashes during testing, the variables
        are not stored when you save the javascript, but when you save the game
        project.
    
        Save often and enjoy!
    
        ~Mirage~
      */

public class FlightController : MonoBehaviour
{
   // Components
   public Transform flyer;
   public Rigidbody flyerRigidbody;
   //var tail : Transform;
   
   // Assorted control variables. These mostly handle realism settings, change as you see fit.

   // Set these close to 0 to smooth out acceleration. Don't set it TO zero or you will have a division by zero error.
   public float accelerateSpeed = 5;          

   // I found this value gives semi-realistic deceleration, change as you see fit.        
   public float decelerateConst = 0.065f;    

   /* The ratio of MaxSpeed to Speed Const determines your true max speed. The formula is maxSpeed/SpeedConst = True Speed. 
      This way you wont have to scale your objects to make them seem like they are going fast or slow.
      MaxSpeed is what you will want to use for a GUI though.
   */ 
   public float maxSpeed  = 100f;     
   public float speedConst  = 50f;
   public float liftConst = 7.5f;             // Another arbitrary constant, change it as you see fit.
   public float angleOfAttack = 15f;         // Effective range: 0 <= angleOfAttack <= 20
   public float gravityConst = 9.8f;          // An arbitrary gravity constant, there is no particular reason it has to be 9.8...
   public int levelFlightPercent = 25;
   public float maxDiveForce = 0.1f;
   public float noseDiveConst = 0.01f;
   public float minSmooth = 0.5f;
   public float maxSmooth = 500f;
   public float minControlSpeedPercent = 25f;     // If you reach the speed defined by either of these, your ship has reached it's max or min sensitivity.

   // Rotation Variables, change these to give the effect of flying anything from a cargo plane to a fighter jet.
   // If this is checked, it locks pitch roll and yaw constants to the var rotationConst.
   public int pitchMultiplier = 1;
   public int rollMultiplier = 15;
   public int yawMultiplier = 50;
   public int ceilingMin = 1000;
   public int ceilingMax = 2000;

   // Airplane Aerodynamics - I strongly reccomend not touching these...
   private float nosePitch;
   private float trueSmooth;
   private float smoothRotation;
   private float truePitch;
   private float trueRoll;
   private float trueYaw;
   private float trueThrust;
   private static float trueDrag;
     
   // Misc. Variables
   private static float afterburnerConst;

   // HUD and Heading Variables. Use these to create your insturments.
   public static int trueSpeed;
   private Airplane planeInteraction;
     
   private void Awake ()
   {
      planeInteraction = GetComponent<Airplane>();

      trueDrag = 0;
      afterburnerConst = 0;
      smoothRotation = minSmooth + 0.01f;
   }

   private void Update () 
   {
      if(!planeInteraction.playerDriving)
         return;
         
      float pitch = Input.GetAxis ("Pitch") * pitchMultiplier;
      float roll = Input.GetAxis ("Roll") * rollMultiplier;
      float yaw = -Input.GetAxis ("Yaw") * yawMultiplier;

      pitch *=  Time.deltaTime;
      roll *= Time.deltaTime;
      yaw *= Time.deltaTime;

      // Smothing Rotations...   
      if ((smoothRotation > minSmooth) && (smoothRotation < maxSmooth))
      {
         smoothRotation = Mathf.Lerp (smoothRotation, trueThrust, (maxSpeed-(maxSpeed/minControlSpeedPercent))* Time.deltaTime);
      }
      if (smoothRotation <= minSmooth)
      {
         smoothRotation = smoothRotation +0.01f;
      }
      if ((smoothRotation >= maxSmooth) && (trueThrust < (maxSpeed*(minControlSpeedPercent/100))))
      {
         smoothRotation = smoothRotation -0.1f;
      }
      trueSmooth = Mathf.Lerp (trueSmooth, smoothRotation, 5 * Time.deltaTime);
      truePitch = Mathf.Lerp (truePitch, pitch, trueSmooth * Time.deltaTime);
      trueRoll = Mathf.Lerp (trueRoll, roll, trueSmooth * Time.deltaTime);
      trueYaw = Mathf.Lerp (trueYaw, yaw, trueSmooth * Time.deltaTime);
   
      // * * This next block handles the thrust and drag.
      float throttle = (((-(Input.GetAxis ("Throttle")))/2) * 90);
   
      if(transform.position.y > ceilingMin)
      {
         //Find the percentage of thinning atmosphere
         float airThinning = ((transform.position.y - ceilingMin)/(ceilingMax - ceilingMin))/20;  
         //Apply the thin atmosphere to the throttle
         throttle = throttle * airThinning;
      }
      
      if ( throttle/speedConst >= trueThrust)
      {
         trueThrust = Mathf.SmoothStep (trueThrust, throttle/speedConst, accelerateSpeed * Time.deltaTime);
      }
      if (throttle/speedConst < trueThrust)
      {
         trueThrust = Mathf.Lerp (trueThrust, throttle/speedConst, decelerateConst * Time.deltaTime);
      }  
   
      flyerRigidbody.drag = liftConst * ((trueThrust) * (trueThrust));
   
      if (trueThrust <= (maxSpeed/levelFlightPercent))
      {
   
         nosePitch = Mathf.Lerp (nosePitch, maxDiveForce, noseDiveConst * Time.deltaTime);
      }
      else
      {
         nosePitch = Mathf.Lerp (nosePitch, 0, 2* noseDiveConst * Time.deltaTime);
      }
   
      trueSpeed = Mathf.RoundToInt(((trueThrust/2f) * maxSpeed));

      // Adding nose dive when speed gets below a percent of your max speed  
      if ( ((trueSpeed - trueDrag) + afterburnerConst) <= (maxSpeed * levelFlightPercent/100))
      {
         noseDiveConst = Mathf.Lerp (noseDiveConst,maxDiveForce, (((trueSpeed - trueDrag) + afterburnerConst) - (maxSpeed * levelFlightPercent/100)) *5 *Time.deltaTime);
         flyer.Rotate(noseDiveConst, 0, 0, Space.World);   
      }
   }
   
   private void FixedUpdate () 
   {
      if(!planeInteraction.playerDriving) return;

      Vector3 rotations = new Vector3(Input.GetAxis("Mouse Y"), Input.GetAxis("Horizontal"), -Input.GetAxis("Mouse X"));
      // flyerRigidbody.rotation = Quaternion.Euler(rotations);

      // rotations += transform.forward;

      rotations *= pitchMultiplier;
      
      flyerRigidbody.AddRelativeTorque(rotations);

      if (trueThrust <= maxSpeed)
      {
         // Horizontal Force
         float speed = ((trueSpeed - trueDrag) / 100 + afterburnerConst);
         
         // transform.Translate(0, 0, speed);

         flyerRigidbody.AddForce(transform.forward * trueSpeed, ForceMode.Acceleration);
      }

   }
}
