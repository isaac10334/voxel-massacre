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

    public bool movementStopped;
    public bool isGrounded;
    
    [SerializeField] private SkinnedMeshRenderer meshRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private float gravity = -9.8f;
    [SerializeField] private float defaultWalkingSpeed = 6f;
    [SerializeField] private float sprintingSpeed = 8f;
    [SerializeField] private float inAirSubtraction = 2f;

    [SerializeField] private float lookSpeed = 3;
    [SerializeField] private float distanceToGround = 0.3f;
    [Header("Falling")]
    [SerializeField] private float minimumTimeInAirRequiredToPayLandingSound = 0.2f;
    [SerializeField] private float amountOfFallingThatCanCauseDamage = 4f;
    [SerializeField] private float fallDamageMultiplier = 1f;
    [Header("Jumping")]
    [SerializeField] private int _jumpBuffering = 2;
    [SerializeField] private float jumpLerpDuration = 0.5f;
    [SerializeField]  private float jumpHeight = 2f;
    [Header("Audio")]
    [SerializeField] private AudioClip landSound;
    [SerializeField] private AudioClip jumpSound;
    private float _leftGroundAt;

    private bool _crouching = false;
    private CharacterController _characterController;
    private Vector2 rotation = Vector2.zero;
    private Rigidbody _rigidbody;

    private bool _toggleOnGroundHit;
    private float _currentlyInAirTime;
    private bool _currentlyInAir;
    private bool _jumpOnReturn;
    private bool _jumping;
    private Vector3 _direction;
    private AudioSource _audioSource;
    private float _walkingSpeed;
    private float _sprintingSpeed;

    public void Awake()
    {
        _audioSource = gameObject.GetOrAddComponent<AudioSource>();
        _characterController = gameObject.GetOrAddComponent<CharacterController>();
        _characterController.detectCollisions = false;

        _rigidbody = gameObject.GetOrAddComponent<Rigidbody>();
        _rigidbody.isKinematic = true;
        meshRenderer.enabled = true;

        cam.enabled = false;
        cam.GetComponent<AudioListener>().enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void OnStartLocalPlayer()
    {
        cam.enabled = true;
        cam.GetComponent<AudioListener>().enabled = true;
        meshRenderer.enabled = false;
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
    
    private void Update()
    {
        if(!isLocalPlayer) return;

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

    private void OnHitGround(float timeInAir)
    {
        float lengthFell = _leftGroundAt - transform.position.y;

        if(lengthFell > amountOfFallingThatCanCauseDamage)
        {
            Player.Instance.CmdTakeDamage((int)(lengthFell * fallDamageMultiplier));
        }

        if(_jumpOnReturn)
        {
            StartCoroutine(JumpTo(new Vector3(0, jumpHeight, 0)) );
            _jumpOnReturn = false;
        }
        else
        {
            if(timeInAir > minimumTimeInAirRequiredToPayLandingSound)
                _audioSource.PlayOneShot(landSound);
        }

        _currentlyInAirTime = 0f;
    }

    private void UpdateForCharacterController()
    {
        CheckGroundedStuff();
        HandleJumping();

        Vector3 moveDirectionForward = Vector3.zero;
        Vector3 moveDirectionSide = Vector3.zero;

        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        animator.SetFloat("Vertical", vertical);
        animator.SetFloat("Horizontal", horizontal);


        if(!movementStopped && PlayerInput.InputEnabled)
        {
            moveDirectionForward = (transform.forward * vertical) * defaultWalkingSpeed;
            moveDirectionSide = (transform.right * horizontal) * defaultWalkingSpeed;
        }

        Vector3 inputDirection = (moveDirectionForward + moveDirectionSide).normalized;
        _walkingSpeed = isGrounded ? (defaultWalkingSpeed) : (defaultWalkingSpeed - inAirSubtraction);
        _sprintingSpeed = isGrounded ? (sprintingSpeed) : (sprintingSpeed - inAirSubtraction);

        _direction.x = inputDirection.x * (Input.GetKey(KeyCode.LeftShift) ? _sprintingSpeed : _walkingSpeed);
        _direction.z = inputDirection.z * (Input.GetKey(KeyCode.LeftShift) ? _sprintingSpeed : _walkingSpeed);
        

        _direction.y += gravity * Time.deltaTime;
        _characterController.Move(_direction * Time.deltaTime);
    }

    private void HandleJumping()
    {
        if(Input.GetButtonDown("Jump"))
        {
            if(_characterController.isGrounded)
            {
                StartCoroutine(JumpTo(new Vector3(0, jumpHeight, 0)) );
                //StartCoroutine(JumpTo(new Vector3(transform.position.x, jumpHeight, transform.position.z)) );
            }
            else
            {
                BufferJumpForFrames(_jumpBuffering);
            }
        }
    }

    IEnumerator JumpTo(Vector3 endPos) 
    {
        float duration = jumpLerpDuration; //seconds

        float elapsedTime = 0;
        float ratio = elapsedTime / duration;

        _jumping = true;

        _leftGroundAt = transform.position.y;

        _audioSource.PlayOneShot(jumpSound);

        while(ratio < 1f)
        {
            elapsedTime += Time.deltaTime;
            ratio = elapsedTime / duration;
            _direction = Vector3.Lerp(_direction, endPos, ratio);
            yield return null;
        }

       //for (float t = 0f; t < duration; t += Time.deltaTime) 
       //{
       //    _direction = Vector3.Lerp(_direction, endPos, t / duration);
       //    yield return 0;
       //}
        _jumping = false;
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
        rotation.x = Mathf.Clamp(rotation.x, -30f, 30f);
        transform.eulerAngles = new Vector2(0, rotation.y) * lookSpeed;
        cam.transform.localRotation = Quaternion.Euler(rotation.x * lookSpeed, 0, 0);
    }

    public void ShakeCamera(float amount, float duration)
    {
        cam.transform.DOShakePosition(amount, duration);
    }
}
