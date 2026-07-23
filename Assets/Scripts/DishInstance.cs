using UnityEngine;

public enum DishStatus
{
    InProgress,
    Success,
    Underheated,
    Exploded
}

public class DishInstance : MonoBehaviour
{
    [SerializeField] private Dish dish;

    public float currentTemp;
    public DishStatus status;
}
