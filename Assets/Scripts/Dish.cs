using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Dish : ScriptableObject 
{
    public string dishName;
    public List<DishComponent> components;
}
