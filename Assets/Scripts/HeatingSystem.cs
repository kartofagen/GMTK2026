using UnityEngine;

public class HeatingSystem : MonoBehaviour
{
    [SerializeField] private Dish dish;
    
    private MicrowaveTimer microwaveTimer;

    void Awake()
    {
        microwaveTimer = GetComponent<MicrowaveTimer>();
    }

    void Update()
    {
        if (microwaveTimer.State == MicrowaveState.Finished || dish.DishStatus == DishStatus.Exploded) return;
        
        switch (microwaveTimer.State)
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
    }

    private void CalculateCooling()
    {
        dish.CoolComponents(Time.deltaTime);
    }
}
