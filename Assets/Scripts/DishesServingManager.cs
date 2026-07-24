using System;
using DG.Tweening;
using UnityEngine;

public class DishesServingManager : MonoBehaviour
{
    [SerializeField] private GameObject microwave;
    [SerializeField] private float duration;
    
    [SerializeField] private Ease easeMove = Ease.InOutBack;
    [SerializeField] private Ease easeRotate = Ease.InOutBack;

    private MovingInsideSystem _movingInsideSystem;
    private HeatingSystem _heatingSystem;
    
    private void Awake()
    {
        _movingInsideSystem = microwave.GetComponent<MovingInsideSystem>();
        _heatingSystem = microwave.GetComponent<HeatingSystem>();
    }

    public void Serve(Transform dish)
    {
        dish.DOMove(_movingInsideSystem.EntryPoint.position, 1.5f).SetEase(easeMove);
        dish.DORotate(_movingInsideSystem.EntryPoint.position, 1.5f).SetEase(easeRotate);
        dish.parent = microwave.transform;
        _heatingSystem.Dish = dish.GetComponent<Dish>();
    }
}
