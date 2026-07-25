using System;
using R3;
using UnityEngine;
using Zenject;

public class HeatingSystem : MonoBehaviour
{
    [SerializeField] private Dish dish;

    [Inject] private MicrowaveContext _context;

    private MicrowaveTimer _microwaveTimer;
    
    public readonly Subject<DishStatus> onHeatingFinished = new();

    public Dish Dish
    {
        set
        {
            dish = value;
            _context.Channels.Value = dish ? dish.Channels : null;
        }
    }

    void Awake()
    {
        _microwaveTimer = GetComponent<MicrowaveTimer>();
        
        _microwaveTimer
            .onFinished
            .Subscribe(OnFinished)
            .AddTo(this);
    }

    void Start()
    {
        // Блюдо могло быть задано в инспекторе, а не через сеттер.
        if (dish) _context.Channels.Value = dish.Channels;
    }

    void Update()
    {
        if (!dish) return;
        if (_microwaveTimer.State == MicrowaveState.Finished) return;
        
        switch (_microwaveTimer.State)
        {
            case MicrowaveState.Heating:
                CalculateHeating();
                break;
            case MicrowaveState.Paused:
                CalculateCooling();
                break;
        }
    }

    private void CalculateHeating()
    {
        /*if (dish.DishStatus == DishStatus.Exploded)
        {
            _microwaveTimer.FinishHeating();
        }*/
        
        dish.HeatComponents(Time.deltaTime);
    }

    private void CalculateCooling()
    {
        dish.CoolComponents(Time.deltaTime);
    }

    private void OnFinished(Unit unit)
    {
        if (!dish) return;
        
        onHeatingFinished.OnNext(dish.GetFinalStatus());
    }
}
