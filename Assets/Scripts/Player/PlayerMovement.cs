using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DG.Tweening;

public class PlayerMovement : NetworkBehaviour
{
    public enum PlayerMovementModes
    {
        CharacterController,
        Rigidbody
    }
    
    public PlayerMovementModes currentMode;

    public Camera cam;
    public Transform cameraShake;
    public bool movementStopped;
    public bool isGrounded;
    
    // hit ground tween
    [SerializeField] private float groundAcceleration;
    [SerializeField] private float maximumGroundVelocity;
    [SerializeField] private float airAcceleration;
    [SerializeField] private float maximumAirVelocity;

    [SerializeField] private Vector3 hitGroundRotation;
    [SerializeField] private float hitGroundRotationDuration;

    [SerializeField] private SkinnedMeshRenderer visibleToOthersRenderer;
    [SerializeField] private SkinnedMeshRenderer visibleToLocalPlayerRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private float gravity = -9.8f;
    [SerializeField] private float defaultWalkingSpeed = 6f;
    [SerializeField] private float sprintingSpeed = 8f;
    [SerializeField] private float inAirSubtraction = 2f;

    [SerializeField] private float mouseSensitivity = 3;
    [SerializeField] private float distanceToGround = 0.3f;
    [Header("Falling")]
    [SerializeField] private float minimumTimeInAirRequiredToPayLandingSound = 0.2f;
    [SerializeField] private float amountOfFallingThatCanCauseDamage = 4f;
    [SerializeField] private float fallDamageMultiplier = 1f;
    [Header("Jumping")]
    [SerializeField] private int _jumpBuffering = 2;
    [SerializeField] private float jumpLerpDuration = 1f;
    [SerializeField]  private float jumpHeight = 2f;
    [Header("Audio")]
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private ItemSway itemSway;
    private float _leftGroundAt;
    private CharacterController _characterController;
    private Vector2 rotation = Vector2.zero;
    private Rigidbody _rigidbody;

    private bool _toggleOnGroundHit;
    private float _currentlyInAirTime;
    private bool _currentlyInAir;
    private bool _jumpOnReturn;
    private bool _isJumping;
    public Vector3 _direction;
    private AudioSource _audioSource;
    private bool _wasMoving;
    private float _elapsedTime = 0f;
    private float _ratio = 0f;
    public void Awake()
    {
        _audioSource = gameObject.GetOrAddComponent<AudioSource>();
        _characterController = gameObject.GetOrAddComponent<CharacterController>();
        _characterController.detectCollisions = false;

        _rigidbody = gameObject.GetOrAddComponent<Rigidbody>();
        _rigidbody.isKinematic = true;
        
        cam.enabled = false;
        cam.GetComponent<AudioListener>().enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        visibleToOthersRenderer.enabled = true;
        visibleToLocalPlayerRenderer.enabled = false;
    }

    private void Start()
    {
        UIThings.Instance.settingsMenu.SubscribeToSettingsUpdate(OnSettingsUpdate);
    }

    private void OnSettingsUpdate(Settings settings)
    {
        mouseSensitivity = settings.mouseSensitivity;
    }

    public override void OnStartLocalPlayer()
    {
        cam.enabled = true;
        cam.GetComponent<AudioListener>().enabled = true;

        visibleToOthersRenderer.enabled = false;
        visibleToLocalPlayerRenderer.enabled = true;
    }

    public void ToggleRigidbodyMode(bool toggle, bool toggleOnGroundHit = false)
    {
        if (toggle)
        {
            _characterController.enabled = false;
            currentMode = PlayerMovementModes.Rigidbody;
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;

            _toggleOnGroundHit = toggleOnGroundHit;
        }
        else
        {
            _characterController.enabled = true;
            currentMode = PlayerMovementModes.CharacterController;
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
        }
    }

    private bool IsMoving() => Mathf.Abs(_characterController.velocity.x) > 0 ||
            Mathf.Abs(_characterController.velocity.z) > 0;

    private void Update()
    {
        if(!isLocalPlayer) return;
        
        if(IsMoving() && !_wasMoving)
        {
            if(!_isJumping)
            {
                OnStartedMoving();
            }
            _wasMoving = true;
        }
        else if(!IsMoving() && _wasMoving)
        {
            OnStoppedMoving();
            _wasMoving = false;
        }

        if (currentMode == PlayerMovementModes.Rigidbody)
        {
            if (_toggleOnGroundHit)
            {
                if (Physics.Raycast(transform.position, -Vector3.up, distanceToGround + 0.1f))
                {
                    ToggleRigidbodyMode(false);
                }
            }

            return;
        }

        UpdateForCharacterController();
    }

    private void LateUpdate()
    {
        if(!isLocalPlayer) return;

        if(PlayerInput.InputEnabled) Look();
    }

    private void CheckGroundedStuff()
    {
        isGrounded = _characterController.isGrounded;

        if(!isGrounded) _currentlyInAir = true;

        if(_currentlyInAir)
            _currentlyInAirTime += Time.deltaTime;

        if(isGrounded)
        {
            if(_currentlyInAir)
            {
                _currentlyInAir = false;
                OnHitGround(_currentlyInAirTime);
            }
        }
    }

    private async void OnHitGround(float timeInAir)
    {
        _isJumping = false;
        _elapsedTime = 0;
        _ratio = _elapsedTime / jumpLerpDuration;

        Debug.Log("Hit ground.");

        float lengthFell = _leftGroundAt - transform.position.y;

        if(lengthFell > amountOfFallingThatCanCauseDamage)
        {
            Player.Instance.CmdTakeDamage((int)(lengthFell * fallDamageMultiplier), false);
        }

        if(_jumpOnReturn)
        {
            Jump();
            _jumpOnReturn = false;
        }
        else
        {
            if(timeInAir > minimumTimeInAirRequiredToPayLandingSound)
                _audioSource.PlayOneShot(landSound);

            await cameraShake.DOShakeRotation(0.25f, 2f).AsyncWaitForCompletion();
        }

        _currentlyInAirTime = 0f;
    }

    private Vector3 prevVelocity;
    [SerializeField] private float friction = 10f;

    private void UpdateForCharacterController()
    {
        CheckGroundedStuff();

        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        animator.SetFloat("Vertical", vertical);
        animator.SetFloat("Horizontal", horizontal);

        Vector3 inputVector = GetInputVector(vertical, horizontal);

        if(isGrounded)
        {
            inputVector = ApplySprinting(inputVector);
            _direction = MoveOnGround(inputVector, _direction);
        }
        else
        {
            _direction = MoveInAir(inputVector, _direction);
        }

        HandleJumping();
        
        _direction.y += gravity * Time.deltaTime;
        
        _characterController.Move(_direction);
    }

    private Vector3 ApplySprinting(Vector3 inputVector)
    {
        float multiplier = (Input.GetKey(KeyCode.LeftShift) ? sprintingSpeed : 1);
        inputVector.x *= multiplier;
        inputVector.z *= multiplier;

        return inputVector;
    }

    private Vector3 GetInputVector(float vertical, float horizontal)
    {
        // So, the problem is that if you're in the air and you're moving, mouse input isn't applied to anything.
        // That's because this is just the movement input vector, which should be influenced by mouselook - but you can't have mouse input here because then you could move the mouse to walk around, makes no sense - so how do I have that mouse look air control I want?
        // Hmm... I think that it's not related to the input vector at all. But, I'll need to test - first get jump working

        Vector3 moveDirectionForward = Vector3.zero;
        Vector3 moveDirectionSide = Vector3.zero;

        if(!movementStopped && PlayerInput.InputEnabled)
        {
            moveDirectionForward = (transform.forward * vertical) * defaultWalkingSpeed;
            moveDirectionSide = (transform.right * horizontal) * defaultWalkingSpeed;
        }

        Vector3 input = (moveDirectionForward + moveDirectionSide).normalized;
        return input;
    }

    private Vector3 MoveOnGround(Vector3 inputVector, Vector3 previousVelocity)
    {
        float speed = prevVelocity.magnitude;

        if (speed != 0) // To avoid divide by zero errors
        {
            float drop = speed * friction * Time.deltaTime;
            prevVelocity *= Mathf.Max(speed - drop, 0) / speed; // Scale the velocity based on friction.
        }

        // ground_accelerate and max_velocity_ground are server-defined movement variables
        return Accelerate(inputVector, prevVelocity, groundAcceleration, maximumGroundVelocity);
    }

    private Vector3 MoveInAir(Vector3 inputVector, Vector3 previousVelocity)
    {
        // air_accelerate and max_velocity_air are server-defined movement variables
        return Accelerate(inputVector, prevVelocity, airAcceleration, maximumAirVelocity);
    }

    private Vector3 Accelerate(Vector3 inputVector, Vector3 prevVelocity, float accelerate, float max_velocity)
    {
        float projVel = Vector3.Dot(prevVelocity, inputVector); // Vector projection of Current velocity onto accelDir.
        float accelVel = accelerate * Time.deltaTime; // Accelerated velocity in direction of movment

        // If necessary, truncate the accelerated velocity so the vector projection does not exceed max_velocity
        if(projVel + accelVel > max_velocity)
            accelVel = max_velocity - projVel;

        return prevVelocity + inputVector * accelVel;
    }

    private void HandleJumping()
    {
        if(Input.GetButtonDown("Jump"))
        {
            if(_characterController.isGrounded)
            {
                Jump();
            }
            else
            {
                BufferJumpForFrames(_jumpBuffering);
            }
        }

        if(_isJumping && _ratio < 1f)
        {
            _elapsedTime += Time.deltaTime;
            _ratio = _elapsedTime / jumpLerpDuration;
            _direction.y = Mathf.Slerp(_direction.y, jumpHeight, _ratio);
        }
    }

    private void Jump() 
    {
        Debug.Log("Jumping");

        itemSway.OnJump();

        _isJumping = true;

        _leftGroundAt = transform.position.y;
        _audioSource.PlayOneShot(jumpSound);
    }

    private void OnStartedMoving()
    {
        itemSway.OnStartedMoving();
    }

    private void OnStoppedMoving()
    {
        itemSway.OnStoppedMoving();
    }

    private async void BufferJumpForFrames(int frames)
    {
        _jumpOnReturn = true;
        
        for(int i = 0; i < frames; i++)
        {
            await new WaitForEndOfFrame();
        }

        _jumpOnReturn = false;

    }

    public void Look()
    {
        rotation.y += Input.GetAxis("Mouse X");
        rotation.x += -Input.GetAxis("Mouse Y");

        rotation.x = Mathf.Clamp(rotation.x, -85f / mouseSensitivity, 90f / mouseSensitivity);

        transform.eulerAngles = new Vector2(0, rotation.y) * mouseSensitivity;

        cam.transform.localRotation = Quaternion.Euler(rotation.x * mouseSensitivity, 0, 0);
    }

    public void ShakeCamera(float amount, float duration)
    {
        cam.transform.DOShakePosition(amount, duration);
    }
}
