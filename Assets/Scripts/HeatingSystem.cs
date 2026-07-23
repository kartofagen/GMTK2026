using UnityEngine;

public enum DishStatus
{
    InProgress,
    Success,
    Underheated,
    Exploded
}

public class HeatingSystem : MonoBehaviour
{
    [SerializeField] private Dish dish;
    
    private MicrowaveTimer microwaveTimer;
    
    public DishStatus DishStatus { get; set; } = DishStatus.InProgress;

    void Awake()
    {
        microwaveTimer = GetComponent<MicrowaveTimer>();
    }

    void Update()
    {
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
