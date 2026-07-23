using UnityEngine;
using Zenject;

public class DishComponent : MonoBehaviour
{
    public string componentName;
    [SerializeField] private AnimationCurve heatingCurve;
    [SerializeField,
     Tooltip("Время, за которое еда готова (по нижней границе)")] private float readyTime; 
    
    [Inject] private GameConfig _gameConfig;
    
    private float currentTemp;
    
    public float CurrentTemp => currentTemp;
    
    public void Heat(float deltaTime)
    {
    }
}
