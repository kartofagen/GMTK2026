using System;
using DG.Tweening;
using UnityEngine;

public class DishesServingManager : MonoBehaviour
{
    [SerializeField] private GameObject microwave;
    [SerializeField] private float duration = 1.5f;
    
    [SerializeField] private Ease easeMove = Ease.InOutBack;
    [SerializeField] private Ease easeRotate = Ease.InOutBack;

    private MovingInsideSystem _movingInsideSystem;
    
    private void Awake()
    {
        _movingInsideSystem = microwave.GetComponent<MovingInsideSystem>();
    }

    public void Serve(Transform dish)
    {
        dish.DORotate(_movingInsideSystem.EntryPoint.eulerAngles, duration).SetEase(easeRotate);
        dish.DOMove(_movingInsideSystem.EntryPoint.position, duration).SetEase(easeMove)
            .OnComplete(() => _movingInsideSystem.Register(dish));
    }
}
