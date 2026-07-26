using System;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class DoorRotation : MonoBehaviour
{ 
    [SerializeField] private MicrowaveTimer microwave;
    
    [SerializeField] private float openedYAngle;
    [SerializeField] private float duration;
    [SerializeField] private Ease easeRotate = Ease.InOutBack;
    
    [SerializeField] private Collider frontWallCollider;
    
    private bool _isOpened = false;
    private CinemachineImpulseSource _impulseSource;
    
    public bool IsOpened => _isOpened;

    public event Action DoorOpened;

    private void Awake()
    {
        _impulseSource =  GetComponent<CinemachineImpulseSource>();
    }
    
    private void OnMouseDown()
    {
        if (microwave.State is MicrowaveState.Heating or MicrowaveState.Paused)
            _impulseSource.DefaultVelocity.z = -0.1f;
        else
            _impulseSource.DefaultVelocity.z = -0.025f;
            
        if (_isOpened)
        {
            _impulseSource.GenerateImpulse();
            transform.DOLocalRotate(new Vector3(0f, 0, 0f), duration).SetEase(easeRotate);
        }
        else
        {
            _impulseSource.GenerateImpulse();
            transform.DOLocalRotate(new Vector3(0f, openedYAngle, 0f), duration).SetEase(easeRotate);
            DoorOpened?.Invoke();
        }

        _isOpened = !_isOpened;
        frontWallCollider.enabled = !_isOpened;
        
        if (microwave.State is MicrowaveState.Heating or MicrowaveState.Paused)
        {
            microwave.FinishHeating();
        }
    }
}
