using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ItemSway : MonoBehaviour
{
    [SerializeField] private Transform hand;
    [SerializeField] private Ease easeType;
    [SerializeField] private float moveAmount;
    [SerializeField] private float moveDuration;
    [SerializeField] private Vector3 moveRotation;
    [SerializeField] private float rotateDuration;
    [SerializeField] private Vector3 onJumpItemMovement;
    [SerializeField] private float onJumpItemMovementAmount;
    [SerializeField] private float onJumpDuration;
    private Sequence _itemMovementTweening;
    private Vector3 _cachedHandPosition;
    private Vector3 _cachedHandRotation;
    
    private void Awake()
    {
        _cachedHandPosition = hand.localPosition;
        _cachedHandRotation = hand.localEulerAngles;
    }
    public void OnStartedMoving()
    {
        _itemMovementTweening = DOTween.Sequence();
        
        _itemMovementTweening
            .Append(hand.DOLocalMoveX(_cachedHandPosition.x + moveAmount, moveDuration)
                .From(_cachedHandPosition.x))
            .Append(hand.DOLocalMoveX(_cachedHandPosition.x, moveDuration))
            .Append(hand.DOLocalMoveX(_cachedHandPosition.x - moveAmount, moveDuration))
            .Append(hand.DOLocalMoveX(_cachedHandPosition.x, moveDuration))
            .Insert(0f, hand.DOLocalRotate(moveRotation, rotateDuration))
                .Append(hand.DOLocalRotate(_cachedHandRotation, rotateDuration))
            .SetLoops(-1).SetEase(easeType);
    }
    public void OnStoppedMoving()
    {
        if(_itemMovementTweening != null)
        {
            _itemMovementTweening.Kill();
        }

        hand.DOLocalMoveX(_cachedHandPosition.x, moveDuration);
        hand.DOLocalRotate(_cachedHandRotation, rotateDuration);
    }

    public async void OnJump()
    {
        await hand.DOLocalJump(_cachedHandPosition + onJumpItemMovement, onJumpItemMovementAmount, 1, onJumpDuration).AsyncWaitForCompletion();
        await hand.DOLocalJump(_cachedHandPosition, onJumpItemMovementAmount, 1, onJumpDuration).AsyncWaitForCompletion();
    }
}
