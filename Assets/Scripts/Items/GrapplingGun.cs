using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapplingGun : MonoBehaviour
{
    
    [SerializeField] private float grappleSpeed = 6f;
    [SerializeField] private float distance = 200f;

    public float grappleDirectionMagnitude;

    private bool _isGrappling;
    private PlayerMovement _player;
    private Rigidbody _rb;
    private LineRenderer _lr;
    
    RaycastHit grapplePoint;

    private void Start()
    {
        _player = FindObjectOfType<PlayerMovement>();
        _rb = _player.gameObject.GetComponent<Rigidbody>();
        _lr = GetComponent<LineRenderer>();
    }
    
    private void Update()
    {
        if(!PlayerInput.InputEnabled) return;

        _lr.SetPosition(0, transform.position);

        Ray ray = _player.cam.ScreenPointToRay(Input.mousePosition);
        
        if (Input.GetMouseButtonDown(0) && Physics.Raycast(ray, out grapplePoint))
        {
            _player.ToggleRigidbodyMode(true, true);
            
            _isGrappling = true;
            Vector3 grappleDirection = (grapplePoint.point - _player.transform.position);
            
            _rb.velocity = grappleDirection.normalized * grappleSpeed;
            _lr.enabled = true;
            _lr.SetPosition(1, grapplePoint.point);
        }

        if (Input.GetMouseButtonUp(0))
        {
            _lr.enabled = false;
            _isGrappling = false;
        }

        if (_isGrappling)
        {
            transform.LookAt(grapplePoint.point);

            Vector3 grappleDirection = (grapplePoint.point - _player.transform.position);
            
            grappleDirectionMagnitude = grappleDirection.magnitude;
            
            /*
            if (distance < grappleDirection.magnitude)
            {
                float velocity = _rb.velocity.magnitude;
 
                Vector3 newDirection = Vector3.ProjectOnPlane(_rb.velocity, grappleDirection);
                
                //multiply by velocity?
                _rb.velocity = newDirection.normalized * velocity;
            }
            else
            {
                _rb.AddForce(grappleDirection.normalized * grappleSpeed, ForceMode.VelocityChange); //forcemodes?
                distance = grappleDirection.magnitude;
            }
            */
        }
        else
        {
            transform.localRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up); 
        }
        
    }
}
