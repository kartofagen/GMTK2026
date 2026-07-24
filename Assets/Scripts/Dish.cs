using UnityEngine;

public enum DishStatus
{
    InProgress,
    Success,
    Underheated,
    Exploded
}

public class Dish : MonoBehaviour
{
    public string dishName;
    [SerializeField] private DishComponent[] components;
    
    public DishStatus DishStatus { get; private set; } = DishStatus.InProgress;

    public void HeatComponents(float deltaTime)
    {
        foreach (var component in components)
        {
            component.Heat(deltaTime);
        }
        UpdateStatus();
    }
    
    public void CoolComponents(float deltaTime)
    {
        float averageTemp = 0f;
        foreach (var component in components)
        {
            averageTemp += component.CurrentTemp;
        }
        averageTemp /= components.Length;

        foreach (var component in components)
        {
            component.Cool(deltaTime, averageTemp);
        }
    }

    public void UpdateStatus()
    {
        foreach (var component in components)
        {
            var status = component.GetStatus();
            if (status == DishComponentStatus.Explodes)
            {
                DishStatus = DishStatus.Exploded;
                Debug.Log("Explode!!!");
                return;
            }
        }
    }
}
