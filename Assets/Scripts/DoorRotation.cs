using System;
using DG.Tweening;
using R3;
using Unity.Cinemachine;
using UnityEngine;

public class DoorRotation : MonoBehaviour
{ 
    [SerializeField] private MicrowaveTimer microwave;
    
    [SerializeField] private float openedYAngle;
    [SerializeField] private float duration;
    [SerializeField] private Ease easeRotate = Ease.InOutBack;
    
    [SerializeField] private Collider frontWallCollider;
    
    private readonly ReactiveProperty<bool> _isOpened = new(false);
    private CinemachineImpulseSource _impulseSource;
    
    public bool IsOpened => _isOpened.Value;
    public ReadOnlyReactiveProperty<bool> IsOpenedProperty => _isOpened;

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
            
        if (_isOpened.Value)
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

        _isOpened.Value = !_isOpened.Value;
        frontWallCollider.enabled = !_isOpened.Value;
        
        if (microwave.State is MicrowaveState.Heating or MicrowaveState.Paused)
        {
            microwave.FinishHeating();
        }
    }
}
