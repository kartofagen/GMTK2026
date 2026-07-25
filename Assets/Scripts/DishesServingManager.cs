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
    private LevelManager _levelManager;
    
    private bool _dishChosen = false;
    
    private void Awake()
    {
        _movingInsideSystem = microwave.GetComponent<MovingInsideSystem>();
        _levelManager = GetComponent<LevelManager>();
    }

    public void Serve(Transform dish)
    {
        if (_dishChosen) return;
        
        _dishChosen = true;
        
        dish.DORotate(_movingInsideSystem.EntryPoint.eulerAngles, duration).SetEase(easeRotate);
        dish.DOMove(_movingInsideSystem.EntryPoint.position, duration).SetEase(easeMove)
            .OnComplete(() => _movingInsideSystem.Register(dish));
        
        Camera.main?.transform.GetComponent<Animator>().SetTrigger("Turn");
    }

    public void RemoveDish(Transform dishTransform)
    {
        _dishChosen = false;
        
        Dish dish = dishTransform.GetComponent<Dish>();
        _levelManager.ManageLevel(dish);
        
        Camera.main?.transform.GetComponent<Animator>().SetTrigger("Turn");
    }
}
