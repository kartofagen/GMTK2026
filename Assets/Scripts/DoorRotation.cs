using System;
using DG.Tweening;
using UnityEngine;

public class DoorRotation : MonoBehaviour
{
    [SerializeField] private float openedYAngle;
    [SerializeField] private float duration;
    [SerializeField] private Ease easeRotate = Ease.InOutBack;
    
    private bool isOpened = false;
    
    public bool IsOpened => isOpened;
    
    public bool IsBlocked { get; set; }

    public event Action DoorOpened;
    
    private void OnMouseDown()
    {
        if (IsBlocked) return;
        
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
