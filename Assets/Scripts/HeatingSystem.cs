using UnityEngine;

public class HeatingSystem : MonoBehaviour
{
    [SerializeField] private Dish dish;
    
    private MicrowaveTimer _microwaveTimer;

    void Awake()
    {
        _microwaveTimer = GetComponent<MicrowaveTimer>();
    }

    void Update()
    {
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
