using System.Collections.Generic;
using UnityEngine;

public class Dish : MonoBehaviour
{
    public string dishName;
    [SerializeField] private DishComponent[] components;

    public void HeatComponents(float deltaTime)
    {
        foreach (var component in components)
        {
            component.Heat(deltaTime);
        }
    }
    
    public void CoolComponents(float deltaTime)
    {
        
    }
}
