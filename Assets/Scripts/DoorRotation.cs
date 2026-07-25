using System;
using DG.Tweening;
using UnityEngine;

public class DoorRotation : MonoBehaviour
{ 
    [SerializeField] private MicrowaveTimer microwave;
    
    [SerializeField] private float openedYAngle;
    [SerializeField] private float duration;
    [SerializeField] private Ease easeRotate = Ease.InOutBack;
    
    private bool isOpened = false;
    
    public bool IsOpened => isOpened;

    public event Action DoorOpened;
    
    private void OnMouseDown()
    {
        if (microwave.State is MicrowaveState.Heating or MicrowaveState.Paused)
        {
            microwave.FinishHeating();
        }
        
        if (isOpened)
        {
            transform.DOLocalRotate(new Vector3(0f, 0, 0f), duration).SetEase(easeRotate);
        }
        else
        {
            transform.DOLocalRotate(new Vector3(0f, openedYAngle, 0f), duration).SetEase(easeRotate);
            DoorOpened?.Invoke();
        }

        isOpened = !isOpened;
    }
}
