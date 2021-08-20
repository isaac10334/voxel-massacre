using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ItemSway : MonoBehaviour
{
    [SerializeField] private Ease easeType;
    [SerializeField] private float moveAmount;
    [SerializeField] private float moveDuration;
    [SerializeField] private Vector3 moveRotation;
    [SerializeField] private Vector3 jumpRotation;
    [SerializeField] private float rotateDuration;
    [SerializeField] private int rotateVibrato;
    [SerializeField] private float rotateElasticity;
    [SerializeField] private Vector3 onJumpItemMovement;
    [SerializeField] private float onJumpItemMovementAmount;
    [SerializeField] private float onJumpDuration;
    private Sequence _itemMovementTweening;
    private Transform _target;
    private Vector3 _cachedTargetPosition;
    private Vector3 _cachedTargetRotation;
    public void OnStartedMoving()
    {
        if(_itemMovementTweening != null)
        {
            _itemMovementTweening.Kill();
        }

        if(_target == null) return;

        _itemMovementTweening = DOTween.Sequence();
        
        _itemMovementTweening
            .Append(_target.DOPunchRotation(moveRotation, rotateDuration, rotateVibrato, rotateElasticity))
            .SetLoops(-1).SetEase(easeType);
    }

    public void OnStoppedMoving()
    {
        if(_itemMovementTweening != null)
        {
            _itemMovementTweening.Kill();
        }

        if(_target == null) return;

        _target.DOLocalMoveX(_cachedTargetPosition.x, moveDuration);
        _target.DOLocalRotate(_cachedTargetRotation, rotateDuration);
    }

    public async void OnJump()
    {
        if(_target == null) return;

        await _target.DOPunchRotation(jumpRotation, onJumpDuration, rotateVibrato, rotateElasticity).AsyncWaitForCompletion();
        // await hand.DOLocalJump(_cachedHandPosition + onJumpItemMovement, onJumpItemMovementAmount, 1, onJumpDuration).AsyncWaitForCompletion();
        // await hand.DOLocalJump(_cachedHandPosition, onJumpItemMovementAmount, 1, onJumpDuration).AsyncWaitForCompletion();
    }

    public void SetTarget(Transform target)
    {
        _target = target;
        _cachedTargetPosition = _target.localPosition;
        _cachedTargetRotation = _target.localEulerAngles;
    }

    private bool IsAimingDownSight() => Input.GetMouseButton(1);
}
