using UnityEngine;

public class HeatingSystem : MonoBehaviour
{
    [SerializeField] private Dish dish;
    
    private MicrowaveTimer _microwaveTimer;
    
    public Dish Dish {  set => dish = value; }

    void Awake()
    {
        _microwaveTimer = GetComponent<MicrowaveTimer>();
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
