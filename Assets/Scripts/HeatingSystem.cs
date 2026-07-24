using UnityEngine;
using Zenject;

public class HeatingSystem : MonoBehaviour
{
    [SerializeField] private Dish dish;

    [Inject] private MicrowaveContext _context;

    private MicrowaveTimer _microwaveTimer;

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
        if (dish.DishStatus != DishStatus.InProgress) return;
        
        dish.HeatComponents(Time.deltaTime);
        
        if (dish.DishStatus == DishStatus.Exploded)
        {
            // _microwaveTimer.FinishHeating();
        }
    }

    private void CalculateCooling()
    {
        dish.CoolComponents(Time.deltaTime);
    }
}
