using System;
using UnityEngine;
using DG.Tweening;

public class MicrowaveButton : MonoBehaviour
{
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private int vibrato = 2;
    [SerializeField] private float elasticity = 0.5f;

    private Tween _pressTween;
    private Tween _actionCallTween;
    private Vector3 _startPosition;
    private bool _calledAction = false;

    private void Awake()
    {
        _startPosition = transform.position;
    }

    private void OnMouseDown()
    {
        // _pressTween =
        //     transform.DOLocalMoveZ(transform.localPosition.z - 0.0113738f, 0.35f)
        //         .SetEase(Ease.OutQuad)
        //         .SetAutoKill(false)
        //         .OnComplete(() =>
        //         {
        //             Action();
        //             _pressTween.PlayBackwards();
        //         });

        if (_actionCallTween != null && _actionCallTween.IsPlaying())
        {
            _actionCallTween.Kill();
            Action();
        }

        _pressTween?.Kill();

        transform.position = _startPosition;

        _pressTween = transform.DOPunchPosition(new Vector3(0, 0, -0.011f), duration, vibrato, elasticity)
            .SetAutoKill(true);

        _actionCallTween = DOVirtual.DelayedCall(duration / (vibrato * 2), Action);
    }

    protected virtual void Action()
    {
    }
}